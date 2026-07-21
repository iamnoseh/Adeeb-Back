using Adeeb.Application.Abstractions.Progression;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.QuestionBank.Infrastructure.Persistence;
using Adeeb.SharedKernel.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adeeb.QuestionBank.Tests;

public sealed class StudentXpServiceTests
{
    [Fact]
    public async Task Positive_credit_creates_ledger_and_returns_previous_and_new_balance()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var service = Service(db);

        var first = await service.GrantAsync(Credit(userId, "attempt-1", 13), default);
        var second = await service.GrantAsync(Credit(userId, "attempt-2", 7), default);

        Assert.Equal((0L, 13L), (first.Value!.PreviousBalanceUnits, first.Value.NewBalanceUnits));
        Assert.Equal((13L, 20L), (second.Value!.PreviousBalanceUnits, second.Value.NewBalanceUnits));
        Assert.Equal(20, (await db.StudentXpBalances.SingleAsync()).TotalXpUnits);
        Assert.Equal(2, await db.XpLedgerEntries.CountAsync());
    }

    [Fact]
    public async Task Zero_settlement_is_audited_without_creating_or_changing_balance()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();

        var result = await Service(db).GrantAsync(new(userId, XpSourceType.TestAttempt, "attempt-zero", 0,
            "test-xp:attempt-zero", XpEntryType.Settlement), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.NewBalanceUnits);
        Assert.Empty(await db.StudentXpBalances.ToListAsync());
        Assert.Equal(XpEntryType.Settlement, (await db.XpLedgerEntries.SingleAsync()).EntryType);
    }

    [Fact]
    public async Task Exact_duplicate_returns_original_entry_without_second_credit()
    {
        await using var db = CreateDb();
        var request = Credit(Guid.NewGuid(), "attempt-1", 13);
        var service = Service(db);

        var first = await service.GrantAsync(request, default);
        var duplicate = await service.GrantAsync(request, default);

        Assert.True(duplicate.Value!.WasAlreadyProcessed);
        Assert.Equal(first.Value!.LedgerEntryId, duplicate.Value.LedgerEntryId);
        Assert.Equal(13, (await db.StudentXpBalances.SingleAsync()).TotalXpUnits);
        Assert.Single(await db.XpLedgerEntries.ToListAsync());
    }

    [Fact]
    public async Task Duplicate_source_or_idempotency_with_different_payload_is_rejected()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var service = Service(db);
        await service.GrantAsync(Credit(userId, "attempt-1", 13), default);

        var sourceConflict = await service.GrantAsync(new(userId, XpSourceType.TestAttempt, "attempt-1", 7,
            "test-xp:other-key", XpEntryType.Credit), default);
        var keyConflict = await service.GrantAsync(new(userId, XpSourceType.TestAttempt, "attempt-2", 7,
            "test-xp:attempt-1", XpEntryType.Credit), default);

        Assert.Equal("xp.source_conflict", sourceConflict.Error?.Code);
        Assert.Equal("xp.source_conflict", keyConflict.Error?.Code);
        Assert.Equal(13, (await db.StudentXpBalances.SingleAsync()).TotalXpUnits);
    }

    [Fact]
    public async Task Invalid_amount_source_and_entry_semantics_fail_closed()
    {
        await using var db = CreateDb();
        var service = Service(db);
        var userId = Guid.NewGuid();

        var negative = await service.GrantAsync(new(userId, XpSourceType.TestAttempt, "a", -1, "k1", XpEntryType.Credit), default);
        var invalidSource = await service.GrantAsync(new(userId, (XpSourceType)999, "a", 1, "k2", XpEntryType.Credit), default);
        var zeroCredit = await service.GrantAsync(new(userId, XpSourceType.TestAttempt, "a", 0, "k3", XpEntryType.Credit), default);

        Assert.Equal("xp.invalid_amount", negative.Error?.Code);
        Assert.Equal("xp.invalid_source", invalidSource.Error?.Code);
        Assert.Equal("xp.invalid_amount", zeroCredit.Error?.Code);
        Assert.Empty(await db.XpLedgerEntries.ToListAsync());
    }

    [Fact]
    public async Task Overflow_and_negative_balance_mutation_are_prevented_and_ledger_is_immutable()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var balance = new StudentXpBalance(userId, FixedClock.Now);
        typeof(StudentXpBalance).GetProperty(nameof(StudentXpBalance.TotalXpUnits))!
            .SetValue(balance, long.MaxValue);
        db.StudentXpBalances.Add(balance);
        await db.SaveChangesAsync();

        var overflow = await Service(db).GrantAsync(Credit(userId, "attempt-overflow", 1), default);

        Assert.Equal("xp.balance_overflow", overflow.Error?.Code);
        Assert.Throws<ArgumentOutOfRangeException>(() => balance.Credit(-1, FixedClock.Now));
        Assert.All(typeof(XpLedgerEntry).GetProperties().Where(x => x.SetMethod is not null),
            property => Assert.False(property.SetMethod!.IsPublic));
    }

    [Fact]
    public void Ledger_enforces_credit_debit_settlement_and_adjustment_transitions()
    {
        var userId = Guid.NewGuid();
        var now = FixedClock.Now;

        _ = new XpLedgerEntry(Guid.NewGuid(), userId, XpSourceType.TestAttempt, "credit",
            XpEntryType.Credit, 10, "credit-key", 5, 15, null, now);
        _ = new XpLedgerEntry(Guid.NewGuid(), userId, XpSourceType.AdminAdjustment, "debit",
            XpEntryType.Debit, 4, "debit-key", 15, 11, null, now);
        _ = new XpLedgerEntry(Guid.NewGuid(), userId, XpSourceType.TestAttempt, "settlement",
            XpEntryType.Settlement, 0, "settlement-key", 11, 11, null, now);
        _ = new XpLedgerEntry(Guid.NewGuid(), userId, XpSourceType.AdminAdjustment, "adjustment",
            XpEntryType.Adjustment, 3, "adjustment-key", 11, 8, null, now);

        Assert.Throws<ArgumentException>(() => new XpLedgerEntry(Guid.NewGuid(), userId,
            XpSourceType.TestAttempt, "invalid-credit", XpEntryType.Credit, 10, "invalid-credit-key",
            5, 14, null, now));
        Assert.Throws<ArgumentException>(() => new XpLedgerEntry(Guid.NewGuid(), userId,
            XpSourceType.AdminAdjustment, "invalid-debit", XpEntryType.Debit, 20, "invalid-debit-key",
            15, 0, null, now));
        Assert.Throws<ArgumentException>(() => new XpLedgerEntry(Guid.NewGuid(), userId,
            XpSourceType.TestAttempt, "invalid-settlement", XpEntryType.Settlement, 1,
            "invalid-settlement-key", 11, 11, null, now));
    }

    private static XpGrantRequest Credit(Guid userId, string sourceId, int amount) => new(
        userId, XpSourceType.TestAttempt, sourceId, amount, $"test-xp:{sourceId}", XpEntryType.Credit);

    private static StudentXpService Service(QuestionBankDbContext db) =>
        new(db, new FixedClock(), NullLogger<StudentXpService>.Instance);

    private static QuestionBankDbContext CreateDb() => new(new DbContextOptionsBuilder<QuestionBankDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class FixedClock : IDateTimeProvider
    {
        public static readonly DateTimeOffset Now = new(2026, 7, 20, 9, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow => Now;
        public DateTimeOffset DushanbeNow => Now.ToOffset(TimeSpan.FromHours(5));
        public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(5));
    }
}
