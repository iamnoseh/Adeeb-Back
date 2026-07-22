using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.Mmt.Application;

public sealed class MmtExamConfigurationService(MmtDbContext db, IDateTimeProvider clock)
{
    public async Task<IReadOnlyList<MmtExamVersionListItemDto>> ListAsync(CancellationToken ct)
    {
        var versions = await db.ExamVersions.AsNoTracking().OrderByDescending(x => x.AdmissionYear)
            .ThenByDescending(x => x.IsOfficial).ToListAsync(ct);
        var result = new List<MmtExamVersionListItemDto>(versions.Count);
        foreach (var version in versions)
        {
            var readiness = await ReadinessAsync(version.Id, ct);
            result.Add(new(version.Id, version.AdmissionYear, version.NameTg, version.NameRu, version.IsOfficial,
                (int)version.Status, version.UpdatedAtUtc, version.PublishedAtUtc, readiness.BlueprintCount, readiness.IsReady));
        }
        return result;
    }

    public async Task<Result<MmtExamVersionDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var version = await db.ExamVersions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (version is null) return Result<MmtExamVersionDto>.Failure(MmtErrors.ExamVersionNotFound);
        return Result<MmtExamVersionDto>.Success(await ToDtoAsync(version, ct));
    }

    public async Task<Result<MmtExamVersionDto>> CreateAsync(CreateMmtExamVersionDto request, CancellationToken ct)
    {
        if (request.AdmissionYear is < 2000 or > 2100 || string.IsNullOrWhiteSpace(request.NameTg)
            || string.IsNullOrWhiteSpace(request.NameRu))
            return Result<MmtExamVersionDto>.Failure(MmtErrors.ExamConfigurationInvalid);
        if (await db.ExamVersions.AnyAsync(x => x.AdmissionYear == request.AdmissionYear && x.IsOfficial == request.IsOfficial, ct))
            return Result<MmtExamVersionDto>.Failure(MmtErrors.ExamVersionDuplicate);
        var entity = new MmtExamVersion(Guid.NewGuid(), request.AdmissionYear, request.NameTg, request.NameRu,
            request.IsOfficial, request.SourceUrl, request.SourceChecksum, clock.UtcNow);
        db.ExamVersions.Add(entity); await db.SaveChangesAsync(ct);
        return Result<MmtExamVersionDto>.Success(await ToDtoAsync(entity, ct));
    }

    public async Task<Result<MmtExamVersionDto>> UpdateAsync(Guid id, UpdateMmtExamVersionDto request, CancellationToken ct)
    {
        var entity = await db.ExamVersions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return Result<MmtExamVersionDto>.Failure(MmtErrors.ExamVersionNotFound);
        if (entity.Status != MmtExamVersionStatus.Draft) return Result<MmtExamVersionDto>.Failure(MmtErrors.ExamVersionImmutable);
        if (entity.Version != request.Version) return Result<MmtExamVersionDto>.Failure(MmtErrors.ProfileConflict);
        entity.Update(request.NameTg, request.NameRu, request.IsOfficial, request.SourceUrl, request.SourceChecksum, clock.UtcNow);
        await db.SaveChangesAsync(ct);
        return Result<MmtExamVersionDto>.Success(await ToDtoAsync(entity, ct));
    }

    public async Task<Result<MmtExamVersionDto>> ReplaceConfigurationAsync(Guid id,
        ReplaceMmtExamConfigurationDto request, CancellationToken ct)
    {
        var version = await db.ExamVersions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (version is null) return Result<MmtExamVersionDto>.Failure(MmtErrors.ExamVersionNotFound);
        if (version.Status != MmtExamVersionStatus.Draft) return Result<MmtExamVersionDto>.Failure(MmtErrors.ExamVersionImmutable);
        var validation = await ValidateRequestAsync(request, ct);
        if (!validation) return Result<MmtExamVersionDto>.Failure(MmtErrors.ExamConfigurationInvalid);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.ScoreScaleEntries.Where(x => x.ExamVersionId == id).ExecuteDeleteAsync(ct);
        await db.SpecialtyRangeSpecialties.Where(x => x.Range.ExamVersionId == id).ExecuteDeleteAsync(ct);
        await db.SpecialtyRanges.Where(x => x.ExamVersionId == id).ExecuteDeleteAsync(ct);
        await db.SubtestBlueprints.Where(x => x.ClusterBlueprint.ExamVersionId == id).ExecuteDeleteAsync(ct);
        await db.ExamBlueprints.Where(x => x.ExamVersionId == id).ExecuteDeleteAsync(ct);
        await db.PassThresholds.Where(x => x.ExamVersionId == id).ExecuteDeleteAsync(ct);

        foreach (var blueprint in request.Blueprints)
        {
            var bp = new MmtClusterExamBlueprint(Guid.NewGuid(), id, blueprint.ClusterId, blueprint.DurationMinutes);
            db.ExamBlueprints.Add(bp);
            foreach (var subtest in blueprint.Subtests)
            {
                db.SubtestBlueprints.Add(new(Guid.NewGuid(), bp.Id, subtest.Code, subtest.DisplayOrder,
                    subtest.SubjectId, subtest.SingleChoiceCount, subtest.MatchingCount, subtest.ShortAnswerCount));
                db.PassThresholds.Add(new(Guid.NewGuid(), id, blueprint.ClusterId, subtest.Code, subtest.MinimumRawScore));
            }
        }

        var rangeIds = new Dictionary<(Guid ClusterId, string Code), Guid>();
        foreach (var input in request.SpecialtyRanges)
        {
            var rangeId = input.Id is { } supplied && supplied != Guid.Empty ? supplied : Guid.NewGuid();
            var range = new MmtSpecialtyRange(rangeId, id, input.ClusterId, input.Code,
                input.A2MaxScore, input.A3MaxScore, input.A4MaxScore);
            db.SpecialtyRanges.Add(range);
            foreach (var specialtyId in input.SpecialtyIds.Distinct())
                db.SpecialtyRangeSpecialties.Add(new(rangeId, specialtyId));
            rangeIds[(input.ClusterId, input.Code.Trim().ToUpperInvariant())] = rangeId;
        }

        foreach (var input in request.ScaleEntries)
        {
            Guid? rangeId = input.SubtestCode.Equals("A1", StringComparison.OrdinalIgnoreCase) ? null
                : input.SpecialtyRangeId;
            if (rangeId is null && input.SubtestCode != "A1" && !string.IsNullOrWhiteSpace(input.SpecialtyRangeCode))
                rangeId = rangeIds[(input.ClusterId, input.SpecialtyRangeCode.Trim().ToUpperInvariant())];
            db.ScoreScaleEntries.Add(new(Guid.NewGuid(), id, input.ClusterId, input.SubtestCode,
                rangeId, input.RawScore, input.ScaledScore, input.MaxScaledScore));
        }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Result<MmtExamVersionDto>.Success(await ToDtoAsync(version, ct));
    }

    public async Task<Result<MmtExamVersionDto>> PublishAsync(Guid id, CancellationToken ct)
    {
        var version = await db.ExamVersions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (version is null) return Result<MmtExamVersionDto>.Failure(MmtErrors.ExamVersionNotFound);
        if (version.Status != MmtExamVersionStatus.Draft) return Result<MmtExamVersionDto>.Failure(MmtErrors.ExamVersionImmutable);
        var readiness = await ReadinessAsync(id, ct);
        if (!readiness.IsReady) return Result<MmtExamVersionDto>.Failure(MmtErrors.ExamVersionNotReady);
        version.Publish(clock.UtcNow); await db.SaveChangesAsync(ct);
        return Result<MmtExamVersionDto>.Success(await ToDtoAsync(version, ct));
    }

    public async Task<MmtExamReadinessDto> ReadinessAsync(Guid id, CancellationToken ct)
    {
        var blueprints = await db.ExamBlueprints.AsNoTracking().Include(x => x.Subtests)
            .Where(x => x.ExamVersionId == id).ToListAsync(ct);
        var ranges = await db.SpecialtyRanges.AsNoTracking().Include(x => x.Specialties)
            .Where(x => x.ExamVersionId == id).ToListAsync(ct);
        var scales = await db.ScoreScaleEntries.AsNoTracking().Where(x => x.ExamVersionId == id).ToListAsync(ct);
        var thresholds = await db.PassThresholds.AsNoTracking().Where(x => x.ExamVersionId == id).ToListAsync(ct);
        var issues = new List<MmtExamReadinessIssueDto>();
        foreach (var bp in blueprints)
        {
            if (bp.Subtests.Count != 4 || bp.Subtests.Select(x => x.Code).Distinct().Count() != 4)
                issues.Add(new("four-subtests-required", bp.MmtClusterId, null));
            if (bp.Subtests.Any(x => x.MaxRawScore != 40)) issues.Add(new("subtest-max-raw-must-be-40", bp.MmtClusterId, null));
            if (thresholds.Count(x => x.MmtClusterId == bp.MmtClusterId) != 4)
                issues.Add(new("thresholds-incomplete", bp.MmtClusterId, null));
            var clusterRanges = ranges.Where(x => x.MmtClusterId == bp.MmtClusterId).ToList();
            if (clusterRanges.Count == 0) issues.Add(new("specialty-ranges-missing", bp.MmtClusterId, null));
            foreach (var range in clusterRanges)
            {
                if (range.Specialties.Count == 0) issues.Add(new("range-specialties-missing", bp.MmtClusterId, range.Code));
                var expected = scales.Count(x => x.MmtClusterId == bp.MmtClusterId
                    && ((x.SubtestCode == "A1" && x.SpecialtyRangeId == null)
                        || (x.SubtestCode != "A1" && x.SpecialtyRangeId == range.Id)));
                if (expected != 164) issues.Add(new("scale-must-have-41-values-per-subtest", bp.MmtClusterId, range.Code));
            }
        }
        if (blueprints.Count == 0) issues.Add(new("blueprints-missing", null, null));
        return new(issues.Count == 0, blueprints.Count, ranges.Count, scales.Count, issues);
    }

    private async Task<bool> ValidateRequestAsync(ReplaceMmtExamConfigurationDto request, CancellationToken ct)
    {
        if (request.Blueprints.Count == 0 || request.Blueprints.Select(x => x.ClusterId).Distinct().Count() != request.Blueprints.Count) return false;
        if (request.Blueprints.Any(x => x.Subtests.Count != 4 || x.Subtests.Any(s => s.MaxRawScore() != 40))) return false;
        var subjects = request.Blueprints.SelectMany(x => x.Subtests).Select(x => x.SubjectId).Distinct().ToArray();
        if (subjects.Any(x => x == Guid.Empty)) return false;
        var specialties = request.SpecialtyRanges.SelectMany(x => x.SpecialtyIds).Distinct().ToArray();
        return specialties.Length == 0 || await db.Specialties.CountAsync(x => specialties.Contains(x.Id) && x.IsActive, ct) == specialties.Length;
    }

    private async Task<MmtExamVersionDto> ToDtoAsync(MmtExamVersion version, CancellationToken ct)
    {
        var blueprints = await db.ExamBlueprints.AsNoTracking().Include(x => x.Subtests)
            .Where(x => x.ExamVersionId == version.Id).OrderBy(x => x.Cluster.Code).ToListAsync(ct);
        var thresholds = await db.PassThresholds.AsNoTracking().Where(x => x.ExamVersionId == version.Id)
            .ToDictionaryAsync(x => (x.MmtClusterId, x.SubtestCode), x => x.MinimumRawScore, ct);
        var ranges = await db.SpecialtyRanges.AsNoTracking().Include(x => x.Specialties)
            .Where(x => x.ExamVersionId == version.Id).ToListAsync(ct);
        return new(version.Id, version.AdmissionYear, version.NameTg, version.NameRu, version.IsOfficial,
            version.SourceUrl, version.SourceChecksum, (int)version.Status, version.Version, version.CreatedAtUtc,
            version.UpdatedAtUtc, version.PublishedAtUtc,
            blueprints.Select(x => new MmtClusterExamBlueprintDto(x.MmtClusterId, x.DurationMinutes,
                x.Subtests.OrderBy(s => s.DisplayOrder).Select(s => new MmtSubtestBlueprintDto(s.Code, s.DisplayOrder,
                    s.SubjectId, s.SingleChoiceCount, s.MatchingCount, s.ShortAnswerCount,
                    thresholds.GetValueOrDefault((x.MmtClusterId, s.Code), 0))).ToList())).ToList(),
            ranges.Select(x => new MmtSpecialtyRangeDto(x.Id, x.MmtClusterId, x.Code, x.A2MaxScore,
                x.A3MaxScore, x.A4MaxScore, x.Specialties.Select(s => s.SpecialtyId).ToList())).ToList(),
            await ReadinessAsync(version.Id, ct));
    }
}

internal static class MmtExamContractExtensions
{
    public static int MaxRawScore(this MmtSubtestBlueprintDto value) =>
        value.SingleChoiceCount + value.MatchingCount * 4 + value.ShortAnswerCount * 2;
}
