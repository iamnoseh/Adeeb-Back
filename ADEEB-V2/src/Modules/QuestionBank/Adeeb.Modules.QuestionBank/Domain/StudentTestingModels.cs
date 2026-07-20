using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.QuestionBank.Domain;

public sealed class TestAttempt : Entity
{
    private readonly List<TestAttemptQuestion> _questions = [];
    private readonly List<TestAttemptAnswer> _answers = [];
    private TestAttempt() { }

    public TestAttempt(Guid id, Guid userId, TestMode mode, Guid? subjectId, Guid? clusterId, string? monthlyWindowKey,
        int questionCount, DateTimeOffset now, DateTimeOffset expiresAt)
    {
        Id = id; UserId = userId; Mode = mode; SubjectId = subjectId; ClusterId = clusterId;
        MonthlyWindowKey = monthlyWindowKey; QuestionCount = questionCount; Status = TestAttemptStatus.Created;
        CreatedAtUtc = now; Start(now, expiresAt);
    }

    public Guid UserId { get; private set; }
    public TestMode Mode { get; private set; }
    public Guid? SubjectId { get; private set; }
    public Guid? ClusterId { get; private set; }
    public string? MonthlyWindowKey { get; private set; }
    public TestAttemptStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public int QuestionCount { get; private set; }
    public int CorrectCount { get; private set; }
    public int WrongCount { get; private set; }
    public decimal Score { get; private set; }
    public decimal Percentage { get; private set; }
    public IReadOnlyCollection<TestAttemptQuestion> Questions => _questions;
    public IReadOnlyCollection<TestAttemptAnswer> Answers => _answers;
    public TestAttemptResult? Result { get; private set; }
    public TestXpReward? XpReward { get; private set; }

    private void Start(DateTimeOffset now, DateTimeOffset expiresAt)
    {
        StartedAtUtc = now; ExpiresAtUtc = expiresAt; Status = TestAttemptStatus.InProgress;
    }

    public void Complete(int correct, int wrong, decimal score, decimal percentage, DateTimeOffset now, bool automatic)
    {
        if (Status != TestAttemptStatus.InProgress) throw new InvalidOperationException("Attempt is not in progress.");
        CorrectCount = correct; WrongCount = wrong; Score = score; Percentage = percentage;
        SubmittedAtUtc = now; Status = automatic ? TestAttemptStatus.AutoSubmitted : TestAttemptStatus.Submitted;
    }
}

public sealed class TestAttemptQuestion : Entity
{
    private TestAttemptQuestion() { }
    public TestAttemptQuestion(Guid id, Guid attemptId, Guid questionId, int displayOrder, Guid subjectId, Guid? topicId,
        QuestionType type, DifficultyLevel difficulty, string snapshotJson)
    {
        Id = id; TestAttemptId = attemptId; QuestionId = questionId; DisplayOrder = displayOrder;
        SubjectId = subjectId; TopicId = topicId; QuestionType = type; Difficulty = difficulty; QuestionSnapshotJson = snapshotJson;
    }
    public Guid TestAttemptId { get; private set; }
    public Guid QuestionId { get; private set; }
    public int DisplayOrder { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid? TopicId { get; private set; }
    public QuestionType QuestionType { get; private set; }
    public DifficultyLevel Difficulty { get; private set; }
    public string QuestionSnapshotJson { get; private set; } = string.Empty;
    public TestAttempt TestAttempt { get; private set; } = null!;
}

public sealed class TestAttemptAnswer : Entity
{
    private TestAttemptAnswer() { }
    public TestAttemptAnswer(Guid id, Guid attemptId, Guid attemptQuestionId, Guid questionId, string answerSnapshotJson,
        bool isAnswered, bool isCorrect, int? correctPairs, int? totalPairs, DateTimeOffset now)
    {
        Id = id; TestAttemptId = attemptId; TestAttemptQuestionId = attemptQuestionId; QuestionId = questionId;
        AnswerSnapshotJson = answerSnapshotJson; IsAnswered = isAnswered; IsCorrect = isCorrect;
        CorrectPairsCount = correctPairs; TotalPairsCount = totalPairs; AnsweredAtUtc = now;
    }
    public Guid TestAttemptId { get; private set; }
    public Guid TestAttemptQuestionId { get; private set; }
    public Guid QuestionId { get; private set; }
    public string AnswerSnapshotJson { get; private set; } = string.Empty;
    public bool IsAnswered { get; private set; }
    public bool IsCorrect { get; private set; }
    public int? CorrectPairsCount { get; private set; }
    public int? TotalPairsCount { get; private set; }
    public DateTimeOffset AnsweredAtUtc { get; private set; }
}

public sealed class TestAttemptResult : Entity
{
    private TestAttemptResult() { }
    public TestAttemptResult(Guid id, Guid attemptId, string topicBreakdownJson, string resultSnapshotJson, DateTimeOffset now)
    { Id = id; TestAttemptId = attemptId; TopicBreakdownJson = topicBreakdownJson; ResultSnapshotJson = resultSnapshotJson; CreatedAtUtc = now; }
    public Guid TestAttemptId { get; private set; }
    public string TopicBreakdownJson { get; private set; } = string.Empty;
    public string ResultSnapshotJson { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public sealed class StudentRedListItem : Entity
{
    private StudentRedListItem() { }
    public StudentRedListItem(Guid id, Guid userId, Guid questionId, Guid subjectId, Guid? topicId, QuestionType type, DateTimeOffset now)
    {
        Id = id; UserId = userId; QuestionId = questionId; SubjectId = subjectId; TopicId = topicId; QuestionType = type;
        WrongCount = 1; CorrectStreak = 0; LastWrongAtUtc = now; LastPracticedAtUtc = now; Status = RedListStatus.Active;
    }
    public Guid UserId { get; private set; }
    public Guid QuestionId { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid? TopicId { get; private set; }
    public QuestionType QuestionType { get; private set; }
    public int WrongCount { get; private set; }
    public int CorrectStreak { get; private set; }
    public DateTimeOffset LastWrongAtUtc { get; private set; }
    public DateTimeOffset LastPracticedAtUtc { get; private set; }
    public RedListStatus Status { get; private set; }
    public DateTimeOffset? MasteredAtUtc { get; private set; }

    public void RecordWrong(DateTimeOffset now)
    { WrongCount++; CorrectStreak = 0; LastWrongAtUtc = now; LastPracticedAtUtc = now; Status = RedListStatus.Active; MasteredAtUtc = null; }
    public void RecordCorrect(DateTimeOffset now)
    { LastPracticedAtUtc = now; CorrectStreak++; if (CorrectStreak >= 3) { CorrectStreak = 3; Status = RedListStatus.Mastered; MasteredAtUtc = now; } }
    public void Archive() => Status = RedListStatus.Archived;
    public void Restore() { Status = RedListStatus.Active; CorrectStreak = 0; MasteredAtUtc = null; }
}
