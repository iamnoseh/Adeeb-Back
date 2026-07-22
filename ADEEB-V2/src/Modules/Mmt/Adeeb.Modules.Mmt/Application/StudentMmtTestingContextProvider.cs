using Adeeb.Application.Abstractions.Testing;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.Mmt.Application;

internal sealed class StudentMmtTestingContextProvider(
    MmtDbContext db,
    IOptions<MmtOptions> options,
    IDateTimeProvider clock) : IStudentMmtTestingContext
{
    public async Task<StudentMmtTestingContext?> GetAsync(Guid userId, CancellationToken ct)
    {
        var year = options.Value.CurrentAdmissionYear ?? clock.UtcNow.Year;
        var profile = await db.StudentProfiles.AsNoTracking()
            .Include(x => x.MmtCluster).ThenInclude(x => x.Subjects)
            .Include(x => x.Choices).ThenInclude(x => x.AdmissionProgram)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.AdmissionYear == year && x.IsActive, ct);
        if (profile is null) return null;

        var version = await db.ExamVersions.AsNoTracking()
            .Where(x => x.Status == MmtExamVersionStatus.Published && x.AdmissionYear <= profile.AdmissionYear)
            .OrderByDescending(x => x.AdmissionYear == profile.AdmissionYear)
            .ThenByDescending(x => x.AdmissionYear)
            .ThenByDescending(x => x.IsOfficial)
            .FirstOrDefaultAsync(ct);
        if (version is null)
            return Basic(profile, ["exam-version-missing"]);

        var blueprint = await db.ExamBlueprints.AsNoTracking().Include(x => x.Subtests)
            .SingleOrDefaultAsync(x => x.ExamVersionId == version.Id && x.MmtClusterId == profile.MmtClusterId, ct);
        if (blueprint is null)
            return Basic(profile, ["cluster-blueprint-missing"]);

        var thresholds = await db.PassThresholds.AsNoTracking()
            .Where(x => x.ExamVersionId == version.Id && x.MmtClusterId == profile.MmtClusterId)
            .ToDictionaryAsync(x => x.SubtestCode, x => x.MinimumRawScore, ct);
        var ranges = await db.SpecialtyRanges.AsNoTracking().Include(x => x.Specialties)
            .Where(x => x.ExamVersionId == version.Id && x.MmtClusterId == profile.MmtClusterId)
            .ToListAsync(ct);
        var rangeBySpecialty = ranges.SelectMany(range => range.Specialties.Select(link => new { link.SpecialtyId, Range = range }))
            .GroupBy(x => x.SpecialtyId).ToDictionary(x => x.Key, x => x.First().Range);
        var choiceScoring = new List<MmtChoiceScoringContext>();
        var issues = new List<string>();
        foreach (var choice in profile.Choices.OrderBy(x => x.PriorityOrder))
        {
            if (!rangeBySpecialty.TryGetValue(choice.AdmissionProgram.SpecialtyId, out var range))
            {
                issues.Add($"specialty-range-missing:{choice.AdmissionProgram.SpecialtyId:N}");
                continue;
            }
            choiceScoring.Add(new(choice.AdmissionProgramId, choice.PriorityOrder,
                choice.AdmissionProgram.SpecialtyId, range.Id, range.Code));
        }

        var subtests = blueprint.Subtests.OrderBy(x => x.DisplayOrder).Select(x => new MmtSubtestDefinition(
            x.Code, x.DisplayOrder, x.SubjectId, x.SingleChoiceCount, x.MatchingCount, x.ShortAnswerCount,
            thresholds.GetValueOrDefault(x.Code, -1))).ToList();
        if (subtests.Count != 4 || subtests.Select(x => x.Code).Distinct().Count() != 4)
            issues.Add("four-subtests-required");
        if (subtests.Any(x => x.MaxRawScore != 40)) issues.Add("subtest-max-raw-must-be-40");
        if (subtests.Any(x => x.MinimumRawScore < 0)) issues.Add("pass-threshold-missing");
        foreach (var rangeId in choiceScoring.Select(x => x.SpecialtyRangeId).Distinct())
        {
            var scaleCount = await db.ScoreScaleEntries.AsNoTracking().CountAsync(x =>
                x.ExamVersionId == version.Id && x.MmtClusterId == profile.MmtClusterId
                && ((x.SubtestCode == "A1" && x.SpecialtyRangeId == null)
                    || (x.SubtestCode != "A1" && x.SpecialtyRangeId == rangeId)), ct);
            if (scaleCount != 164) issues.Add($"score-scale-incomplete:{rangeId:N}");
        }

        return new(profile.Id, profile.MmtClusterId, subtests.Select(x => x.SubjectId).ToList(),
            profile.Choices.Count, profile.AdmissionYear, version.Id, version.NameTg, version.IsOfficial,
            blueprint.DurationMinutes, subtests, choiceScoring, issues.Distinct().ToList());
    }

    public async Task<MmtOfficialScore?> CalculateAsync(Guid examVersionId, Guid clusterId,
        IReadOnlyList<MmtChoiceScoringContext> choices, IReadOnlyList<MmtSubtestRawScore> rawScores,
        CancellationToken ct)
    {
        var version = await db.ExamVersions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == examVersionId, ct);
        if (version is null) return null;
        var thresholds = await db.PassThresholds.AsNoTracking()
            .Where(x => x.ExamVersionId == examVersionId && x.MmtClusterId == clusterId)
            .ToDictionaryAsync(x => x.SubtestCode, x => x.MinimumRawScore, ct);
        var rangeIds = choices.Select(x => x.SpecialtyRangeId).Distinct().ToArray();
        var scales = await db.ScoreScaleEntries.AsNoTracking().Where(x => x.ExamVersionId == examVersionId
            && x.MmtClusterId == clusterId && (x.SpecialtyRangeId == null || rangeIds.Contains(x.SpecialtyRangeId.Value)))
            .ToListAsync(ct);
        var results = new List<MmtChoiceScore>(choices.Count);
        foreach (var choice in choices.OrderBy(x => x.PriorityOrder))
        {
            var subtestScores = new List<MmtScaledSubtestScore>(4);
            foreach (var raw in rawScores.OrderBy(x => x.Code))
            {
                Guid? rangeId = raw.Code == "A1" ? null : choice.SpecialtyRangeId;
                var scale = scales.SingleOrDefault(x => x.SubtestCode == raw.Code
                    && x.SpecialtyRangeId == rangeId && x.RawScore == raw.RawScore);
                if (scale is null) return null;
                var minimum = thresholds.GetValueOrDefault(raw.Code, 0);
                subtestScores.Add(new(raw.Code, raw.RawScore, minimum, raw.RawScore >= minimum,
                    scale.ScaledScore, scale.MaxScaledScore));
            }
            var passed = subtestScores.Count == 4 && subtestScores.All(x => x.Passed);
            results.Add(new(choice.AdmissionProgramId, choice.PriorityOrder, choice.SpecialtyRangeId,
                choice.SpecialtyRangeCode, passed ? subtestScores.Sum(x => x.ScaledScore) : null,
                passed, subtestScores));
        }
        return new(version.Id, version.NameTg, version.IsOfficial, results);
    }

    private static StudentMmtTestingContext Basic(StudentMmtProfile profile, IReadOnlyList<string> issues) =>
        new(profile.Id, profile.MmtClusterId, profile.MmtCluster.Subjects.Select(x => x.SubjectId).ToList(),
            profile.Choices.Count, profile.AdmissionYear, ReadinessIssues: issues);
}
