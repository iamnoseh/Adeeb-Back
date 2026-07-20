using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.QuestionBank.Domain;

public enum TestXpRewardType
{
    AttemptCompletion = 1
}

public sealed class TestXpReward : Entity
{
    private TestXpReward() { }

    public TestXpReward(Guid id, Guid userId, Guid attemptId, TestMode testMode,
        int easyCorrectCount, int mediumCorrectCount, int hardCorrectCount,
        int answerXpUnits, int completionBonusXpUnits, int totalXpUnits, DateTimeOffset createdAtUtc)
    {
        if (easyCorrectCount < 0 || mediumCorrectCount < 0 || hardCorrectCount < 0
            || answerXpUnits < 0 || completionBonusXpUnits < 0 || totalXpUnits < 0)
            throw new ArgumentOutOfRangeException(nameof(totalXpUnits), "XP reward values cannot be negative.");
        if (totalXpUnits != checked(answerXpUnits + completionBonusXpUnits))
            throw new ArgumentException("Total XP units must equal answer and completion units.", nameof(totalXpUnits));

        Id = id;
        UserId = userId;
        AttemptId = attemptId;
        TestMode = testMode;
        RewardType = TestXpRewardType.AttemptCompletion;
        EasyCorrectCount = easyCorrectCount;
        MediumCorrectCount = mediumCorrectCount;
        HardCorrectCount = hardCorrectCount;
        AnswerXpUnits = answerXpUnits;
        CompletionBonusXpUnits = completionBonusXpUnits;
        TotalXpUnits = totalXpUnits;
        IdempotencyKey = ForAttempt(attemptId);
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }
    public Guid AttemptId { get; private set; }
    public TestMode TestMode { get; private set; }
    public TestXpRewardType RewardType { get; private set; }
    public int EasyCorrectCount { get; private set; }
    public int MediumCorrectCount { get; private set; }
    public int HardCorrectCount { get; private set; }
    public int AnswerXpUnits { get; private set; }
    public int CompletionBonusXpUnits { get; private set; }
    public int TotalXpUnits { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static string ForAttempt(Guid attemptId) => $"test-xp:{attemptId:N}";
}

public sealed class StudentTestXpBalance
{
    private StudentTestXpBalance() { }

    public StudentTestXpBalance(Guid userId, DateTimeOffset now)
    {
        UserId = userId;
        UpdatedAtUtc = now;
    }

    public Guid UserId { get; private set; }
    public long TotalXpUnits { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Credit(int xpUnits, DateTimeOffset now)
    {
        if (xpUnits < 0) throw new ArgumentOutOfRangeException(nameof(xpUnits));
        TotalXpUnits = checked(TotalXpUnits + xpUnits);
        UpdatedAtUtc = now;
    }
}
