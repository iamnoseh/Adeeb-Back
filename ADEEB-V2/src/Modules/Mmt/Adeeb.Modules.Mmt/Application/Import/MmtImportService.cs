using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.IO.Compression;

namespace Adeeb.Modules.Mmt.Application.Import;

public sealed class MmtImportService(MmtDbContext db, IDateTimeProvider clock, MmtSpreadsheet spreadsheet)
{
    private const long MaxFileBytes = 5 * 1024 * 1024;
    private const long MaxExpandedBytes = 50 * 1024 * 1024;
    private const int MaxPackageEntries = 1000;
    public byte[] CreateTemplate() => spreadsheet.CreateTemplate();

    public async Task<Result<MmtImportPreviewResultDto>> PreviewAsync(MmtImportPreviewRequestDto request, CancellationToken ct)
    {
        if (!Enum.IsDefined(typeof(ExistingScoreMode), request.ExistingScoreMode)) return InvalidFile<MmtImportPreviewResultDto>("ExistingScoreMode is invalid.");
        var loaded = await LoadRowsAsync(request.File, request.AdmissionYear, ct); if (loaded.IsFailure) return Result<MmtImportPreviewResultDto>.ValidationFailure(loaded.ValidationErrors!);
        var rows = await AddDatabaseValidationAsync(loaded.Value!, request.CreateMissingReferences, (ExistingScoreMode)request.ExistingScoreMode, ct);
        return Result<MmtImportPreviewResultDto>.Success(Summarize(rows));
    }

    public async Task<Result<MmtImportResultDto>> ConfirmAsync(MmtImportConfirmRequestDto request, CancellationToken ct)
    {
        if (!Enum.IsDefined(typeof(ExistingScoreMode), request.ExistingScoreMode)) return InvalidFile<MmtImportResultDto>("ExistingScoreMode is invalid.");
        var mode = (ExistingScoreMode)request.ExistingScoreMode;
        var loaded = await LoadRowsAsync(request.File, request.AdmissionYear, ct); if (loaded.IsFailure) return Result<MmtImportResultDto>.ValidationFailure(loaded.ValidationErrors!);
        var rows = await AddDatabaseValidationAsync(loaded.Value!, request.CreateMissingReferences, mode, ct);
        if (mode == ExistingScoreMode.FailOnExisting && rows.Any(x => x.ValidationErrors.Contains("Passing score already exists."))) return Result<MmtImportResultDto>.Failure(MmtErrors.ImportExistingScore);

        IDbContextTransaction? transaction = null;
        if (db.Database.IsRelational()) transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var validValues = rows.Where(x => x.IsValid).Select(x => x.Values!).ToList();
            var clusterCodes = validValues.Select(x => x.ClusterCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var universityNames = validValues.Select(x => MmtNormalization.NameKey(x.UniversityFullName)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var specialtyCodes = validValues.Select(x => x.SpecialtyCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var years = validValues.Select(x => x.Year).Distinct().ToArray();
            var clusters = (await db.Clusters.Where(x => clusterCodes.Contains(x.Code)).ToListAsync(ct)).ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
            var universities = (await db.Universities.Where(x => universityNames.Contains(x.NormalizedFullName)).ToListAsync(ct)).ToDictionary(x => x.NormalizedFullName, StringComparer.OrdinalIgnoreCase);
            var specialties = (await db.Specialties.Where(x => specialtyCodes.Contains(x.Code)).ToListAsync(ct)).ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
            if (validValues.Any(row =>
                    (clusters.TryGetValue(row.ClusterCode, out var cluster) && !cluster.IsActive)
                    || (universities.TryGetValue(MmtNormalization.NameKey(row.UniversityFullName), out var university) && !university.IsActive)
                    || (specialties.TryGetValue(row.SpecialtyCode, out var specialty) && !specialty.IsActive)
                    || (!request.CreateMissingReferences && (!clusters.ContainsKey(row.ClusterCode) || !universities.ContainsKey(MmtNormalization.NameKey(row.UniversityFullName)) || !specialties.ContainsKey(row.SpecialtyCode)))))
            {
                if (transaction is not null) await transaction.RollbackAsync(ct);
                return Result<MmtImportResultDto>.Failure(MmtErrors.ImportConflict);
            }

            var clusterIds = clusters.Values.Select(x => x.Id).ToArray();
            var universityIds = universities.Values.Select(x => x.Id).ToArray();
            var specialtyIds = specialties.Values.Select(x => x.Id).ToArray();
            var loadedPrograms = await db.AdmissionPrograms
                .Where(x => clusterIds.Contains(x.MmtClusterId) && universityIds.Contains(x.UniversityId) && specialtyIds.Contains(x.SpecialtyId) && years.Contains(x.AdmissionYear))
                .ToListAsync(ct);
            var programGroups = loadedPrograms.GroupBy(ProgramKey).ToList();
            if (programGroups.Any(x => x.Count() > 1))
            {
                if (transaction is not null) await transaction.RollbackAsync(ct);
                return Result<MmtImportResultDto>.Failure(MmtErrors.ImportConflict);
            }
            var programs = programGroups.ToDictionary(x => x.Key, x => x.Single());
            var programIds = programs.Values.Select(x => x.Id).ToArray();
            var scores = (await db.PassingScores.Where(x => programIds.Contains(x.AdmissionProgramId) && years.Contains(x.Year)).ToListAsync(ct))
                .ToDictionary(x => ScoreKey(x.AdmissionProgramId, x.Year, x.DistributionRound));
            var importedPrograms = 0; var inserted = 0; var updated = 0; var skipped = 0;
            foreach (var preview in rows.Where(x => x.IsValid))
            {
                var row = preview.Values!;
                if (!clusters.TryGetValue(row.ClusterCode, out var cluster)) { cluster = new MmtCluster(Guid.NewGuid(), row.ClusterName, row.ClusterCode, null, clock.UtcNow); clusters.Add(row.ClusterCode, cluster); db.Clusters.Add(cluster); }
                if (!universities.TryGetValue(MmtNormalization.NameKey(row.UniversityFullName), out var university)) { university = new University(Guid.NewGuid(), row.UniversityFullName, row.UniversityShortName, row.UniversityCity, (UniversityType)row.UniversityType, null, clock.UtcNow); universities.Add(university.NormalizedFullName, university); db.Universities.Add(university); }
                if (!specialties.TryGetValue(row.SpecialtyCode, out var specialty)) { specialty = new Specialty(Guid.NewGuid(), row.SpecialtyCode, row.SpecialtyName, null, clock.UtcNow); specialties.Add(row.SpecialtyCode, specialty); db.Specialties.Add(specialty); }
                var key = ProgramKey(university.Id, specialty.Id, cluster.Id, row.AdmissionType, row.StudyForm, row.StudyLanguage, row.Year);
                if (!programs.TryGetValue(key, out var program))
                {
                    program = new AdmissionProgram(Guid.NewGuid(), university.Id, specialty.Id, cluster.Id,
                        (AdmissionType)row.AdmissionType, (StudyForm)row.StudyForm, (StudyLanguage)row.StudyLanguage,
                        row.Year, row.SeatsCount, row.UniversityCity, row.UniversityCity, null,
                        request.PublishAdmissionPrograms, clock.UtcNow);
                    programs.Add(key, program); db.AdmissionPrograms.Add(program); importedPrograms++;
                }
                else if (request.PublishAdmissionPrograms && !program.IsPublished) program.SetPublished(true, clock.UtcNow);
                var round = (DistributionRound)row.DistributionRound;
                var scoreKey = ScoreKey(program.Id, row.Year, round);
                scores.TryGetValue(scoreKey, out var score);
                if (score is null) { score = new PassingScoreHistory(Guid.NewGuid(), program.Id, row.Year, row.PassingScore, row.SeatsCount, row.Source, row.Note, clock.UtcNow, round); scores.Add(scoreKey, score); db.PassingScores.Add(score); inserted++; }
                else if (mode == ExistingScoreMode.UpdateExisting) { score.Update(row.Year, row.PassingScore, row.SeatsCount, row.Source, row.Note, clock.UtcNow, round); updated++; }
                else if (mode == ExistingScoreMode.FailOnExisting)
                {
                    if (transaction is not null) await transaction.RollbackAsync(ct);
                    return Result<MmtImportResultDto>.Failure(MmtErrors.ImportExistingScore);
                }
                else skipped++;
            }
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Result<MmtImportResultDto>.Success(new(rows.Count, importedPrograms, inserted, updated, skipped, rows.Count(x => !x.IsValid), rows));
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            if (transaction is not null) await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            return Result<MmtImportResultDto>.Failure(MmtErrors.ImportConflict);
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(ct);
            throw;
        }
        finally { if (transaction is not null) await transaction.DisposeAsync(); }
    }

    private async Task<Result<IReadOnlyList<MmtImportRowPreviewDto>>> LoadRowsAsync(IFormFile? file, int? defaultYear, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return InvalidFile<IReadOnlyList<MmtImportRowPreviewDto>>("File is required.");
        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) return InvalidFile<IReadOnlyList<MmtImportRowPreviewDto>>("Only .xlsx files are supported.");
        if (file.Length > MaxFileBytes) return InvalidFile<IReadOnlyList<MmtImportRowPreviewDto>>("File exceeds 5 MB.");
        try
        {
            await using var input = file.OpenReadStream(); await using var memory = new MemoryStream();
            var buffer = new byte[81920]; long total = 0; int read;
            while ((read = await input.ReadAsync(buffer, ct)) > 0) { total += read; if (total > MaxFileBytes) return InvalidFile<IReadOnlyList<MmtImportRowPreviewDto>>("File exceeds 5 MB."); await memory.WriteAsync(buffer.AsMemory(0, read), ct); }
            memory.Position = 0;
            ValidatePackage(memory);
            memory.Position = 0;
            return Result<IReadOnlyList<MmtImportRowPreviewDto>>.Success(spreadsheet.Parse(memory, defaultYear));
        }
        catch (Exception ex) when (ex is InvalidDataException or DocumentFormat.OpenXml.Packaging.OpenXmlPackageException or IOException)
        { return InvalidFile<IReadOnlyList<MmtImportRowPreviewDto>>("Workbook is malformed: " + ex.Message); }
    }

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

    private async Task<IReadOnlyList<MmtImportRowPreviewDto>> AddDatabaseValidationAsync(IReadOnlyList<MmtImportRowPreviewDto> source, bool createMissing, ExistingScoreMode mode, CancellationToken ct)
    {
        var values = source.Where(x => x.Values is not null).Select(x => x.Values!).ToList();
        var clusterCodes = values.Select(x => x.ClusterCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var universityNames = values.Select(x => MmtNormalization.NameKey(x.UniversityFullName)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var specialtyCodes = values.Select(x => x.SpecialtyCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var clusters = (await db.Clusters.AsNoTracking().Where(x => clusterCodes.Contains(x.Code)).ToListAsync(ct)).ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var universities = (await db.Universities.AsNoTracking().Where(x => universityNames.Contains(x.NormalizedFullName)).ToListAsync(ct)).ToDictionary(x => x.NormalizedFullName, StringComparer.OrdinalIgnoreCase);
        var specialties = (await db.Specialties.AsNoTracking().Where(x => specialtyCodes.Contains(x.Code)).ToListAsync(ct)).ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var clusterIds = clusters.Values.Select(x => x.Id).ToArray();
        var universityIds = universities.Values.Select(x => x.Id).ToArray();
        var specialtyIds = specialties.Values.Select(x => x.Id).ToArray();
        var years = values.Select(x => x.Year).Distinct().ToArray();
        var programs = await db.AdmissionPrograms.AsNoTracking()
            .Where(x => clusterIds.Contains(x.MmtClusterId) && universityIds.Contains(x.UniversityId) && specialtyIds.Contains(x.SpecialtyId) && years.Contains(x.AdmissionYear))
            .ToListAsync(ct);
        var programGroups = programs.GroupBy(ProgramKey, StringComparer.Ordinal).ToDictionary(x => x.Key, StringComparer.Ordinal);
        var programMap = programGroups.Where(x => x.Value.Count() == 1).ToDictionary(x => x.Key, x => x.Value.Single(), StringComparer.Ordinal);
        var programIds = programs.Select(x => x.Id).ToArray();
        HashSet<string> existingScores = mode == ExistingScoreMode.FailOnExisting
            ? (await db.PassingScores.AsNoTracking().Where(x => programIds.Contains(x.AdmissionProgramId) && years.Contains(x.Year)).Select(x => new { x.AdmissionProgramId, x.Year, x.DistributionRound }).ToListAsync(ct)).Select(x => ScoreKey(x.AdmissionProgramId, x.Year, x.DistributionRound)).ToHashSet(StringComparer.Ordinal)
            : [];
        var rows = new List<MmtImportRowPreviewDto>(source.Count);
        foreach (var item in source)
        {
            if (item.Values is null || item.ValidationErrors.Count > 0) { rows.Add(item); continue; }
            var r = item.Values; var errors = item.ValidationErrors.ToList();
            clusters.TryGetValue(r.ClusterCode, out var cluster);
            var universityKey = MmtNormalization.NameKey(r.UniversityFullName);
            universities.TryGetValue(universityKey, out var university);
            specialties.TryGetValue(r.SpecialtyCode, out var specialty);
            if (!createMissing) { if (cluster is null) errors.Add("Cluster does not exist."); if (university is null) errors.Add("University does not exist."); if (specialty is null) errors.Add("Specialty does not exist."); }
            if (cluster is { IsActive: false }) errors.Add("Cluster is inactive."); if (university is { IsActive: false }) errors.Add("University is inactive."); if (specialty is { IsActive: false }) errors.Add("Specialty is inactive.");
            if (cluster is not null && university is not null && specialty is not null)
            {
                var programKey = ProgramKey(university.Id, specialty.Id, cluster.Id, r.AdmissionType, r.StudyForm, r.StudyLanguage, r.Year);
                programMap.TryGetValue(programKey, out var program);
                if (programGroups.TryGetValue(programKey, out var candidates) && candidates.Count() > 1)
                    errors.Add("Admission program identity is ambiguous across study locations.");
                if (program is not null && mode == ExistingScoreMode.FailOnExisting && existingScores.Contains(ScoreKey(program.Id, r.Year, (DistributionRound)r.DistributionRound))) errors.Add("Passing score already exists.");
            }
            rows.Add(item with { IsValid = errors.Count == 0, ValidationErrors = errors });
        }
        return rows;
    }

    private static MmtImportPreviewResultDto Summarize(IReadOnlyList<MmtImportRowPreviewDto> rows) => new(rows.Count, rows.Count(x => x.IsValid), rows.Count(x => !x.IsValid), rows.Count(x => x.IsDuplicate), rows);
    private static string ProgramKey(AdmissionProgram x) => ProgramKey(x.UniversityId, x.SpecialtyId, x.MmtClusterId, (int)x.AdmissionType, (int)x.StudyForm, (int)x.StudyLanguage, x.AdmissionYear);
    private static string ProgramKey(Guid university, Guid specialty, Guid cluster, int type, int form, int language, int year) => $"{university:N}|{specialty:N}|{cluster:N}|{type}|{form}|{language}|{year}";
    private static string ScoreKey(Guid programId, int year, DistributionRound distributionRound) => $"{programId:N}|{year}|{(int)distributionRound}";
    private static Result<T> InvalidFile<T>(string message) => Result<T>.ValidationFailure(new Dictionary<string, IReadOnlyList<Adeeb.SharedKernel.Errors.Error>> { ["file"] = [Adeeb.SharedKernel.Errors.Error.Validation("mmt.import.file_invalid", message)] });
}
