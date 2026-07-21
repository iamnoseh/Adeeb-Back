using Adeeb.Modules.Progression.Application;
using Adeeb.Modules.Progression.Domain;

namespace Adeeb.Progression.Tests;

public sealed class ProgressionDomainTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(3, 1)]
    [InlineData(20, 6)]
    [InlineData(30, 10)]
    [InlineData(100, 10)]
    public void Movement_count_is_dynamic_and_capped(int participants, int expected) =>
        Assert.Equal(expected, ProgressionService.MovementCount(participants));

    [Fact]
    public void Season_is_exactly_ten_days()
    {
        var start = new DateTimeOffset(2026, 7, 21, 8, 0, 0, TimeSpan.Zero);
        var season = new LeagueSeason(Guid.NewGuid(), 1, start, 1, true, start);
        Assert.Equal(TimeSpan.FromDays(10), season.EndsAtUtc - season.StartsAtUtc);
    }

    [Fact]
    public void League_rejects_invalid_range() => Assert.Throws<ArgumentException>(() =>
        new LeagueDefinition(Guid.NewGuid(), "Лига", "Лига", null, 20, 20, 1, 1, DateTimeOffset.UtcNow));
}
