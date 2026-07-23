using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.QuestionBank.Domain;

public sealed class TestXpSettlement : Entity
{
    private TestXpSettlement() { }

    public TestXpSettlement(Guid id, Guid attemptId, Guid ledgerEntryId,
        int easyCorrectCount, int mediumCorrectCount, int hardCorrectCount,
        int answerXpUnits, int completionBonusXpUnits, int totalXpUnits, DateTimeOffset createdAtUtc)
    {
        if (easyCorrectCount < 0 || mediumCorrectCount < 0 || hardCorrectCount < 0
            || answerXpUnits < 0 || completionBonusXpUnits < 0 || totalXpUnits < 0)
            throw new ArgumentOutOfRangeException(nameof(totalXpUnits), "XP settlement values cannot be negative.");
        if (totalXpUnits != checked(answerXpUnits + completionBonusXpUnits))
            throw new ArgumentException("Total XP units must equal answer and completion units.", nameof(totalXpUnits));
        if (easyCorrectCount + mediumCorrectCount + hardCorrectCount == 0 && answerXpUnits != 0)
            throw new ArgumentException("A zero-correct settlement cannot award answer XP.", nameof(answerXpUnits));

        Id = id;
        AttemptId = attemptId;
        LedgerEntryId = ledgerEntryId;
        EasyCorrectCount = easyCorrectCount;
        MediumCorrectCount = mediumCorrectCount;
        HardCorrectCount = hardCorrectCount;
        AnswerXpUnits = answerXpUnits;
        CompletionBonusXpUnits = completionBonusXpUnits;
        TotalXpUnits = totalXpUnits;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid AttemptId { get; private set; }
    public Guid LedgerEntryId { get; private set; }
    public int EasyCorrectCount { get; private set; }
    public int MediumCorrectCount { get; private set; }
    public int HardCorrectCount { get; private set; }
    public int AnswerXpUnits { get; private set; }
    public int CompletionBonusXpUnits { get; private set; }
    public int TotalXpUnits { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
