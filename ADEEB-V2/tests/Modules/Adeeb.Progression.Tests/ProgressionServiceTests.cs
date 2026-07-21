using Adeeb.Application.Abstractions.Progression;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Progression.Application;
using Adeeb.Modules.Progression.Contracts;
using Adeeb.Modules.Progression.Infrastructure.Persistence;
using Adeeb.Modules.Students.Contracts;
using Adeeb.SharedKernel.Progression;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Progression.Tests;

public sealed class ProgressionServiceTests
{
    [Fact]
    public async Task Duplicate_xp_delivery_is_idempotent_and_places_new_student_immediately()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new Clock();
        var xp = new XpRead([new(userId, 200, clock.UtcNow)]);
        var service = new ProgressionService(db, xp, new Directory(userId), clock);
        await ConfigureAndStartAsync(service);
        var ledgerId = Guid.NewGuid();
        var message = new XpGrantedIntegrationEvent(ledgerId, userId, XpSourceType.TestAttempt,
            XpEntryType.Credit, 20, 220, clock.UtcNow.AddMinutes(1));

        await service.HandleAsync(message, default);
        await service.HandleAsync(message, default);

        var membership = await db.Memberships.SingleAsync(x => x.UserId == userId);
        Assert.Equal(20, membership.SeasonScoreUnits);
        Assert.Equal(1, await db.ScoreEvents.CountAsync());
    }

    [Fact]
    public async Task Admin_adjustment_does_not_change_season_score()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new Clock();
        var service = new ProgressionService(db, new XpRead([new(userId, 200, clock.UtcNow)]),
            new Directory(userId), clock);
        await ConfigureAndStartAsync(service);

        await service.HandleAsync(new(Guid.NewGuid(), userId, XpSourceType.AdminAdjustment,
            XpEntryType.Credit, 40, 240, clock.UtcNow.AddMinutes(1)), default);

        Assert.Equal(0, (await db.Memberships.SingleAsync()).SeasonScoreUnits);
        Assert.Empty(db.ScoreEvents);
    }

    [Fact]
    public async Task Start_rejects_non_contiguous_thresholds()
    {
        await using var db = CreateDb();
        var service = new ProgressionService(db, new XpRead([]), new Directory(), new Clock());
        Assert.True((await service.CreateLeagueAsync(Form("Starter", 0, 100, 1), null, default)).IsSuccess);
        Assert.False((await service.CreateLeagueAsync(Form("Gold", 120, null, 2), null, default)).IsSuccess);
        Assert.True((await service.StartSeasonAsync(default)).IsFailure);
    }

    [Fact]
    public async Task Season_closure_recovers_member_from_delayed_outbox_event()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new Clock();
        var xp = new XpRead([]);
        var service = new ProgressionService(db, xp, new Directory(userId), clock);
        await ConfigureAndStartAsync(service);
        xp.Balances = [new(userId, 200, clock.UtcNow)];
        xp.Aggregates = [new(userId, 40, clock.UtcNow.AddDays(1))];
        clock.Advance(TimeSpan.FromDays(11));

        await service.ProcessExpiredSeasonAsync(default);

        var closedMember = await db.Memberships.OrderBy(x => x.JoinedAtUtc).FirstAsync(x => x.UserId == userId);
        Assert.Equal(40, closedMember.SeasonScoreUnits);
        Assert.Equal(1, closedMember.FinalRank);
        Assert.Single(db.MovementResults);
    }

    private static async Task ConfigureAndStartAsync(ProgressionService service)
    {
        Assert.True((await service.CreateLeagueAsync(Form("Starter", 0, 100, 1), null, default)).IsSuccess);
        Assert.True((await service.CreateLeagueAsync(Form("Gold", 100, null, 2), null, default)).IsSuccess);
        Assert.True((await service.StartSeasonAsync(default)).IsSuccess);
    }

    private static LeagueFormRequest Form(string name, decimal min, decimal? max, int order) =>
        new() { NameTg = name, NameRu = name, MinXp = min, MaxXp = max, DisplayOrder = order, IsActive = true };

    private static ProgressionDbContext CreateDb() => new(new DbContextOptionsBuilder<ProgressionDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; private set; } = new(2026, 7, 21, 10, 0, 0, TimeSpan.Zero);
        public DateTimeOffset DushanbeNow => UtcNow.AddHours(5);
        public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.AddHours(5);
        public void Advance(TimeSpan value) => UtcNow = UtcNow.Add(value);
    }

    private sealed class Directory(params Guid[] users) : IStudentCompetitionDirectory
    {
        public Task<IReadOnlyDictionary<Guid, StudentCompetitionReference>> GetByIdentityUserIdsAsync(
            IReadOnlyCollection<Guid> identityUserIds, CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, StudentCompetitionReference> result = identityUserIds
                .Where(users.Contains).ToDictionary(x => x, x => new StudentCompetitionReference(x, $"Student {x:N}", null, true));
            return Task.FromResult(result);
        }
    }

    private sealed class XpRead(IReadOnlyList<StudentXpBalanceSnapshot> balances) : IStudentXpReadService
    {
        public IReadOnlyList<StudentXpBalanceSnapshot> Balances { get; set; } = balances;
        public IReadOnlyList<StudentSeasonXpAggregate> Aggregates { get; set; } = [];
        public Task<StudentXpRankSnapshot> GetRankAsync(Guid userId, CancellationToken cancellationToken)
        {
            var ordered = Balances.OrderByDescending(x => x.TotalXpUnits).ThenBy(x => x.UserId).ToList();
            var index = ordered.FindIndex(x => x.UserId == userId);
            var value = index < 0 ? new StudentXpRankSnapshot(0, null, ordered.Count, null)
                : new StudentXpRankSnapshot(ordered[index].TotalXpUnits, index + 1, ordered.Count, ordered[index].UpdatedAtUtc);
            return Task.FromResult(value);
        }

        public Task<IReadOnlyList<StudentXpBalanceSnapshot>> GetPositiveBalancesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Balances);

        public Task<IReadOnlyList<StudentSeasonXpAggregate>> GetSeasonAggregatesAsync(DateTimeOffset startsAtUtc,
            DateTimeOffset endsAtUtc, CancellationToken cancellationToken) =>
            Task.FromResult(Aggregates);
    }
}
