using System.Data;
using System.Security.Claims;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.Mmt.Application;

public sealed class MmtSimulatorService(
    MmtDbContext db,
    IDateTimeProvider clock,
    IOptions<MmtOptions> options)
{
    private readonly MmtOptions options = options.Value;

    public async Task<Result<StudentMmtProfileDto>> GetCurrentProfileAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (!userId.HasValue) return Result<StudentMmtProfileDto>.Failure(MmtErrors.UserRequired);
        var profile = await ProfileQuery().SingleOrDefaultAsync(x => x.UserId == userId && x.AdmissionYear == CurrentAdmissionYear && x.IsActive, ct);
        return profile is null ? Result<StudentMmtProfileDto>.Failure(MmtErrors.StudentProfileNotFound) : Result<StudentMmtProfileDto>.Success(ToProfileDto(profile));
    }

    public async Task<Result<StudentMmtProfileDto>> UpsertProfileAsync(ClaimsPrincipal principal, UpsertStudentMmtProfileDto request, CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (!userId.HasValue) return Result<StudentMmtProfileDto>.Failure(MmtErrors.UserRequired);
        var year = CurrentAdmissionYear;
        if (request.AdmissionYear.HasValue && request.AdmissionYear.Value != year) return Result<StudentMmtProfileDto>.Failure(MmtErrors.AdmissionYearUnavailable);

        await using var transaction = await BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var profile = await db.StudentProfiles.Include(x => x.Choices)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.AdmissionYear == year && x.IsActive, ct);
        if (profile is not null && profile.MmtClusterId != request.MmtClusterId)
            return Result<StudentMmtProfileDto>.Failure(MmtErrors.ClusterLocked);

        var cluster = await db.Clusters.SingleOrDefaultAsync(x => x.Id == request.MmtClusterId && x.IsActive, ct);
        if (cluster is null) return Result<StudentMmtProfileDto>.Failure(MmtErrors.InactiveReference);
        if (request.GoalAdmissionProgramId.HasValue && !await ProgramMatchesProfileAsync(request.GoalAdmissionProgramId.Value, request.MmtClusterId, year, ct))
            return Result<StudentMmtProfileDto>.Failure(MmtErrors.GoalProgramInvalid);

        var now = clock.UtcNow;
        if (profile is null)
        {
            profile = new StudentMmtProfile(Guid.NewGuid(), userId.Value, request.MmtClusterId, year, request.GoalAdmissionProgramId, now);
            db.StudentProfiles.Add(profile);
        }
        else
        {
            profile.Update(request.MmtClusterId, year, request.GoalAdmissionProgramId, now);
        }

        try
        {
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (MmtDatabaseConstraints.IsUniqueViolation(ex, MmtDatabaseConstraints.ActiveStudentProfile))
        {
            if (transaction is not null) await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            return Result<StudentMmtProfileDto>.Failure(MmtErrors.ProfileConflict);
        }

        db.ChangeTracker.Clear();
        return Result<StudentMmtProfileDto>.Success(ToProfileDto(await ProfileQuery().SingleAsync(x => x.Id == profile.Id, ct)));
    }

    public async Task<Result<IReadOnlyList<StudentAdmissionChoiceDto>>> GetCurrentChoicesAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (!userId.HasValue) return Result<IReadOnlyList<StudentAdmissionChoiceDto>>.Failure(MmtErrors.UserRequired);
        var profileId = await db.StudentProfiles.AsNoTracking()
            .Where(x => x.UserId == userId && x.AdmissionYear == CurrentAdmissionYear && x.IsActive)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (!profileId.HasValue) return Result<IReadOnlyList<StudentAdmissionChoiceDto>>.Failure(MmtErrors.StudentProfileNotFound);
        return Result<IReadOnlyList<StudentAdmissionChoiceDto>>.Success(await ChoiceDtosAsync(profileId.Value, ct));
    }

    public async Task<Result<IReadOnlyList<StudentAdmissionChoiceDto>>> ReplaceChoicesAsync(
        ClaimsPrincipal principal,
        UpsertAdmissionChoicesDto request,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (!userId.HasValue) return Result<IReadOnlyList<StudentAdmissionChoiceDto>>.Failure(MmtErrors.UserRequired);
        var validationError = ValidateChoices(request.Choices);
        if (validationError is not null) return Result<IReadOnlyList<StudentAdmissionChoiceDto>>.Failure(validationError);

        await using var transaction = await BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var profile = await db.StudentProfiles.SingleOrDefaultAsync(x => x.UserId == userId && x.AdmissionYear == CurrentAdmissionYear && x.IsActive, ct);
        if (profile is null) return Result<IReadOnlyList<StudentAdmissionChoiceDto>>.Failure(MmtErrors.StudentProfileNotFound);
        var programIds = request.Choices.Select(x => x.AdmissionProgramId).ToArray();
        var validPrograms = await db.AdmissionPrograms.AsNoTracking()
            .Where(x => programIds.Contains(x.Id) && x.IsActive && x.IsPublished && x.MmtClusterId == profile.MmtClusterId && x.AdmissionYear == profile.AdmissionYear)
            .Select(x => x.Id).ToListAsync(ct);
        if (validPrograms.Count != programIds.Length) return Result<IReadOnlyList<StudentAdmissionChoiceDto>>.Failure(MmtErrors.ChoiceProgramInvalid);

        var existing = await db.StudentAdmissionChoices.Where(x => x.StudentMmtProfileId == profile.Id).ToListAsync(ct);
        db.StudentAdmissionChoices.RemoveRange(existing);
        await db.SaveChangesAsync(ct);
        var now = clock.UtcNow;
        db.StudentAdmissionChoices.AddRange(request.Choices.Select(x =>
            new StudentAdmissionChoice(Guid.NewGuid(), profile.Id, x.AdmissionProgramId, x.PriorityOrder, now)));
        try
        {
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (
            MmtDatabaseConstraints.IsUniqueViolation(ex, MmtDatabaseConstraints.ChoicePriority)
            || MmtDatabaseConstraints.IsUniqueViolation(ex, MmtDatabaseConstraints.ChoiceProgram))
        {
            if (transaction is not null) await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            return Result<IReadOnlyList<StudentAdmissionChoiceDto>>.Failure(MmtErrors.ChoiceUpdateConflict);
        }

        db.ChangeTracker.Clear();
        return Result<IReadOnlyList<StudentAdmissionChoiceDto>>.Success(await ChoiceDtosAsync(profile.Id, ct));
    }

    public async Task<Result<MmtEvaluationDto>> SimulateAsync(ClaimsPrincipal principal, SimulateMmtEvaluationDto request, CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (!userId.HasValue) return Result<MmtEvaluationDto>.Failure(MmtErrors.UserRequired);
        if (request.TotalScore < 0 || request.TotalScore > 1000 || MmtValidation.Scale(request.TotalScore) > 2)
            return Result<MmtEvaluationDto>.Failure(MmtErrors.EvaluationScoreInvalid);

        await using var transaction = await BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var profile = await db.StudentProfiles.AsNoTracking().Include(x => x.MmtCluster)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.AdmissionYear == CurrentAdmissionYear && x.IsActive, ct);
        if (profile is null) return Result<MmtEvaluationDto>.Failure(MmtErrors.StudentProfileNotFound);
        var choices = await db.StudentAdmissionChoices.AsNoTracking()
            .Where(x => x.StudentMmtProfileId == profile.Id)
            .Include(x => x.AdmissionProgram).ThenInclude(x => x.University)
            .Include(x => x.AdmissionProgram).ThenInclude(x => x.Specialty)
            .Include(x => x.AdmissionProgram).ThenInclude(x => x.MmtCluster)
            .Include(x => x.AdmissionProgram).ThenInclude(x => x.PassingScores)
            .OrderBy(x => x.PriorityOrder)
            .AsSplitQuery()
            .ToListAsync(ct);
        if (choices.Count == 0) return Result<MmtEvaluationDto>.Failure(MmtErrors.ChoicesRequired);
        if (choices.Any(x => !x.AdmissionProgram.IsActive || !x.AdmissionProgram.IsPublished
            || x.AdmissionProgram.MmtClusterId != profile.MmtClusterId
            || x.AdmissionProgram.AdmissionYear != profile.AdmissionYear))
            return Result<MmtEvaluationDto>.Failure(MmtErrors.ChoiceProgramInvalid);

        var calculations = choices.Select(choice => Calculate(choice, request.TotalScore)).ToList();
        var accepted = calculations.FirstOrDefault(x => x.Threshold.HasValue && request.TotalScore >= x.Threshold.Value);
        var goalProgramId = profile.GoalAdmissionProgramId ?? choices[0].AdmissionProgramId;
        var goal = calculations.FirstOrDefault(x => x.Choice.AdmissionProgramId == goalProgramId);
        decimal? goalThreshold = goal?.Threshold;
        if (goal is null && profile.GoalAdmissionProgramId.HasValue)
        {
            var goalProgram = await db.AdmissionPrograms.AsNoTracking().Include(x => x.PassingScores)
                .SingleOrDefaultAsync(x => x.Id == profile.GoalAdmissionProgramId, ct);
            goalThreshold = goalProgram is null ? null : Threshold(goalProgram.PassingScores).Threshold;
        }

        decimal? goalMissing = goalThreshold.HasValue ? decimal.Max(0, goalThreshold.Value - request.TotalScore) : null;
        decimal? readiness = goalThreshold is > 0 ? decimal.Round(decimal.Min(100, request.TotalScore / goalThreshold.Value * 100), 2, MidpointRounding.AwayFromZero) : null;
        var messageKey = MotivationalMessage(accepted, calculations, request.TotalScore);
        var now = clock.UtcNow;
        var evaluationId = Guid.NewGuid();
        var evaluation = new MmtExamEvaluation(
            evaluationId, userId.Value, profile.Id, null, request.TotalScore, profile.AdmissionYear,
            profile.MmtClusterId, now, accepted?.Choice.PriorityOrder, accepted?.Choice.AdmissionProgramId,
            goalMissing, readiness, messageKey, now);
        var snapshots = calculations.Select(x => new MmtAdmissionChoiceSnapshot(
            Guid.NewGuid(), evaluationId, x.Choice.PriorityOrder, x.Choice.AdmissionProgramId,
            x.Choice.AdmissionProgram.University.FullNameFor(MmtCatalogService.CurrentLanguage), x.Choice.AdmissionProgram.Specialty.Code,
            x.Choice.AdmissionProgram.Specialty.NameFor(MmtCatalogService.CurrentLanguage), x.Choice.AdmissionProgram.MmtCluster.Code,
            x.Choice.AdmissionProgram.AdmissionType, x.Choice.AdmissionProgram.StudyForm,
            x.Choice.AdmissionProgram.StudyLanguage, x.Choice.AdmissionProgram.AdmissionYear,
            x.Latest, x.Threshold, request.TotalScore,
            accepted?.Choice.Id == x.Choice.Id, x.MissingScore)).ToList();
        db.ExamEvaluations.Add(evaluation);
        db.AdmissionChoiceSnapshots.AddRange(snapshots);
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        db.ChangeTracker.Clear();
        return Result<MmtEvaluationDto>.Success(ToEvaluationDto(await EvaluationQuery().SingleAsync(x => x.Id == evaluationId, ct)));
    }

    public Task<Result<PagedResponse<MmtEvaluationListItemDto>>> GetCurrentEvaluationsAsync(ClaimsPrincipal principal, MmtEvaluationFilter filter, CancellationToken ct) =>
        GetOwnedEvaluationsAsync(GetUserId(principal), filter, ct);

    public async Task<Result<MmtEvaluationDto>> GetCurrentEvaluationAsync(ClaimsPrincipal principal, Guid id, CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (!userId.HasValue) return Result<MmtEvaluationDto>.Failure(MmtErrors.UserRequired);
        var evaluation = await EvaluationQuery().SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        return evaluation is null ? Result<MmtEvaluationDto>.Failure(MmtErrors.EvaluationNotFound) : Result<MmtEvaluationDto>.Success(ToEvaluationDto(evaluation));
    }

    public async Task<Result<PagedResponse<StudentMmtProfileDto>>> GetAdminProfilesAsync(StudentMmtProfileFilter filter, CancellationToken ct)
    {
        var query = ProfileQuery();
        if (filter.UserId.HasValue) query = query.Where(x => x.UserId == filter.UserId);
        if (filter.AdmissionYear.HasValue) query = query.Where(x => x.AdmissionYear == filter.AdmissionYear);
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive);
        var page = MmtPaging.Page(filter.Page); var size = MmtPaging.PageSize(filter.PageSize);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.AdmissionYear).ThenByDescending(x => x.UpdatedAtUtc)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return Result<PagedResponse<StudentMmtProfileDto>>.Success(new(items.Select(ToProfileDto).ToList(), page, size, total));
    }

    public async Task<Result<StudentMmtProfileDto>> GetAdminProfileAsync(Guid id, CancellationToken ct)
    {
        var profile = await ProfileQuery().SingleOrDefaultAsync(x => x.Id == id, ct);
        return profile is null ? Result<StudentMmtProfileDto>.Failure(MmtErrors.StudentProfileNotFound) : Result<StudentMmtProfileDto>.Success(ToProfileDto(profile));
    }

    public async Task<Result<IReadOnlyList<StudentAdmissionChoiceDto>>> GetAdminChoicesAsync(Guid profileId, CancellationToken ct)
    {
        if (!await db.StudentProfiles.AsNoTracking().AnyAsync(x => x.Id == profileId, ct))
            return Result<IReadOnlyList<StudentAdmissionChoiceDto>>.Failure(MmtErrors.StudentProfileNotFound);

        return Result<IReadOnlyList<StudentAdmissionChoiceDto>>.Success(await ChoiceDtosAsync(profileId, ct));
    }

    public Task<Result<PagedResponse<MmtEvaluationListItemDto>>> GetAdminEvaluationsAsync(MmtEvaluationFilter filter, CancellationToken ct) =>
        GetOwnedEvaluationsAsync(null, filter, ct, admin: true);

    public async Task<Result<MmtEvaluationDto>> GetAdminEvaluationAsync(Guid id, CancellationToken ct)
    {
        var evaluation = await EvaluationQuery().SingleOrDefaultAsync(x => x.Id == id, ct);
        return evaluation is null ? Result<MmtEvaluationDto>.Failure(MmtErrors.EvaluationNotFound) : Result<MmtEvaluationDto>.Success(ToEvaluationDto(evaluation));
    }

    private int CurrentAdmissionYear => options.CurrentAdmissionYear ?? clock.UtcNow.Year;

    private async Task<Result<PagedResponse<MmtEvaluationListItemDto>>> GetOwnedEvaluationsAsync(
        Guid? userId,
        MmtEvaluationFilter filter,
        CancellationToken ct,
        bool admin = false)
    {
        if (!admin && !userId.HasValue) return Result<PagedResponse<MmtEvaluationListItemDto>>.Failure(MmtErrors.UserRequired);
        var query = db.ExamEvaluations.AsNoTracking();
        if (!admin) query = query.Where(x => x.UserId == userId);
        else if (filter.UserId.HasValue) query = query.Where(x => x.UserId == filter.UserId);
        if (filter.StudentMmtProfileId.HasValue) query = query.Where(x => x.StudentMmtProfileId == filter.StudentMmtProfileId);
        if (filter.AdmissionYear.HasValue) query = query.Where(x => x.AdmissionYear == filter.AdmissionYear);
        var page = MmtPaging.Page(filter.Page); var size = MmtPaging.PageSize(filter.PageSize);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.EvaluatedAtUtc).Skip((page - 1) * size).Take(size)
            .Select(x => new MmtEvaluationListItemDto(x.Id, x.TotalScore, x.AdmissionYear, x.ClusterId,
                x.EvaluatedAtUtc, x.AcceptedChoicePriority, x.AcceptedAdmissionProgramId,
                x.MissingScoreForGoal, x.ReadinessPercentage, x.MotivationalMessageKey)).ToListAsync(ct);
        return Result<PagedResponse<MmtEvaluationListItemDto>>.Success(new(items, page, size, total));
    }

    private IQueryable<StudentMmtProfile> ProfileQuery() => db.StudentProfiles.AsNoTracking().Include(x => x.MmtCluster).Include(x => x.Choices);

    private IQueryable<MmtExamEvaluation> EvaluationQuery() => db.ExamEvaluations.AsNoTracking()
        .Include(x => x.ChoiceSnapshots).AsSplitQuery();

    private async Task<IReadOnlyList<StudentAdmissionChoiceDto>> ChoiceDtosAsync(Guid profileId, CancellationToken ct)
    {
        var choices = await db.StudentAdmissionChoices.AsNoTracking()
            .Include(x => x.AdmissionProgram).ThenInclude(x => x.University)
            .Include(x => x.AdmissionProgram).ThenInclude(x => x.Specialty)
            .Include(x => x.AdmissionProgram).ThenInclude(x => x.MmtCluster)
            .Include(x => x.AdmissionProgram).ThenInclude(x => x.PassingScores)
            .Where(x => x.StudentMmtProfileId == profileId)
            .OrderBy(x => x.PriorityOrder)
            .AsSplitQuery()
            .ToListAsync(ct);

        return choices.Select(x => new StudentAdmissionChoiceDto(
            x.Id,
            x.PriorityOrder,
            new AdmissionProgramListItemDto(
                x.AdmissionProgram.Id, x.AdmissionProgram.UniversityId, x.AdmissionProgram.University.FullNameFor(MmtCatalogService.CurrentLanguage),
                x.AdmissionProgram.SpecialtyId, x.AdmissionProgram.Specialty.Code, x.AdmissionProgram.Specialty.NameFor(MmtCatalogService.CurrentLanguage),
                x.AdmissionProgram.MmtClusterId, x.AdmissionProgram.MmtCluster.Code, x.AdmissionProgram.MmtCluster.NameFor(MmtCatalogService.CurrentLanguage),
                (int)x.AdmissionProgram.AdmissionType, (int)x.AdmissionProgram.StudyForm, (int)x.AdmissionProgram.StudyLanguage,
                x.AdmissionProgram.AdmissionYear, x.AdmissionProgram.SeatsCount, x.AdmissionProgram.IsPublished,
                x.AdmissionProgram.IsActive, x.AdmissionProgram.PassingScores.Where(s => s.DistributionRound == DistributionRound.Main).OrderByDescending(s => s.Year)
                    .Select(s => (decimal?)s.PassingScore).FirstOrDefault()),
            x.CreatedAtUtc,
            x.UpdatedAtUtc)).ToList();
    }

    private async Task<bool> ProgramMatchesProfileAsync(Guid programId, Guid clusterId, int year, CancellationToken ct) =>
        await db.AdmissionPrograms.AnyAsync(x => x.Id == programId && x.MmtClusterId == clusterId
            && x.AdmissionYear == year && x.IsActive && x.IsPublished, ct);

    private async ValueTask<IDbContextTransaction?> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct) =>
        db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(isolationLevel, ct) : null;

    private static Adeeb.SharedKernel.Errors.Error? ValidateChoices(IReadOnlyList<AdmissionChoiceInputDto> choices)
    {
        if (choices.Count > 12) return MmtErrors.TooManyChoices;
        if (choices.Select(x => x.AdmissionProgramId).Distinct().Count() != choices.Count) return MmtErrors.DuplicateChoiceProgram;
        if (choices.Select(x => x.PriorityOrder).Distinct().Count() != choices.Count) return MmtErrors.DuplicateChoicePriority;
        if (!choices.Select(x => x.PriorityOrder).Order().SequenceEqual(Enumerable.Range(1, choices.Count))) return MmtErrors.InvalidChoiceOrder;
        return null;
    }

    private static ChoiceCalculation Calculate(StudentAdmissionChoice choice, decimal score)
    {
        var threshold = Threshold(choice.AdmissionProgram.PassingScores);
        return new(choice, threshold.Latest, threshold.Threshold,
            threshold.Threshold.HasValue ? decimal.Max(0, threshold.Threshold.Value - score) : null);
    }

    private static (decimal? Latest, decimal? Threshold) Threshold(IEnumerable<PassingScoreHistory> history)
    {
        var newest = history.Where(x => x.DistributionRound == DistributionRound.Main)
            .OrderByDescending(x => x.Year).Select(x => x.PassingScore).Take(3).ToList();
        if (newest.Count == 0) return (null, null);
        var latest = newest[0];
        var average = decimal.Round(newest.Average(), 2, MidpointRounding.AwayFromZero);
        return (latest, decimal.Max(latest, average));
    }

    private static string MotivationalMessage(ChoiceCalculation? accepted, IReadOnlyList<ChoiceCalculation> choices, decimal score)
    {
        if (accepted is not null) return "MMT.Accepted";
        var nearest = choices.Where(x => x.Threshold.HasValue)
            .OrderBy(x => x.MissingScore)
            .ThenBy(x => x.Choice.PriorityOrder)
            .FirstOrDefault();
        if (nearest is null) return "MMT.NoThresholdData";
        return nearest.Threshold.HasValue && nearest.Threshold > 0
            && decimal.Max(0, nearest.Threshold.Value - score) <= nearest.Threshold.Value * 0.10m
            ? "MMT.NearMiss"
            : "MMT.ProgressNeeded";
    }

    private static StudentMmtProfileDto ToProfileDto(StudentMmtProfile x) => new(
        x.Id, x.UserId, MmtCatalogService.ToDto(x.MmtCluster), x.AdmissionYear,
        x.GoalAdmissionProgramId, x.IsActive, x.Choices.Count, x.CreatedAtUtc, x.UpdatedAtUtc);

    private static MmtEvaluationDto ToEvaluationDto(MmtExamEvaluation x) => new(
        x.Id, x.UserId, x.StudentMmtProfileId, x.ExamSessionId, x.TotalScore, x.AdmissionYear,
        x.ClusterId, x.EvaluatedAtUtc, x.AcceptedChoicePriority, x.AcceptedAdmissionProgramId,
        x.MissingScoreForGoal, x.ReadinessPercentage, x.MotivationalMessageKey, x.CreatedAtUtc,
        x.ChoiceSnapshots.OrderBy(s => s.PriorityOrder).Select(s => new MmtAdmissionChoiceSnapshotDto(
            s.Id, s.PriorityOrder, s.AdmissionProgramId, s.UniversityNameSnapshot,
            s.SpecialtyCodeSnapshot, s.SpecialtyNameSnapshot, s.ClusterCodeSnapshot,
            (int)s.AdmissionType, (int)s.StudyForm, (int)s.StudyLanguage, s.AdmissionYear,
            s.PassingScoreUsed, s.ConservativeThresholdUsed, s.StudentScore, s.IsAccepted,
            s.MissingScore)).ToList());

    private static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var userId)
            ? userId
            : null;

    private sealed record ChoiceCalculation(
        StudentAdmissionChoice Choice,
        decimal? Latest,
        decimal? Threshold,
        decimal? MissingScore);
}
