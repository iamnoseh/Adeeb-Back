namespace Adeeb.Application.Abstractions.Testing;

public sealed record StudentMmtTestingContext(
    Guid ProfileId,
    Guid ClusterId,
    IReadOnlyList<Guid> SubjectIds,
    int AdmissionChoicesCount,
    int AdmissionYear,
    Guid? ExamVersionId = null,
    string? ExamVersionName = null,
    bool IsOfficialScale = false,
    int DurationMinutes = 0,
    IReadOnlyList<MmtSubtestDefinition>? Subtests = null,
    IReadOnlyList<MmtChoiceScoringContext>? ChoiceScoring = null,
    IReadOnlyList<string>? ReadinessIssues = null)
{
    public bool IsExamReady => ExamVersionId.HasValue && Subtests is { Count: 4 }
        && ReadinessIssues is { Count: 0 };
    public int ExactQuestionCount => Subtests?.Sum(x => x.QuestionCount) ?? 0;
}

public sealed record MmtSubtestDefinition(string Code, int DisplayOrder, Guid SubjectId,
    int SingleChoiceCount, int MatchingCount, int ShortAnswerCount, int MinimumRawScore)
{
    public int QuestionCount => SingleChoiceCount + MatchingCount + ShortAnswerCount;
    public int MaxRawScore => SingleChoiceCount + MatchingCount * 4 + ShortAnswerCount * 2;
}

public sealed record MmtChoiceScoringContext(Guid AdmissionProgramId, int PriorityOrder,
    Guid SpecialtyId, Guid SpecialtyRangeId, string SpecialtyRangeCode);

public sealed record MmtSubtestRawScore(string Code, int RawScore);
public sealed record MmtScaledSubtestScore(string Code, int RawScore, int MinimumRawScore,
    bool Passed, decimal ScaledScore, decimal MaxScaledScore);
public sealed record MmtChoiceScore(Guid AdmissionProgramId, int PriorityOrder, Guid SpecialtyRangeId,
    string SpecialtyRangeCode, decimal? TotalScaledScore, bool PassedAllSubtests,
    IReadOnlyList<MmtScaledSubtestScore> Subtests);
public sealed record MmtOfficialScore(Guid ExamVersionId, string ExamVersionName, bool IsOfficialScale,
    IReadOnlyList<MmtChoiceScore> Choices);

public interface IStudentMmtTestingContext
{
    Task<StudentMmtTestingContext?> GetAsync(Guid userId, CancellationToken ct);
    Task<MmtOfficialScore?> CalculateAsync(Guid examVersionId, Guid clusterId,
        IReadOnlyList<MmtChoiceScoringContext> choices, IReadOnlyList<MmtSubtestRawScore> rawScores,
        CancellationToken ct) => Task.FromResult<MmtOfficialScore?>(null);
}
