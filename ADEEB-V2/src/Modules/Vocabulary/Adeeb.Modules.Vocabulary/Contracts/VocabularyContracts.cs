namespace Adeeb.Modules.Vocabulary.Contracts;

public sealed record VocabularyPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record VocabularyListQuery(string? Search = null, Guid? LanguageId = null, int? Level = null, Guid? TopicId = null, int? Type = null, int? Status = null, int Page = 1, int PageSize = 10);

public sealed record LanguageUpsertRequest(string Code, string NameTg, string NameRu, int DisplayOrder, bool IsActive = true);
public sealed record LearningLanguageDto(Guid Id, string Code, string Name, string NameTg, string NameRu, int DisplayOrder, bool IsActive);

public sealed record TopicUpsertRequest(Guid LanguageId, int Level, string NameTg, string NameRu, string? DescriptionTg, string? DescriptionRu, int Status = 0);
public sealed record VocabularyTopicDto(Guid Id, Guid LanguageId, int Level, string Name, string NameTg, string NameRu, string? Description, string? DescriptionTg, string? DescriptionRu, int Status);

public sealed record WordUpsertRequest(Guid LanguageId, Guid TopicId, int Level, string TargetText, string TranslationTg, string TranslationRu,
    string? ExplanationTg, string? ExplanationRu, string ExampleTarget, string ExampleTg, string ExampleRu, int Status = 0,
    IReadOnlyList<RelationRequest>? Relations = null);
public sealed record RelationRequest(Guid RelatedWordId, int Type);
public sealed record VocabularyRelationDto(Guid Id, Guid RelatedWordId, string RelatedTargetText, int Type);
public sealed record VocabularyWordDto(Guid Id, Guid LanguageId, Guid TopicId, int Level, string TargetText, string Translation,
    string TranslationTg, string TranslationRu, string? Explanation, string? ExplanationTg, string? ExplanationRu,
    string ExampleTarget, string Example, string ExampleTg, string ExampleRu, int Status, IReadOnlyList<VocabularyRelationDto> Relations);

public sealed record QuestionOptionRequest(Guid? Id, Guid? WordId, string ValueTarget, string ValueTg, string ValueRu, int DisplayOrder, bool IsCorrect, int? CorrectOrder);
public sealed record QuestionUpsertRequest(Guid WordId, int Type, string PromptTarget, string PromptTg, string PromptRu, int? CorrectTokenIndex, IReadOnlyList<QuestionOptionRequest> Options);
public sealed record VocabularyQuestionOptionDto(Guid Id, Guid? WordId, string Value, string ValueTarget, string ValueTg, string ValueRu, int DisplayOrder, bool IsCorrect, int? CorrectOrder);
public sealed record VocabularyQuestionDto(Guid Id, Guid WordId, int Type, string Prompt, string PromptTarget, string PromptTg, string PromptRu,
    int? CorrectTokenIndex, int Status, Guid? ReviewedBy, DateTimeOffset? ReviewedAtUtc, IReadOnlyList<VocabularyQuestionOptionDto> Options);
public sealed record DraftGenerationWarning(int Type, string Code);
public sealed record DraftGenerationResult(IReadOnlyList<VocabularyQuestionDto> Created, IReadOnlyList<DraftGenerationWarning> Warnings);

public sealed record DailyWordUpsertRequest(Guid LanguageId, DateOnly LocalDate, Guid WordId);
public sealed record DailyWordDto(Guid LanguageId, DateOnly LocalDate, bool IsAutomatic, VocabularyWordDto Word);

public sealed record StudentVocabularyCourseRequest(Guid LanguageId, int Level);
public sealed record StudentVocabularyCourseDto(Guid LanguageId, string LanguageName, int Level, DateTimeOffset UpdatedAtUtc);
public sealed record StudentVocabularyDashboardDto(StudentVocabularyCourseDto Course, DailyWordDto Today, int MasteredWords, int DueReviews, int CompletedSessions, int TotalPracticedWords);

public sealed record StartVocabularySessionRequest(int Mode, Guid? TopicId = null, int? Level = null, int? QuestionCount = null);
public sealed record SubmitVocabularyAnswerRequest(Guid QuestionId, Guid? SelectedOptionId = null, int? SelectedTokenIndex = null, IReadOnlyList<Guid>? OrderedOptionIds = null);
public sealed record StudentVocabularyOptionDto(Guid Id, string Value, int DisplayOrder);
public sealed record StudentVocabularyQuestionDto(Guid Id, int Order, int Type, string Prompt, IReadOnlyList<StudentVocabularyOptionDto> Options, bool IsAnswered);
public sealed record VocabularySessionDto(Guid Id, int Mode, int Status, Guid LanguageId, int Level, Guid? TopicId, DateOnly LocalDate,
    int QuestionCount, int AnsweredCount, int CorrectCount, DateTimeOffset StartedAtUtc, DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<StudentVocabularyQuestionDto> Questions);
public sealed record VocabularyAnswerFeedbackDto(Guid QuestionId, bool IsCorrect, Guid? CorrectOptionId, int? CorrectTokenIndex, IReadOnlyList<Guid>? CorrectOrder);
public sealed record VocabularyAnswerResponse(VocabularySessionDto Session, VocabularyAnswerFeedbackDto? Feedback);
public sealed record VocabularySessionResultDto(Guid SessionId, int Mode, int QuestionCount, int CorrectCount, int WrongCount, decimal Percentage, DateTimeOffset CompletedAtUtc, IReadOnlyList<VocabularyAnswerFeedbackDto> Answers);
public sealed record VocabularyHistoryItemDto(Guid SessionId, int Mode, int QuestionCount, int CorrectCount, decimal Percentage, DateTimeOffset CompletedAtUtc);
public sealed record VocabularyMistakeDto(Guid WordId, string TargetText, string Translation, int WrongCount, int MasteryLevel, DateOnly? NextReviewDate);
