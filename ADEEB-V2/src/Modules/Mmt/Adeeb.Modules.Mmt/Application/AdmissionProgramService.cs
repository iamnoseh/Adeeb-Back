using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.Mmt.Application;

public sealed class AdmissionProgramService(
    MmtDbContext db,
    IDateTimeProvider clock,
    IOptions<MmtOptions> options)
{
    private readonly MmtOptions options = options.Value;

    public async Task<Result<PagedResponse<AdmissionProgramListItemDto>>> GetProgramsAsync(AdmissionProgramFilter filter, bool admin, CancellationToken ct)
    {
        var validation = ValidateFilter(filter); if (validation is not null) return Result<PagedResponse<AdmissionProgramListItemDto>>.ValidationFailure(validation);
        var query = ApplyFilter(BaseQuery(), filter, admin);
        var page = Math.Max(1, filter.Page); var size = Math.Clamp(filter.PageSize, 1, 100);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.AdmissionYear).ThenBy(x => x.University.FullName).ThenBy(x => x.Specialty.Code)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return Result<PagedResponse<AdmissionProgramListItemDto>>.Success(new(rows.Select(ToListDto).ToList(), page, size, total));
    }

    public async Task<Result<AdmissionProgramDto>> GetProgramAsync(Guid id, bool admin, CancellationToken ct)
    {
        var query = BaseQuery();
        if (!admin) query = query.Where(x => x.IsActive && x.IsPublished && x.University.IsActive && x.Specialty.IsActive && x.MmtCluster.IsActive && x.AdmissionYear == CurrentAdmissionYear);
        var entity = await query.SingleOrDefaultAsync(x => x.Id == id, ct);
        return entity is null ? Result<AdmissionProgramDto>.Failure(MmtErrors.ProgramNotFound) : Result<AdmissionProgramDto>.Success(ToDetailsDto(entity));
    }

    public async Task<Result<AdmissionProgramDto>> CreateProgramAsync(CreateAdmissionProgramDto request, CancellationToken ct)
    {
        var validation = MmtValidation.ValidateProgram(request); if (validation.IsFailure) return Invalid<AdmissionProgramDto>(validation);
        var refs = await ReferencesActiveAsync(request.UniversityId, request.SpecialtyId, request.MmtClusterId, ct); if (!refs) return Result<AdmissionProgramDto>.Failure(MmtErrors.InactiveReference);
        if (await DuplicateAsync(null, request.UniversityId, request.SpecialtyId, request.MmtClusterId, request.AdmissionType, request.StudyForm, request.StudyLanguage, request.AdmissionYear, ct)) return Result<AdmissionProgramDto>.Failure(MmtErrors.DuplicateProgram);
        var entity = new AdmissionProgram(Guid.NewGuid(), request.UniversityId, request.SpecialtyId, request.MmtClusterId,
            (AdmissionType)request.AdmissionType, (StudyForm)request.StudyForm, (StudyLanguage)request.StudyLanguage,
            request.AdmissionYear, request.SeatsCount, request.IsPublished, clock.UtcNow);
        db.AdmissionPrograms.Add(entity);
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException ex) when (MmtDatabaseConstraints.IsUniqueViolation(ex, MmtDatabaseConstraints.ProgramIdentity)) { db.ChangeTracker.Clear(); return Result<AdmissionProgramDto>.Failure(MmtErrors.DuplicateProgram); }
        return await GetProgramAsync(entity.Id, true, ct);
    }

    public async Task<Result<AdmissionProgramDto>> UpdateProgramAsync(Guid id, UpdateAdmissionProgramDto request, CancellationToken ct)
    {
        var validation = MmtValidation.ValidateProgram(request); if (validation.IsFailure) return Invalid<AdmissionProgramDto>(validation);
        var entity = await db.AdmissionPrograms.SingleOrDefaultAsync(x => x.Id == id, ct); if (entity is null) return Result<AdmissionProgramDto>.Failure(MmtErrors.ProgramNotFound);
        if (!await ReferencesActiveAsync(request.UniversityId, request.SpecialtyId, request.MmtClusterId, ct)) return Result<AdmissionProgramDto>.Failure(MmtErrors.InactiveReference);
        if (await DuplicateAsync(id, request.UniversityId, request.SpecialtyId, request.MmtClusterId, request.AdmissionType, request.StudyForm, request.StudyLanguage, request.AdmissionYear, ct)) return Result<AdmissionProgramDto>.Failure(MmtErrors.DuplicateProgram);
        entity.Update(request.UniversityId, request.SpecialtyId, request.MmtClusterId, (AdmissionType)request.AdmissionType,
            (StudyForm)request.StudyForm, (StudyLanguage)request.StudyLanguage, request.AdmissionYear, request.SeatsCount,
            request.IsPublished, request.IsActive, clock.UtcNow);
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException ex) when (MmtDatabaseConstraints.IsUniqueViolation(ex, MmtDatabaseConstraints.ProgramIdentity)) { db.ChangeTracker.Clear(); return Result<AdmissionProgramDto>.Failure(MmtErrors.DuplicateProgram); }
        return await GetProgramAsync(id, true, ct);
    }

    public async Task<Result> SetStatusAsync(Guid id, bool active, CancellationToken ct)
    {
        var entity = await db.AdmissionPrograms.SingleOrDefaultAsync(x => x.Id == id, ct); if (entity is null) return Result.Failure(MmtErrors.ProgramNotFound);
        entity.SetActive(active, clock.UtcNow); await db.SaveChangesAsync(ct); return Result.Success();
    }

    public async Task<Result> SetPublishedAsync(Guid id, bool published, CancellationToken ct)
    {
        var entity = await db.AdmissionPrograms.SingleOrDefaultAsync(x => x.Id == id, ct); if (entity is null) return Result.Failure(MmtErrors.ProgramNotFound);
        if (published && (!entity.IsActive || !MmtValidation.IsYear(entity.AdmissionYear) || !await ReferencesActiveAsync(entity.UniversityId, entity.SpecialtyId, entity.MmtClusterId, ct))) return Result.Failure(MmtErrors.PublishInvalid);
        entity.SetPublished(published, clock.UtcNow); await db.SaveChangesAsync(ct); return Result.Success();
    }

    public async Task<Result<IReadOnlyList<PassingScoreHistoryDto>>> GetScoresAsync(Guid programId, CancellationToken ct)
    {
        if (!await db.AdmissionPrograms.AnyAsync(x => x.Id == programId, ct)) return Result<IReadOnlyList<PassingScoreHistoryDto>>.Failure(MmtErrors.ProgramNotFound);
        var scores = await db.PassingScores.AsNoTracking().Where(x => x.AdmissionProgramId == programId).OrderByDescending(x => x.Year).ToListAsync(ct);
        return Result<IReadOnlyList<PassingScoreHistoryDto>>.Success(scores.Select(ToDto).ToList());
    }

    public async Task<Result<PassingScoreHistoryDto>> AddScoreAsync(Guid programId, CreatePassingScoreHistoryDto request, CancellationToken ct)
    {
        var validation = MmtValidation.ValidateScore(request.Year, request.PassingScore, request.SeatsCount, request.Source, request.Note); if (validation.IsFailure) return Invalid<PassingScoreHistoryDto>(validation);
        if (!await db.AdmissionPrograms.AnyAsync(x => x.Id == programId, ct)) return Result<PassingScoreHistoryDto>.Failure(MmtErrors.ProgramNotFound);
        if (await db.PassingScores.AnyAsync(x => x.AdmissionProgramId == programId && x.Year == request.Year, ct)) return Result<PassingScoreHistoryDto>.Failure(MmtErrors.DuplicateScore);
        var entity = new PassingScoreHistory(Guid.NewGuid(), programId, request.Year, request.PassingScore, request.SeatsCount, request.Source, request.Note, clock.UtcNow);
        db.PassingScores.Add(entity);
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException ex) when (MmtDatabaseConstraints.IsUniqueViolation(ex, MmtDatabaseConstraints.ProgramYearScore)) { db.ChangeTracker.Clear(); return Result<PassingScoreHistoryDto>.Failure(MmtErrors.DuplicateScore); }
        return Result<PassingScoreHistoryDto>.Success(ToDto(entity));
    }

    public async Task<Result<PassingScoreHistoryDto>> UpdateScoreAsync(Guid id, UpdatePassingScoreHistoryDto request, CancellationToken ct)
    {
        var validation = MmtValidation.ValidateScore(request.Year, request.PassingScore, request.SeatsCount, request.Source, request.Note); if (validation.IsFailure) return Invalid<PassingScoreHistoryDto>(validation);
        var entity = await db.PassingScores.SingleOrDefaultAsync(x => x.Id == id, ct); if (entity is null) return Result<PassingScoreHistoryDto>.Failure(MmtErrors.ScoreNotFound);
        if (await db.PassingScores.AnyAsync(x => x.Id != id && x.AdmissionProgramId == entity.AdmissionProgramId && x.Year == request.Year, ct)) return Result<PassingScoreHistoryDto>.Failure(MmtErrors.DuplicateScore);
        entity.Update(request.Year, request.PassingScore, request.SeatsCount, request.Source, request.Note, clock.UtcNow);
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException ex) when (MmtDatabaseConstraints.IsUniqueViolation(ex, MmtDatabaseConstraints.ProgramYearScore)) { db.ChangeTracker.Clear(); return Result<PassingScoreHistoryDto>.Failure(MmtErrors.DuplicateScore); }
        return Result<PassingScoreHistoryDto>.Success(ToDto(entity));
    }

    public async Task<Result> DeleteScoreAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.PassingScores.SingleOrDefaultAsync(x => x.Id == id, ct); if (entity is null) return Result.Failure(MmtErrors.ScoreNotFound);
        db.PassingScores.Remove(entity); await db.SaveChangesAsync(ct); return Result.Success();
    }

    public async Task<Result<PassingScoreAnalyticsDto>> GetScoreAnalyticsAsync(Guid programId, CancellationToken ct)
    {
        if (!await db.AdmissionPrograms.AnyAsync(x => x.Id == programId, ct)) return Result<PassingScoreAnalyticsDto>.Failure(MmtErrors.ProgramNotFound);
        var values = await db.PassingScores.AsNoTracking().Where(x => x.AdmissionProgramId == programId).OrderByDescending(x => x.Year).Select(x => x.PassingScore).Take(3).ToListAsync(ct);
        return Result<PassingScoreAnalyticsDto>.Success(Analytics(values));
    }

    public static PassingScoreAnalyticsDto Analytics(IReadOnlyList<decimal> newestFirst)
    {
        if (newestFirst.Count == 0) return new(null, null, null);
        var latest = newestFirst[0]; var average = decimal.Round(newestFirst.Take(3).Average(), 2, MidpointRounding.AwayFromZero);
        return new(latest, average, Math.Max(latest, average));
    }

    private IQueryable<AdmissionProgram> BaseQuery() => db.AdmissionPrograms.AsNoTracking().Include(x => x.University).Include(x => x.Specialty).Include(x => x.MmtCluster).Include(x => x.PassingScores);
    private IQueryable<AdmissionProgram> ApplyFilter(IQueryable<AdmissionProgram> q, AdmissionProgramFilter f, bool admin)
    {
        if (!admin) q = q.Where(x => x.IsActive && x.IsPublished && x.University.IsActive && x.Specialty.IsActive && x.MmtCluster.IsActive && x.AdmissionYear == CurrentAdmissionYear);
        else if (f.AdmissionYear.HasValue) q = q.Where(x => x.AdmissionYear == f.AdmissionYear);
        if (f.ClusterId.HasValue) q = q.Where(x => x.MmtClusterId == f.ClusterId);
        if (f.UniversityId.HasValue) q = q.Where(x => x.UniversityId == f.UniversityId);
        if (f.SpecialtyId.HasValue) q = q.Where(x => x.SpecialtyId == f.SpecialtyId);
        if (f.AdmissionType.HasValue) q = q.Where(x => (int)x.AdmissionType == f.AdmissionType);
        if (f.StudyForm.HasValue) q = q.Where(x => (int)x.StudyForm == f.StudyForm);
        if (f.StudyLanguage.HasValue) q = q.Where(x => (int)x.StudyLanguage == f.StudyLanguage);
        if (admin && f.IsPublished.HasValue) q = q.Where(x => x.IsPublished == f.IsPublished);
        if (admin && f.IsActive.HasValue) q = q.Where(x => x.IsActive == f.IsActive);
        if (!string.IsNullOrWhiteSpace(f.Search)) { var s = f.Search.Trim().ToLower(); q = q.Where(x => x.University.FullName.ToLower().Contains(s) || x.Specialty.Name.ToLower().Contains(s) || x.Specialty.Code.ToLower().Contains(s)); }
        return q;
    }
    private static IReadOnlyDictionary<string, IReadOnlyList<Adeeb.SharedKernel.Errors.Error>>? ValidateFilter(AdmissionProgramFilter f)
    {
        var errors = new Dictionary<string, IReadOnlyList<Adeeb.SharedKernel.Errors.Error>>();
        if (f.AdmissionYear.HasValue && !MmtValidation.IsYear(f.AdmissionYear.Value)) errors["admissionYear"] = [Adeeb.SharedKernel.Errors.Error.Validation("mmt.year.invalid", "MMT.YearInvalid")];
        if (f.AdmissionType.HasValue && !Enum.IsDefined(typeof(AdmissionType), f.AdmissionType.Value)) errors["admissionType"] = [Adeeb.SharedKernel.Errors.Error.Validation("mmt.admission_type.invalid", "MMT.EnumInvalid")];
        if (f.StudyForm.HasValue && !Enum.IsDefined(typeof(StudyForm), f.StudyForm.Value)) errors["studyForm"] = [Adeeb.SharedKernel.Errors.Error.Validation("mmt.study_form.invalid", "MMT.EnumInvalid")];
        if (f.StudyLanguage.HasValue && !Enum.IsDefined(typeof(StudyLanguage), f.StudyLanguage.Value)) errors["studyLanguage"] = [Adeeb.SharedKernel.Errors.Error.Validation("mmt.study_language.invalid", "MMT.EnumInvalid")];
        return errors.Count == 0 ? null : errors;
    }
    private async Task<bool> ReferencesActiveAsync(Guid university, Guid specialty, Guid cluster, CancellationToken ct) =>
        await db.Universities.AnyAsync(x => x.Id == university && x.IsActive, ct) && await db.Specialties.AnyAsync(x => x.Id == specialty && x.IsActive, ct) && await db.Clusters.AnyAsync(x => x.Id == cluster && x.IsActive, ct);
    private Task<bool> DuplicateAsync(Guid? id, Guid university, Guid specialty, Guid cluster, int type, int form, int language, int year, CancellationToken ct) =>
        db.AdmissionPrograms.AnyAsync(x => (!id.HasValue || x.Id != id) && x.UniversityId == university && x.SpecialtyId == specialty && x.MmtClusterId == cluster && (int)x.AdmissionType == type && (int)x.StudyForm == form && (int)x.StudyLanguage == language && x.AdmissionYear == year, ct);
    private int CurrentAdmissionYear => options.CurrentAdmissionYear ?? clock.UtcNow.Year;
    private static Result<T> Invalid<T>(Result validation) => Result<T>.ValidationFailure(validation.ValidationErrors!);
    private static PassingScoreHistoryDto ToDto(PassingScoreHistory x) => new(x.Id, x.AdmissionProgramId, x.Year, x.PassingScore, x.SeatsCount, x.Source, x.Note, x.CreatedAtUtc, x.UpdatedAtUtc);
    private static AdmissionProgramListItemDto ToListDto(AdmissionProgram x) => new(x.Id, x.UniversityId, x.University.FullName, x.SpecialtyId, x.Specialty.Code, x.Specialty.Name, x.MmtClusterId, x.MmtCluster.Code, x.MmtCluster.Name, (int)x.AdmissionType, (int)x.StudyForm, (int)x.StudyLanguage, x.AdmissionYear, x.SeatsCount, x.IsPublished, x.IsActive, x.PassingScores.OrderByDescending(s => s.Year).Select(s => (decimal?)s.PassingScore).FirstOrDefault());
    private static AdmissionProgramDto ToDetailsDto(AdmissionProgram x)
    {
        var analytics = Analytics(x.PassingScores.OrderByDescending(s => s.Year).Select(s => s.PassingScore).Take(3).ToList());
        return new(x.Id, MmtCatalogService.ToDto(x.University), MmtCatalogService.ToDto(x.Specialty), MmtCatalogService.ToDto(x.MmtCluster), (int)x.AdmissionType, (int)x.StudyForm, (int)x.StudyLanguage, x.AdmissionYear, x.SeatsCount, x.IsPublished, x.IsActive, analytics.LatestPassingScore, analytics.AverageLast3Years, analytics.ConservativeThreshold, x.CreatedAtUtc, x.UpdatedAtUtc);
    }
}
