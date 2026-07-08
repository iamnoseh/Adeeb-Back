using Microsoft.AspNetCore.Http;

namespace Adeeb.Modules.QuestionBank.Contracts;

public sealed record QuestionTranslationRequest(int Language, string Content, string? Explanation);
public sealed record AnswerOptionTranslationRequest(int Language, string Text, string? MatchPairText);
public sealed record AnswerOptionRequest(int DisplayOrder, bool IsCorrect, IReadOnlyList<AnswerOptionTranslationRequest> Translations);
public sealed record QuestionUpsertRequest(Guid SubjectId, Guid? TopicId, string? Topic, int Type, int Difficulty, int Status, string? ImageUrl, IReadOnlyList<QuestionTranslationRequest> Translations, IReadOnlyList<AnswerOptionRequest> AnswerOptions);
public sealed record QuestionListQuery(Guid? SubjectId, Guid? TopicId, string? Topic, int? Type, int? Difficulty, int? Status, string? Search, int Page = 1, int PageSize = 20, string? Sort = null);
public sealed record QuestionTranslationResponse(int Language, string Content, string? Explanation);
public sealed record AnswerOptionTranslationResponse(int Language, string Text, string? MatchPairText);
public sealed record AnswerOptionResponse(Guid Id, int DisplayOrder, bool IsCorrect, IReadOnlyList<AnswerOptionTranslationResponse> Translations);
public sealed record QuestionResponse(Guid Id, Guid SubjectId, Guid? TopicId, string? Topic, int Type, int Difficulty, int Status, string Content, string? ImageUrl, IReadOnlyList<QuestionTranslationResponse> Translations, IReadOnlyList<AnswerOptionResponse> AnswerOptions);
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed class QuestionFormRequest
{
    public Guid SubjectId { get; init; }
    public Guid? TopicId { get; init; }
    public string Content { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
    public int Type { get; init; }
    public int Difficulty { get; init; }
    public int Status { get; init; } = 1;
    public string? AnswersJson { get; init; }
    public string? CorrectAnswer { get; init; }
    public IFormFile? Image { get; init; }
}

public sealed record LegacyAnswerFormRequest(string? Text, string? Answer, bool IsCorrect, string? MatchPair);
