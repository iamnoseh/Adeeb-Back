using Adeeb.Application.Abstractions.AcademicCatalog;
using Adeeb.Application.Abstractions.Testing;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.Mmt.Application;

internal sealed class StudentMmtTestingContextProvider(
    MmtDbContext db,
    IOptions<MmtOptions> options,
    IDateTimeProvider clock,
    IAcademicSubjectLookup academicCatalog) : IStudentMmtTestingContext
{
    public async Task<StudentMmtTestingContext?> GetAsync(Guid userId, CancellationToken ct)
    {
        var year = options.Value.CurrentAdmissionYear ?? clock.UtcNow.Year;
        var profile = await db.StudentProfiles.AsNoTracking()
            .Include(x => x.MmtCluster).ThenInclude(x => x.Subjects)
            .Include(x => x.Choices).ThenInclude(x => x.AdmissionProgram)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.AdmissionYear == year && x.IsActive, ct);
        if (profile is null) return null;

        var language = IsRussian() ? SupportedLanguage.Russian : SupportedLanguage.Tajik;
        var orderedLinks = profile.MmtCluster.Subjects.OrderBy(x => x.DisplayOrder).ToList();
        var subjectIds = orderedLinks.Select(x => x.SubjectId).ToList();
        var subjectLookup = (await academicCatalog.GetActiveSubjectsAsync(subjectIds, language, ct))
            .ToDictionary(x => x.Id);
        var structureIssues = new List<string>();
        if (orderedLinks.Count != 4 || subjectLookup.Count != 4)
            structureIssues.Add("cluster-must-have-four-ordered-subjects");
        if (!MmtOfficialTestPolicy.TryDurationMinutes(profile.MmtCluster.Code, out var durationMinutes))
            structureIssues.Add("unsupported-cluster-code");

        var version = await db.ExamVersions.AsNoTracking()
            .Where(x => x.Status == MmtExamVersionStatus.Published && x.AdmissionYear <= profile.AdmissionYear)
            .OrderByDescending(x => x.AdmissionYear == profile.AdmissionYear)
            .ThenByDescending(x => x.AdmissionYear)
            .ThenByDescending(x => x.IsOfficial)
            .FirstOrDefaultAsync(ct);
        if (version is null)
            return Basic(profile, [.. structureIssues, "exam-version-missing"]);

        var thresholds = await db.PassThresholds.AsNoTracking()
            .Where(x => x.ExamVersionId == version.Id && x.MmtClusterId == profile.MmtClusterId)
            .ToDictionaryAsync(x => x.SubtestCode, x => x.MinimumRawScore, ct);
        var ranges = await db.SpecialtyRanges.AsNoTracking().Include(x => x.Specialties)
            .Where(x => x.ExamVersionId == version.Id && x.MmtClusterId == profile.MmtClusterId)
            .ToListAsync(ct);
        var rangeBySpecialty = ranges.SelectMany(range => range.Specialties.Select(link => new { link.SpecialtyId, Range = range }))
            .GroupBy(x => x.SpecialtyId).ToDictionary(x => x.Key, x => x.First().Range);
        var choiceScoring = new List<MmtChoiceScoringContext>();
        var issues = new List<string>(structureIssues);
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

        var subtests = orderedLinks.Where(x => subjectLookup.ContainsKey(x.SubjectId)).Select(x =>
        {
            var code = $"A{x.DisplayOrder}";
            var mix = MmtOfficialTestPolicy.QuestionMix(subjectLookup[x.SubjectId].Code);
            return new MmtSubtestDefinition(code, x.DisplayOrder, x.SubjectId,
                mix.SingleChoice, mix.Matching, mix.ShortAnswer, thresholds.GetValueOrDefault(code, -1));
        }).ToList();
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
            profile.Choices.Count, profile.AdmissionYear, version.Id, version.NameFor(IsRussian()), version.IsOfficial,
            durationMinutes, subtests, choiceScoring, issues.Distinct().ToList());
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
        var rawResults = rawScores.OrderBy(x => x.Code).Select(x => new MmtRawSubtestResult(x.Code,
            x.RawScore, 40, thresholds.GetValueOrDefault(x.Code, 0),
            x.RawScore >= thresholds.GetValueOrDefault(x.Code, 0))).ToList();
        return new(version.Id, version.NameFor(IsRussian()), version.IsOfficial, rawResults, results);
    }

    private static StudentMmtTestingContext Basic(StudentMmtProfile profile, IReadOnlyList<string> issues) =>
        new(profile.Id, profile.MmtClusterId, profile.MmtCluster.Subjects.OrderBy(x => x.DisplayOrder)
                .Select(x => x.SubjectId).ToList(),
            profile.Choices.Count, profile.AdmissionYear, ReadinessIssues: issues);

    private static bool IsRussian() => SupportedLanguageExtensions.TryParseCulture(
        System.Globalization.CultureInfo.CurrentUICulture.Name, out var language)
        && language == SupportedLanguage.Russian;
}
