namespace Adeeb.Modules.QuestionBank.Contracts;

public sealed record StartSubjectTestRequest(Guid SubjectId, int QuestionCount, bool IncludeRedList = true);
public sealed record StartMmtPracticeRequest(bool StrictSimulation = false, int? QuestionCount = null);
public sealed record StartRedListPracticeRequest(int? QuestionCount = null);
public sealed record SubmitAttemptRequest(IReadOnlyList<SubmitAnswerDto> Answers);
public sealed record SubmitAnswerDto(Guid QuestionId, Guid? SelectedOptionId = null, string? TextResponse = null,
    IReadOnlyDictionary<Guid, string>? MatchingPairs = null);

public sealed record StudentTestingConfigDto(IReadOnlyList<int> SubjectQuestionCounts, int RedListMinimumQuestions,
    int RedListDefaultQuestions, int MmtPracticeDefaultQuestions, int MonthlyExamQuestionCount,
    int MmtDurationMinutes, bool MonthlyExamAvailable, DateTimeOffset? MonthlyExamClosesAtUtc);

public sealed record TestAttemptDto(Guid Id, int Mode, int Status, Guid? SubjectId, Guid? ClusterId,
    DateTimeOffset StartedAtUtc, DateTimeOffset ExpiresAtUtc, DateTimeOffset? SubmittedAtUtc,
    int QuestionCount, IReadOnlyList<TestQuestionDto> Questions);
public sealed record TestQuestionDto(Guid Id, int Order, Guid SubjectId, Guid? TopicId, int Type, int Difficulty,
    string Content, string? ImageUrl, IReadOnlyList<TestAnswerOptionDto> Options,
    IReadOnlyList<string> MatchingOptions);
public sealed record TestAnswerOptionDto(Guid Id, string Text);

public sealed record TestResultDto(Guid AttemptId, int Mode, int Status, int QuestionCount, int CorrectCount,
    int WrongCount, decimal Score, decimal Percentage, DateTimeOffset SubmittedAtUtc,
    IReadOnlyList<TopicBreakdownDto> TopicBreakdown, IReadOnlyList<SubjectBreakdownDto> SubjectBreakdown,
    IReadOnlyList<WeakTopicDto> WeakTopics, IReadOnlyList<TestAnswerResultDto> Answers);
public sealed record TopicBreakdownDto(Guid? TopicId, int Total, int Correct, int Wrong);
public sealed record SubjectBreakdownDto(Guid SubjectId, int Total, int Correct, int Wrong, decimal Percentage);
public sealed record WeakTopicDto(Guid SubjectId, Guid? TopicId, int Total, int Correct, decimal Percentage);
public sealed record TestAnswerResultDto(Guid QuestionId, Guid SubjectId, bool IsAnswered, bool IsCorrect, string Content,
    string? UserAnswer, string? CorrectAnswer, string? Explanation, Guid? TopicId, int Difficulty,
    int? CorrectPairsCount, int? TotalPairsCount);
public sealed record TestHistoryItemDto(Guid AttemptId, int Mode, int Status, DateTimeOffset StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc, int QuestionCount, int CorrectCount, decimal Percentage);
public sealed record TestHistoryQuery(int Page = 1, int PageSize = 10);

public sealed record RedListItemDto(Guid Id, Guid QuestionId, Guid SubjectId, Guid? TopicId, int QuestionType,
    int WrongCount, int CorrectStreak, DateTimeOffset LastWrongAtUtc, DateTimeOffset LastPracticedAtUtc, int Status,
    string QuestionContent);
public sealed record RedListSummaryDto(int ActiveCount, int MasteredCount, int ArchivedCount,
    IReadOnlyList<RedListSubjectSummaryDto> Subjects);
public sealed record RedListSubjectSummaryDto(Guid SubjectId, int ActiveCount);
public sealed record RedListQuery(Guid? SubjectId = null, int? Status = null, int Page = 1, int PageSize = 10);
public sealed record TestingPageDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
