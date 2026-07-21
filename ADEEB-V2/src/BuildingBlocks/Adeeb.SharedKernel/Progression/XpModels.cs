using Adeeb.SharedKernel.Domain;

namespace Adeeb.SharedKernel.Progression;

public enum XpSourceType
{
    TestAttempt = 1,
    VocabularySession = 2,
    Duel = 3,
    Mission = 4,
    DailyTask = 5,
    Streak = 6,
    RedListActivity = 7,
    ClanCompetition = 8,
    Achievement = 9,
    Event = 10,
    AdminAdjustment = 11
}

public enum XpEntryType
{
    Credit = 1,
    Debit = 2,
    Settlement = 3,
    Adjustment = 4
}

public sealed class XpLedgerEntry : Entity
{
    private XpLedgerEntry() { }

    public XpLedgerEntry(Guid id, Guid userId, XpSourceType sourceType, string sourceId,
        XpEntryType entryType, int amountUnits, string idempotencyKey, long balanceBeforeUnits,
        long balanceAfterUnits, string? metadataJson, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Ledger entry ID is required.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        if (!Enum.IsDefined(sourceType)) throw new ArgumentOutOfRangeException(nameof(sourceType));
        if (!Enum.IsDefined(entryType)) throw new ArgumentOutOfRangeException(nameof(entryType));
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Source ID is required.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        if (amountUnits < 0) throw new ArgumentOutOfRangeException(nameof(amountUnits));
        if (balanceBeforeUnits < 0 || balanceAfterUnits < 0)
            throw new ArgumentOutOfRangeException(nameof(balanceAfterUnits));
        ValidateTransition(entryType, amountUnits, balanceBeforeUnits, balanceAfterUnits);

        Id = id;
        UserId = userId;
        SourceType = sourceType;
        SourceId = sourceId;
        EntryType = entryType;
        AmountUnits = amountUnits;
        IdempotencyKey = idempotencyKey;
        BalanceBeforeUnits = balanceBeforeUnits;
        BalanceAfterUnits = balanceAfterUnits;
        MetadataJson = metadataJson;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }
    public XpSourceType SourceType { get; private set; }
    public string SourceId { get; private set; } = string.Empty;
    public XpEntryType EntryType { get; private set; }
    public int AmountUnits { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public long BalanceBeforeUnits { get; private set; }
    public long BalanceAfterUnits { get; private set; }
    public string? MetadataJson { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private static void ValidateTransition(XpEntryType entryType, int amountUnits, long before, long after)
    {
        var valid = entryType switch
        {
            XpEntryType.Credit => amountUnits > 0 && after == checked(before + amountUnits),
            XpEntryType.Debit => amountUnits > 0 && before >= amountUnits && after == before - amountUnits,
            XpEntryType.Settlement => amountUnits == 0 && after == before,
            XpEntryType.Adjustment => amountUnits > 0 && Math.Abs(checked(after - before)) == amountUnits,
            _ => false
        };
        if (!valid) throw new ArgumentException("XP ledger balance transition does not match its entry type and amount.");
    }
}

public sealed class StudentXpBalance
{
    private StudentXpBalance() { }

    public StudentXpBalance(Guid userId, DateTimeOffset now)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        UserId = userId;
        UpdatedAtUtc = now;
    }

    public Guid UserId { get; private set; }
    public long TotalXpUnits { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public (long Previous, long Current) Credit(int amountUnits, DateTimeOffset now)
    {
        if (amountUnits <= 0) throw new ArgumentOutOfRangeException(nameof(amountUnits));
        var previous = TotalXpUnits;
        TotalXpUnits = checked(TotalXpUnits + amountUnits);
        UpdatedAtUtc = now;
        return (previous, TotalXpUnits);
    }
}
