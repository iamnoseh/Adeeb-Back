using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.QuestionBank.Domain;

namespace Adeeb.Modules.QuestionBank.Application;

internal sealed record TestQuestionSnapshot(int Version, Guid QuestionId, Guid SubjectId, Guid? TopicId,
    int Type, int Difficulty, string Content, string? Explanation, string? ImageUrl,
    SupportedLanguage Language, IReadOnlyList<TestOptionSnapshot> Options,
    IReadOnlyList<Guid>? MatchingDisplayOrder, int? RedListCorrectStreak = null,
    int RedListRequiredCorrectStreak = StudentRedListItem.RequiredCorrectStreak)
{
    public const int CurrentVersion = 3;
}

internal sealed record TestOptionSnapshot(Guid Id, int DisplayOrder, bool IsCorrect, string Text, string? MatchPairText);

internal sealed record StoredAnswerSnapshot(Guid? SelectedOptionId, string? TextResponse,
    IReadOnlyDictionary<Guid, string>? MatchingPairs, string? SubmittedDisplayText,
    StoredRedListFeedback? RedList = null);

internal sealed record StoredRedListFeedback(int Action, int CorrectStreak, int RequiredCorrectStreak,
    int CorrectAnswersRemaining, int MasteryBonusXpUnits, bool MasteryBonusAwarded, long? TotalXpUnits);

internal sealed record StoredAnswerResult(Guid QuestionId, Guid SubjectId, bool IsAnswered, bool IsCorrect, string Content,
    string? UserAnswer, string? CorrectAnswer, string? Explanation, Guid? TopicId, int Difficulty,
    int? CorrectPairsCount, int? TotalPairsCount);
