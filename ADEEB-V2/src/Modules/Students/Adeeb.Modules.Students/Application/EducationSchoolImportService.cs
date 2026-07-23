using System.Globalization;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Students.Domain.Education;
using Adeeb.Modules.Students.Infrastructure.Persistence;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Adeeb.Modules.Students.Application;

/// <summary>Parses a bounded school catalog file twice: once for preview and once inside the commit transaction.</summary>
public sealed class EducationSchoolImportService(StudentsDbContext db, IDateTimeProvider clock)
{
    private const long MaxFileBytes = 5 * 1024 * 1024;
    private const long MaxExpandedWorkbookBytes = 50 * 1024 * 1024;
    private const int MaxPackageEntries = 1_000;
    private const int MaxRows = 5_000;
    private static readonly string[] Headers = ["RegionPathRu", "RegionPathTg", "SchoolNameRu", "SchoolNameTg", "SchoolNumber", "SchoolType", "AddressText"];

    public async Task<Result<EducationSchoolImportPreviewResponse>> PreviewAsync(EducationSchoolImportRequest request, CancellationToken ct)
    {
        var loaded = await LoadAsync(request.File, ct);
        return loaded.IsFailure
            ? Result<EducationSchoolImportPreviewResponse>.ValidationFailure(loaded.ValidationErrors!)
            : Result<EducationSchoolImportPreviewResponse>.Success(ToPreview(loaded.Value!));
    }

    public byte[] CreateCsvTemplate() => Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(
        string.Join(',', Headers) + Environment.NewLine +
        "Таджикистан / Душанбе,Тоҷикистон / Душанбе,Школа № 1,Мактаби № 1,1,school,Душанбе" + Environment.NewLine)).ToArray();

    public async Task<Result<EducationSchoolImportResultResponse>> ConfirmAsync(EducationSchoolImportRequest request, ClaimsPrincipal actor, CancellationToken ct)
    {
        var loaded = await LoadAsync(request.File, ct);
        if (loaded.IsFailure) return Result<EducationSchoolImportResultResponse>.ValidationFailure(loaded.ValidationErrors!);
        var rows = loaded.Value!;
        if (rows.Any(x => !x.IsValid)) return Result<EducationSchoolImportResultResponse>.Failure(EducationErrors.ImportInvalid);

        IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        await using (transaction)
        {
            try
            {
                var now = clock.UtcNow;
                var actorId = ActorId(actor);
                var batch = new EducationImportBatch(Guid.NewGuid(), EducationImportKind.Schools, request.File!.FileName, actorId,
                    rows.Count, rows.Count, 0, now);
                db.EducationImportBatches.Add(batch);
                var regionCache = await db.Regions.ToDictionaryAsync(x => RegionKey(x.ParentId, x.NormalizedNameRu), ct);
                var createdRegions = 0;
                var createdSchools = 0;
                var skippedSchools = 0;
                foreach (var row in rows)
                {
                    var (region, regionsAdded) = FindOrCreatePath(row, regionCache, now);
                    createdRegions += regionsAdded;
                    var existing = await db.Schools.AnyAsync(x => x.RegionId == region.Id && x.Type == row.Type &&
                        (x.Status == SchoolStatus.Draft || x.Status == SchoolStatus.Verified || x.Status == SchoolStatus.Inactive) &&
                        (row.Number.HasValue ? x.Number == row.Number : x.Number == null && x.NormalizedName == row.NormalizedNameRu), ct);
                    if (existing)
                    {
                        skippedSchools++;
                        continue;
                    }
                    var school = new School(Guid.NewGuid(), region.Id, row.SchoolNameTg, row.SchoolNameRu, null, row.Number, row.Type,
                        row.NormalizedNameRu, EducationNormalization.SearchText(row.SchoolNameTg, row.SchoolNameRu, null, row.Number), row.AddressText, actorId, now);
                    if (request.VerifyImportedSchools) school.Verify(actorId, now);
                    db.Schools.Add(school);
                    createdSchools++;
                }
                batch.Complete(createdRegions, createdSchools, skippedSchools,
                    JsonSerializer.Serialize(new { request.VerifyImportedSchools, sourceRows = rows.Count }), now);
                db.EducationAuditLogs.Add(new StudentEducationAuditLog(Guid.NewGuid(), actorId, "student.school_import.committed", "education_import_batch", batch.Id.ToString(), null,
                    null, JsonSerializer.Serialize(new { createdRegions, createdSchools, skippedSchools }), null, now));
                await db.SaveChangesAsync(ct);
                if (transaction is not null) await transaction.CommitAsync(ct);
                return Result<EducationSchoolImportResultResponse>.Success(new(batch.Id, createdRegions, createdSchools, skippedSchools, 0));
            }
            catch (DbUpdateException)
            {
                if (transaction is not null) await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
                return Result<EducationSchoolImportResultResponse>.Failure(EducationErrors.ImportConflict);
            }
        }
    }

    private RegionPathResult FindOrCreatePath(ParsedImportRow row, Dictionary<string, Region> cache, DateTimeOffset now)
    {
        Region? parent = null;
        var created = 0;
        for (var index = 0; index < row.RegionPathRu.Length; index++)
        {
            var ru = row.RegionPathRu[index];
            var key = RegionKey(parent?.Id, EducationNormalization.Key(ru));
            if (cache.TryGetValue(key, out var found)) { parent = found; continue; }
            var tg = row.RegionPathTg.ElementAtOrDefault(index);
            if (string.IsNullOrWhiteSpace(tg)) tg = ru;
            var type = TypeForDepth(index);
            var id = Guid.NewGuid();
            var paths = parent is null ? new[] { id } : [.. parent.PathIds, id];
            var region = new Region(id, parent?.Id, type, tg, ru, EducationNormalization.Key(tg), EducationNormalization.Key(ru), 0, paths, paths.Length - 1, now);
            region.SetPaths(parent is null ? tg : $"{parent.FullPathTg} / {tg}", parent is null ? ru : $"{parent.FullPathRu} / {ru}", paths, paths.Length - 1, now);
            db.Regions.Add(region);
            cache.Add(key, region);
            parent = region;
            created++;
        }
        return new RegionPathResult(parent!, created);
    }

    private async Task<Result<IReadOnlyList<ParsedImportRow>>> LoadAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0 || file.Length > MaxFileBytes)
            return InvalidFile<IReadOnlyList<ParsedImportRow>>("File is required and must not exceed 5 MB.");
        var extension = Path.GetExtension(file.FileName);
        if (!extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            return InvalidFile<IReadOnlyList<ParsedImportRow>>("Only .xlsx and .csv files are supported.");
        try
        {
            await using var input = file.OpenReadStream();
            await using var memory = new MemoryStream();
            await input.CopyToAsync(memory, ct);
            memory.Position = 0;
            IReadOnlyList<IReadOnlyList<string>> values;
            if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                values = ReadCsv(memory);
            }
            else
            {
                ValidateWorkbookPackage(memory);
                memory.Position = 0;
                values = ReadWorkbook(memory);
            }
            return Result<IReadOnlyList<ParsedImportRow>>.Success(Parse(values));
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or OpenXmlPackageException)
        {
            return InvalidFile<IReadOnlyList<ParsedImportRow>>(ex.Message);
        }
    }

    private static IReadOnlyList<ParsedImportRow> Parse(IReadOnlyList<IReadOnlyList<string>> values)
    {
        if (values.Count == 0) throw new InvalidDataException("Header row is missing.");
        if (values[0].Count != Headers.Length || Headers.Where((x, i) => !string.Equals(values[0][i].Trim(), x, StringComparison.Ordinal)).Any())
            throw new InvalidDataException("Headers must exactly match: " + string.Join(", ", Headers));
        if (values.Count - 1 > MaxRows) throw new InvalidDataException($"At most {MaxRows} rows are supported.");
        var identities = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<ParsedImportRow>();
        for (var i = 1; i < values.Count; i++)
        {
            var row = values[i];
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            string Value(int index) => index < row.Count ? row[index].Trim() : string.Empty;
            var errors = new List<string>();
            var ruPath = Value(0).Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var tgPath = Value(1).Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var nameRu = Value(2);
            var nameTg = string.IsNullOrWhiteSpace(Value(3)) ? null : Value(3);
            if (ruPath.Length is < 1 or > 8 || ruPath.Any(x => x.Length > Region.NameMaxLength)) errors.Add("RegionPathRu is invalid.");
            if (tgPath.Length > 0 && tgPath.Length != ruPath.Length) errors.Add("RegionPathTg must have the same segments as RegionPathRu.");
            if (string.IsNullOrWhiteSpace(nameRu) || nameRu.Length > School.NameMaxLength) errors.Add("SchoolNameRu is invalid.");
            if (nameTg?.Length > School.NameMaxLength) errors.Add("SchoolNameTg is too long.");
            int? number = null;
            if (!string.IsNullOrWhiteSpace(Value(4)) && (!int.TryParse(Value(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedNumber) || parsedNumber <= 0)) errors.Add("SchoolNumber must be a positive integer.");
            else if (int.TryParse(Value(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out var numberValue)) number = numberValue;
            if (!TryType(Value(5), out var type)) errors.Add("SchoolType is invalid.");
            var address = string.IsNullOrWhiteSpace(Value(6)) ? null : Value(6);
            if (address?.Length > School.AddressTextMaxLength) errors.Add("AddressText is too long.");
            var normalizedName = EducationNormalization.Key(nameRu);
            var duplicate = false;
            if (errors.Count == 0)
            {
                var identity = $"{string.Join('/', ruPath.Select(EducationNormalization.Key))}|{(int)type}|{number?.ToString(CultureInfo.InvariantCulture) ?? normalizedName}";
                duplicate = !identities.Add(identity);
                if (duplicate) errors.Add("Duplicate school identity in file.");
            }
            rows.Add(new ParsedImportRow(i + 1, ruPath, tgPath, nameRu, nameTg, number, type, normalizedName, address, duplicate, errors));
        }
        return rows;
    }

    private static EducationSchoolImportPreviewResponse ToPreview(IReadOnlyList<ParsedImportRow> rows) => new(rows.Count, rows.Count(x => x.IsValid), rows.Count(x => !x.IsValid), rows.Count(x => x.IsDuplicate),
        rows.Select(x => new EducationSchoolImportRowResponse(x.RowNumber, string.Join(" / ", x.RegionPathRu), x.SchoolNameRu, x.Number, x.IsValid, x.IsDuplicate, x.Errors)).ToArray());

    private static IReadOnlyList<IReadOnlyList<string>> ReadWorkbook(Stream stream)
    {
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbook = document.WorkbookPart ?? throw new InvalidDataException("Workbook is missing.");
        var sheet = workbook.Workbook.Sheets?.Elements<Sheet>().FirstOrDefault() ?? throw new InvalidDataException("Worksheet is missing.");
        var part = (WorksheetPart)workbook.GetPartById(sheet.Id!);
        return part.Worksheet.GetFirstChild<SheetData>()?.Elements<Row>().Select(row => Cells(row, workbook)).ToArray() ?? [];
    }

    private static void ValidateWorkbookPackage(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count > MaxPackageEntries) throw new InvalidDataException("Workbook contains too many package entries.");
        long expanded = 0;
        foreach (var entry in archive.Entries)
        {
            expanded += entry.Length;
            if (expanded > MaxExpandedWorkbookBytes) throw new InvalidDataException("Workbook expands beyond the allowed size.");
        }
    }

    private static IReadOnlyList<string> Cells(Row row, WorkbookPart workbook)
    {
        var output = new List<string>();
        foreach (var cell in row.Elements<Cell>())
        {
            var index = Column(cell.CellReference?.Value);
            while (output.Count < index) output.Add(string.Empty);
            var value = cell.DataType?.Value == CellValues.InlineString ? cell.InlineString?.InnerText ?? string.Empty : cell.CellValue?.Text ?? string.Empty;
            if (cell.DataType?.Value == CellValues.SharedString && int.TryParse(value, out var sharedIndex))
                value = workbook.SharedStringTablePart?.SharedStringTable?.Elements<SharedStringItem>().ElementAtOrDefault(sharedIndex)?.InnerText ?? string.Empty;
            output[index - 1] = value;
        }
        return output;
    }

    private static IReadOnlyList<IReadOnlyList<string>> ReadCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
        var rows = new List<IReadOnlyList<string>>();
        while (reader.ReadLine() is { } line) rows.Add(ParseCsvLine(line));
        return rows;
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var values = new List<string>(); var buffer = new StringBuilder(); var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            if (line[index] == '"') { if (quoted && index + 1 < line.Length && line[index + 1] == '"') { buffer.Append('"'); index++; } else quoted = !quoted; }
            else if (line[index] == ',' && !quoted) { values.Add(buffer.ToString()); buffer.Clear(); }
            else buffer.Append(line[index]);
        }
        if (quoted) throw new InvalidDataException("CSV contains an unclosed quoted value.");
        values.Add(buffer.ToString()); return values;
    }

    private static int Column(string? reference)
    {
        var column = 0;
        foreach (var ch in (reference ?? "A1").TakeWhile(char.IsLetter)) column = column * 26 + char.ToUpperInvariant(ch) - 'A' + 1;
        return Math.Max(1, column);
    }
    private static RegionType TypeForDepth(int depth) => depth switch { 0 => RegionType.Country, 1 => RegionType.Province, 2 => RegionType.City, 3 => RegionType.District, 4 => RegionType.Jamoat, 5 => RegionType.Village, 6 => RegionType.Neighborhood, _ => RegionType.Neighborhood };
    private static string RegionKey(Guid? parentId, string normalizedRu) => $"{parentId?.ToString("N") ?? "root"}|{normalizedRu}";
    private static bool TryType(string value, out SchoolType type)
    {
        var normalized = EducationNormalization.Key(value);
        type = normalized switch
        {
            "school" or "школа" => SchoolType.GeneralSchool,
            "lyceum" or "лицей" => SchoolType.Lyceum,
            "gymnasium" or "гимназия" => SchoolType.Gymnasium,
            "private" or "частная" => SchoolType.PrivateSchool,
            "presidential" or "президентская" => SchoolType.PresidentialSchool,
            "college" or "колледж" => SchoolType.College,
            "other" or "другое" => SchoolType.Other,
            _ => (SchoolType)(-1)
        };
        return Enum.IsDefined(type);
    }
    private static Guid? ActorId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) ? id : null;
    private static Result<T> InvalidFile<T>(string detail) => Result<T>.ValidationFailure(new Dictionary<string, IReadOnlyList<Error>> { ["file"] = [Error.Validation("student.education_import.invalid", "Student.EducationImport.Invalid: " + detail)] });

    private sealed record ParsedImportRow(int RowNumber, string[] RegionPathRu, string[] RegionPathTg, string SchoolNameRu, string? SchoolNameTg,
        int? Number, SchoolType Type, string NormalizedNameRu, string? AddressText, bool IsDuplicate, IReadOnlyList<string> Errors)
    { public bool IsValid => Errors.Count == 0; }
    private sealed record RegionPathResult(Region Region, int CreatedCount);
}
