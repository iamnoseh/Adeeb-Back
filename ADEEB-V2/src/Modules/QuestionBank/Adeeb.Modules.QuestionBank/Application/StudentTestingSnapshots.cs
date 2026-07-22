using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Testing;
using Adeeb.Modules.QuestionBank.Domain;

namespace Adeeb.Modules.QuestionBank.Application;

internal sealed record TestQuestionSnapshot(int Version, Guid QuestionId, Guid SubjectId, Guid? TopicId,
    int Type, int Difficulty, string Content, string? Explanation, string? ImageUrl,
    SupportedLanguage Language, IReadOnlyList<TestOptionSnapshot> Options,
    IReadOnlyList<Guid>? MatchingDisplayOrder, int? RedListCorrectStreak = null,
    int RedListRequiredCorrectStreak = StudentRedListItem.RequiredCorrectStreak,
    string? MmtSubtestCode = null, int PointsAvailable = 1)
{
    public const int CurrentVersion = 4;
}

internal sealed record StoredMmtAttemptSnapshot(Guid ExamVersionId, string ExamVersionName,
    bool IsOfficialScale, int DurationMinutes, IReadOnlyList<MmtSubtestDefinition> Subtests,
    IReadOnlyList<MmtChoiceScoringContext> Choices);

internal sealed record TestOptionSnapshot(Guid Id, int DisplayOrder, bool IsCorrect, string Text, string? MatchPairText);

internal sealed record StoredAnswerSnapshot(Guid? SelectedOptionId, string? TextResponse,
    IReadOnlyDictionary<Guid, string>? MatchingPairs, string? SubmittedDisplayText,
    StoredRedListFeedback? RedList = null);
internal sealed record StoredDraftAnswer(Guid? SelectedOptionId, string? TextResponse,
    IReadOnlyDictionary<Guid, string>? MatchingPairs);

internal sealed record StoredRedListFeedback(int Action, int CorrectStreak, int RequiredCorrectStreak,
    int CorrectAnswersRemaining, int MasteryBonusXpUnits, bool MasteryBonusAwarded, long? TotalXpUnits);

internal sealed record StoredAnswerResult(Guid QuestionId, Guid SubjectId, bool IsAnswered, bool IsCorrect, string Content,
    string? UserAnswer, string? CorrectAnswer, string? Explanation, Guid? TopicId, int Difficulty,
    int? CorrectPairsCount, int? TotalPairsCount);
