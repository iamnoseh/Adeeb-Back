using System.IO.Compression;
using System.Text.Json;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Adeeb.Modules.Mmt.Application.Import;

public sealed class MmtCatalogImportService(MmtDbContext db, IDateTimeProvider clock, MmtCatalogSpreadsheet spreadsheet)
{
    private const long MaxFileBytes = 5 * 1024 * 1024;
    private const long MaxExpandedBytes = 50 * 1024 * 1024;
    private const int MaxPackageEntries = 1000;

    public byte[] CreateTemplate() => spreadsheet.CreateTemplate();

    public async Task<Result<MmtCatalogImportPreviewResultDto>> PreviewAsync(MmtCatalogImportRequestDto request, CancellationToken ct)
    {
        var validation = await ValidateRequestAsync<MmtCatalogImportPreviewResultDto>(request, ct);
        if (validation is not null) return validation;
        var loaded = await LoadRowsAsync(request.File, ct);
        if (loaded.IsFailure) return Result<MmtCatalogImportPreviewResultDto>.ValidationFailure(loaded.ValidationErrors!);
        var overrides = ParseOverrides<MmtCatalogImportPreviewResultDto>(request.UniversityTypeOverridesJson);
        if (overrides.IsFailure) return Result<MmtCatalogImportPreviewResultDto>.ValidationFailure(overrides.ValidationErrors!);
        var rows = await EnrichAsync(loaded.Value!, request.MmtClusterId, request.AdmissionYear, ct);
        return Result<MmtCatalogImportPreviewResultDto>.Success(Summarize(rows, request.DefaultUniversityType, overrides.Value!));
    }

    public async Task<Result<MmtCatalogImportResultDto>> ConfirmAsync(MmtCatalogImportRequestDto request, CancellationToken ct)
    {
        var validation = await ValidateRequestAsync<MmtCatalogImportResultDto>(request, ct);
        if (validation is not null) return validation;
        var loaded = await LoadRowsAsync(request.File, ct);
        if (loaded.IsFailure) return Result<MmtCatalogImportResultDto>.ValidationFailure(loaded.ValidationErrors!);
        var overrides = ParseOverrides<MmtCatalogImportResultDto>(request.UniversityTypeOverridesJson);
        if (overrides.IsFailure) return Result<MmtCatalogImportResultDto>.ValidationFailure(overrides.ValidationErrors!);
        var rows = await EnrichAsync(loaded.Value!, request.MmtClusterId, request.AdmissionYear, ct);
        if (rows.Any(x => !x.IsValid)) return Result<MmtCatalogImportResultDto>.Failure(MmtErrors.ImportFileInvalid);

        IDbContextTransaction? transaction = null;
        if (db.Database.IsRelational()) transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var values = rows.Select(x => x.Values!).ToList();
            var universityKeys = values.Select(x => MmtNormalization.NameKey(x.UniversityNameRu)).Distinct().ToArray();
            var specialtyCodes = values.Select(x => x.SpecialtyCode).Distinct().ToArray();
            var universities = (await db.Universities.Where(x => universityKeys.Contains(x.NormalizedFullNameRu)).ToListAsync(ct))
                .ToDictionary(x => x.NormalizedFullNameRu, StringComparer.Ordinal);
            var specialties = (await db.Specialties.Where(x => specialtyCodes.Contains(x.Code)).ToListAsync(ct))
                .ToDictionary(x => x.Code, StringComparer.Ordinal);
            var programs = await db.AdmissionPrograms
                .Include(x => x.University).Include(x => x.Specialty)
                .Where(x => x.MmtClusterId == request.MmtClusterId && x.AdmissionYear == request.AdmissionYear)
                .ToListAsync(ct);
            var programKeys = programs.ToDictionary(ProgramKey, StringComparer.Ordinal);
            var overrideTypes = overrides.Value!.ToDictionary(x => MmtNormalization.NameKey(x.UniversityNameRu), x => (UniversityType)x.UniversityType, StringComparer.Ordinal);

            var createdUniversities = 0;
            var createdSpecialties = 0;
            var importedPrograms = 0;
            var skippedPrograms = 0;
            foreach (var row in values)
            {
                var universityKey = MmtNormalization.NameKey(row.UniversityNameRu);
                if (!universities.TryGetValue(universityKey, out var university))
                {
                    var type = overrideTypes.GetValueOrDefault(universityKey, (UniversityType)request.DefaultUniversityType);
                    university = University.CreateRussianOnly(Guid.NewGuid(), row.UniversityNameRu, row.StudyLocationRu, type, clock.UtcNow);
                    db.Universities.Add(university);
                    universities.Add(universityKey, university);
                    createdUniversities++;
                }

                if (!specialties.TryGetValue(row.SpecialtyCode, out var specialty))
                {
                    specialty = Specialty.CreateRussianOnly(Guid.NewGuid(), row.SpecialtyCode, row.SpecialtyNameRu, clock.UtcNow);
                    db.Specialties.Add(specialty);
                    specialties.Add(row.SpecialtyCode, specialty);
                    createdSpecialties++;
                }

                var key = ProgramKey(university.Id, specialty.Id, request.MmtClusterId, row, request.AdmissionYear);
                if (programKeys.ContainsKey(key))
                {
                    skippedPrograms++;
                    continue;
                }

                var program = new AdmissionProgram(Guid.NewGuid(), university.Id, specialty.Id, request.MmtClusterId,
                    (AdmissionType)row.AdmissionType, (StudyForm)row.StudyForm, (StudyLanguage)row.StudyLanguage,
                    request.AdmissionYear, row.SeatsCount, null, row.StudyLocationRu, row.TuitionFeeTjs, false, clock.UtcNow);
                db.AdmissionPrograms.Add(program);
                programKeys.Add(key, program);
                importedPrograms++;
            }

            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Result<MmtCatalogImportResultDto>.Success(new(rows.Count, importedPrograms, skippedPrograms,
                createdUniversities, createdSpecialties, 0));
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            if (transaction is not null) await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            return Result<MmtCatalogImportResultDto>.Failure(MmtErrors.ImportConflict);
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private async Task<Result<T>?> ValidateRequestAsync<T>(MmtCatalogImportRequestDto request, CancellationToken ct)
    {
        var errors = new Dictionary<string, IReadOnlyList<Error>>();
        if (request.MmtClusterId == Guid.Empty || !await db.Clusters.AnyAsync(x => x.Id == request.MmtClusterId && x.IsActive, ct))
            errors["mmtClusterId"] = [Error.Validation("mmt.cluster.invalid", "MMT.ClusterNotFound")];
        if (!MmtValidation.IsYear(request.AdmissionYear))
            errors["admissionYear"] = [Error.Validation("mmt.year.invalid", "MMT.YearInvalid")];
        if (!Enum.IsDefined(typeof(UniversityType), request.DefaultUniversityType))
            errors["defaultUniversityType"] = [Error.Validation("mmt.university_type.invalid", "MMT.EnumInvalid")];
        return errors.Count == 0 ? null : Result<T>.ValidationFailure(errors);
    }

    private async Task<Result<IReadOnlyList<MmtCatalogImportRowPreviewDto>>> LoadRowsAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return InvalidFile<IReadOnlyList<MmtCatalogImportRowPreviewDto>>("File is required.");
        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) return InvalidFile<IReadOnlyList<MmtCatalogImportRowPreviewDto>>("Only .xlsx files are supported.");
        if (file.Length > MaxFileBytes) return InvalidFile<IReadOnlyList<MmtCatalogImportRowPreviewDto>>("File exceeds 5 MB.");
        try
        {
            await using var input = file.OpenReadStream();
            await using var memory = new MemoryStream();
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await input.ReadAsync(buffer, ct)) > 0)
            {
                total += read;
                if (total > MaxFileBytes) return InvalidFile<IReadOnlyList<MmtCatalogImportRowPreviewDto>>("File exceeds 5 MB.");
                await memory.WriteAsync(buffer.AsMemory(0, read), ct);
            }
            memory.Position = 0;
            ValidatePackage(memory);
            memory.Position = 0;
            return Result<IReadOnlyList<MmtCatalogImportRowPreviewDto>>.Success(spreadsheet.Parse(memory));
        }
        catch (Exception ex) when (ex is InvalidDataException or OpenXmlPackageException or IOException)
        {
            return InvalidFile<IReadOnlyList<MmtCatalogImportRowPreviewDto>>("Workbook is malformed: " + ex.Message);
        }
    }

    private async Task<IReadOnlyList<MmtCatalogImportRowPreviewDto>> EnrichAsync(
        IReadOnlyList<MmtCatalogImportRowPreviewDto> source, Guid clusterId, int admissionYear, CancellationToken ct)
    {
        var values = source.Where(x => x.Values is not null).Select(x => x.Values!).ToList();
        var universityKeys = values.Select(x => MmtNormalization.NameKey(x.UniversityNameRu)).Distinct().ToArray();
        var specialtyCodes = values.Select(x => x.SpecialtyCode).Distinct().ToArray();
        var universities = await db.Universities.AsNoTracking().Where(x => universityKeys.Contains(x.NormalizedFullNameRu)).ToListAsync(ct);
        var specialties = await db.Specialties.AsNoTracking().Where(x => specialtyCodes.Contains(x.Code)).ToListAsync(ct);
        var universityMap = universities.ToDictionary(x => x.NormalizedFullNameRu, StringComparer.Ordinal);
        var specialtyMap = specialties.ToDictionary(x => x.Code, StringComparer.Ordinal);
        var programs = await db.AdmissionPrograms.AsNoTracking().Include(x => x.University).Include(x => x.Specialty)
            .Where(x => x.MmtClusterId == clusterId && x.AdmissionYear == admissionYear).ToListAsync(ct);
        var existingPrograms = programs.ToDictionary(ProgramKey, StringComparer.Ordinal);

        return source.Select(item =>
        {
            if (item.Values is null) return item;
            var value = item.Values;
            universityMap.TryGetValue(MmtNormalization.NameKey(value.UniversityNameRu), out var university);
            specialtyMap.TryGetValue(value.SpecialtyCode, out var specialty);
            var key = university is null || specialty is null ? null : ProgramKey(university.Id, specialty.Id, clusterId, value, admissionYear);
            AdmissionProgram? existingProgram = null;
            var existing = key is not null && existingPrograms.TryGetValue(key, out existingProgram);
            var warnings = item.Warnings.ToList();
            if (university is not null && !string.Equals(university.CityRu, value.StudyLocationRu, StringComparison.OrdinalIgnoreCase))
                warnings.Add("University city differs from this program study location.");
            if (specialty is not null && !string.Equals(specialty.NameRu, value.SpecialtyNameRu, StringComparison.OrdinalIgnoreCase))
                warnings.Add("Existing specialty Russian name differs; existing translation will be kept.");
            if (existing && existingProgram!.SeatsCount != value.SeatsCount)
                warnings.Add($"Existing seats count is {existingProgram.SeatsCount?.ToString() ?? "empty"}; imported value {value.SeatsCount} will be skipped.");
            if (existing && existingProgram!.TuitionFeeTjs != value.TuitionFeeTjs)
                warnings.Add($"Existing tuition fee is {existingProgram.TuitionFeeTjs?.ToString() ?? "empty"}; imported value {value.TuitionFeeTjs?.ToString() ?? "empty"} will be skipped.");
            return item with { IsExisting = existing, Warnings = warnings };
        }).ToList();
    }

    private static MmtCatalogImportPreviewResultDto Summarize(IReadOnlyList<MmtCatalogImportRowPreviewDto> rows,
        int defaultUniversityType, IReadOnlyList<MmtCatalogUniversityTypeOverrideDto> overrides)
    {
        var overrideMap = overrides.ToDictionary(x => MmtNormalization.NameKey(x.UniversityNameRu), x => x.UniversityType, StringComparer.Ordinal);
        var universities = rows.Where(x => x.Values is not null).Select(x => x.Values!.UniversityNameRu)
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)
            .Select(name => new MmtCatalogImportUniversityDto(name,
                overrideMap.GetValueOrDefault(MmtNormalization.NameKey(name), defaultUniversityType),
                rows.Any(x => x.Values is not null && string.Equals(x.Values.UniversityNameRu, name, StringComparison.OrdinalIgnoreCase) && x.IsExisting)))
            .ToList();
        return new(rows.Count, rows.Count(x => x.IsValid), rows.Count(x => !x.IsValid),
            rows.Count(x => x.IsValid && !x.IsExisting), rows.Count(x => x.IsExisting),
            rows.Count(x => x.IsValid && x.NeedsTranslation), universities, rows);
    }

    private static Result<IReadOnlyList<MmtCatalogUniversityTypeOverrideDto>> ParseOverrides<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Result<IReadOnlyList<MmtCatalogUniversityTypeOverrideDto>>.Success([]);
        try
        {
            var values = JsonSerializer.Deserialize<List<MmtCatalogUniversityTypeOverrideDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            if (values.Any(x => string.IsNullOrWhiteSpace(x.UniversityNameRu) || !Enum.IsDefined(typeof(UniversityType), x.UniversityType))
                || values.Select(x => MmtNormalization.NameKey(x.UniversityNameRu)).Distinct().Count() != values.Count)
                return InvalidFile<IReadOnlyList<MmtCatalogUniversityTypeOverrideDto>>("University type overrides are invalid.");
            return Result<IReadOnlyList<MmtCatalogUniversityTypeOverrideDto>>.Success(values);
        }
        catch (JsonException)
        {
            return InvalidFile<IReadOnlyList<MmtCatalogUniversityTypeOverrideDto>>("UniversityTypeOverridesJson is invalid JSON.");
        }
    }

    private static string ProgramKey(AdmissionProgram program) => ProgramKey(program.UniversityId, program.SpecialtyId,
        program.MmtClusterId, (int)program.AdmissionType, (int)program.StudyForm, (int)program.StudyLanguage,
        program.AdmissionYear, program.NormalizedStudyLocation);

    private static string ProgramKey(Guid universityId, Guid specialtyId, Guid clusterId,
        MmtCatalogImportNormalizedRowDto row, int year) => ProgramKey(universityId, specialtyId, clusterId,
        row.AdmissionType, row.StudyForm, row.StudyLanguage, year, MmtNormalization.NameKey(row.StudyLocationRu));

    private static string ProgramKey(Guid universityId, Guid specialtyId, Guid clusterId, int admissionType,
        int studyForm, int studyLanguage, int year, string normalizedLocation) =>
        $"{universityId:N}|{specialtyId:N}|{clusterId:N}|{admissionType}|{studyForm}|{studyLanguage}|{year}|{normalizedLocation}";

    private static void ValidatePackage(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count > MaxPackageEntries) throw new InvalidDataException($"Workbook contains more than {MaxPackageEntries} package entries.");
        long expanded = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.Length > MaxExpandedBytes - expanded) throw new InvalidDataException("Expanded workbook exceeds 50 MB.");
            expanded += entry.Length;
        }
    }

    private static Result<T> InvalidFile<T>(string message) => Result<T>.ValidationFailure(new Dictionary<string, IReadOnlyList<Error>>
    {
        ["file"] = [Error.Validation("mmt.import_file_invalid", message)]
    });
}
