using Adeeb.Modules.QuestionBank.Infrastructure.Persistence;
using Adeeb.SharedKernel.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Adeeb.IntegrationTests;

public sealed class GlobalXpMigrationIntegrationScenarios(AdeebApiFactory factory)
    : IClassFixture<AdeebApiFactory>
{
    [Fact]
    public async Task Existing_test_rewards_and_balance_migrate_without_double_credit_and_down_round_trips()
    {
        await using var db = CreateDb();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260720133652_AddTestXpRewards");
        var userId = Guid.NewGuid();
        var positiveAttempt = Guid.NewGuid();
        var zeroAttempt = Guid.NewGuid();
        await SeedLegacyXpAsync(db, userId, positiveAttempt, zeroAttempt);

        await migrator.MigrateAsync();

        Assert.Equal(13, (await db.StudentXpBalances.AsNoTracking().SingleAsync()).TotalXpUnits);
        var ledger = await db.XpLedgerEntries.AsNoTracking().OrderBy(x => x.CreatedAtUtc).ToListAsync();
        Assert.Equal(2, ledger.Count);
        Assert.Equal(13, ledger[0].AmountUnits);
        Assert.Equal(XpEntryType.Credit, ledger[0].EntryType);
        Assert.Equal("test-xp:positive", ledger[0].IdempotencyKey);
        Assert.Equal(0, ledger[1].AmountUnits);
        Assert.Equal(XpEntryType.Settlement, ledger[1].EntryType);
        Assert.Equal(2, await db.TestXpSettlements.AsNoTracking().CountAsync());

        await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(
            "UPDATE question_bank.student_xp_balances SET total_xp_units = -1"));
        db.ChangeTracker.Clear();

        await migrator.MigrateAsync("20260720133652_AddTestXpRewards");
        await using var command = new NpgsqlCommand(
            "SELECT count(*), sum(total_xp_units) FROM question_bank.test_xp_rewards;",
            (NpgsqlConnection)db.Database.GetDbConnection());
        if (command.Connection!.State != System.Data.ConnectionState.Open) await command.Connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(2, reader.GetInt64(0));
        Assert.Equal(13, reader.GetInt64(1));
    }

    private QuestionBankDbContext CreateDb() => new(new DbContextOptionsBuilder<QuestionBankDbContext>()
        .UseNpgsql(factory.ConnectionString).Options);

    private static async Task SeedLegacyXpAsync(QuestionBankDbContext db, Guid userId, Guid positiveAttempt, Guid zeroAttempt)
    {
        var positiveReward = Guid.NewGuid();
        var zeroReward = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($$"""
            INSERT INTO question_bank.test_attempts
                (id, user_id, mode, status, created_at_utc, started_at_utc, expires_at_utc, submitted_at_utc,
                 question_count, correct_count, wrong_count, score, percentage)
            VALUES
                ({{positiveAttempt}}, {{userId}}, 'SubjectTest', 'Submitted', TIMESTAMPTZ '2026-07-20 08:00:00+00',
                 TIMESTAMPTZ '2026-07-20 08:00:00+00', TIMESTAMPTZ '2026-07-20 09:00:00+00',
                 TIMESTAMPTZ '2026-07-20 08:10:00+00', 1, 1, 0, 1, 100),
                ({{zeroAttempt}}, {{userId}}, 'SubjectTest', 'Submitted', TIMESTAMPTZ '2026-07-20 09:00:00+00',
                 TIMESTAMPTZ '2026-07-20 09:00:00+00', TIMESTAMPTZ '2026-07-20 10:00:00+00',
                 TIMESTAMPTZ '2026-07-20 09:10:00+00', 1, 0, 1, 0, 0);

            INSERT INTO question_bank.student_test_xp_balances (user_id, total_xp_units, updated_at_utc)
            VALUES ({{userId}}, 13, TIMESTAMPTZ '2026-07-20 09:10:00+00');

            INSERT INTO question_bank.test_xp_rewards
                (id, user_id, attempt_id, test_mode, reward_type, easy_correct_count, medium_correct_count,
                 hard_correct_count, answer_xp_units, completion_bonus_xp_units, total_xp_units,
                 idempotency_key, created_at_utc)
            VALUES
                ({{positiveReward}}, {{userId}}, {{positiveAttempt}}, 'SubjectTest', 'AttemptCompletion',
                 1, 0, 0, 3, 10, 13, 'test-xp:positive', TIMESTAMPTZ '2026-07-20 08:10:00+00'),
                ({{zeroReward}}, {{userId}}, {{zeroAttempt}}, 'SubjectTest', 'AttemptCompletion',
                 0, 0, 0, 0, 0, 0, 'test-xp:zero', TIMESTAMPTZ '2026-07-20 09:10:00+00');
            """);
    }
}
