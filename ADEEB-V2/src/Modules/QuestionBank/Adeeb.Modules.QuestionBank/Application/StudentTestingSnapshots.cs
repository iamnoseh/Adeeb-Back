using Adeeb.Application.Abstractions.Localization;

namespace Adeeb.Modules.QuestionBank.Application;

internal sealed record TestQuestionSnapshot(int Version, Guid QuestionId, Guid SubjectId, Guid? TopicId,
    int Type, int Difficulty, string Content, string? Explanation, string? ImageUrl,
    SupportedLanguage Language, IReadOnlyList<TestOptionSnapshot> Options)
{
    public const int CurrentVersion = 1;
}

internal sealed record TestOptionSnapshot(Guid Id, int DisplayOrder, bool IsCorrect, string Text, string? MatchPairText);

internal sealed record StoredAnswerSnapshot(Guid? SelectedOptionId, string? TextResponse,
    IReadOnlyDictionary<Guid, string>? MatchingPairs, string? SubmittedDisplayText);

internal sealed record StoredAnswerResult(Guid QuestionId, Guid SubjectId, bool IsAnswered, bool IsCorrect, string Content,
    string? UserAnswer, string? CorrectAnswer, string? Explanation, Guid? TopicId, int Difficulty,
    int? CorrectPairsCount, int? TotalPairsCount);
