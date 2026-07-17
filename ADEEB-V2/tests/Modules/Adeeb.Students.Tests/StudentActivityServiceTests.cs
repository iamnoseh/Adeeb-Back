using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Students.Application;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Students.Domain.Students;
using Adeeb.Modules.Students.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Students.Tests;

public sealed class StudentActivityServiceTests
{
    [Fact]
    public async Task First_and_repeated_visit_create_one_active_day()
    {
        await using var db = CreateDb();
        var (student, principal) = await AddStudentAsync(db);
        var service = new StudentActivityService(db, new MutableClock(new(2026, 7, 16, 20, 30, 0, TimeSpan.Zero)));

        var first = await service.RecordVisitAsync(principal, new("Asia/Dushanbe"), CancellationToken.None);
        var second = await service.RecordVisitAsync(principal, new("Asia/Dushanbe"), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(new DateOnly(2026, 7, 17), first.Value!.TodayLocalDate);
        Assert.Equal(1, first.Value.CurrentStreak);
        Assert.Equal(1, second.Value!.ActiveDaysInMonth);
        Assert.Equal(1, await db.DailyActivities.CountAsync(x => x.StudentId == student.Id));
    }

    [Fact]
    public async Task Time_zone_change_keeps_historical_local_date_immutable()
    {
        await using var db = CreateDb();
        var (student, principal) = await AddStudentAsync(db);
        var clock = new MutableClock(new(2026, 7, 17, 2, 0, 0, TimeSpan.Zero));
        var service = new StudentActivityService(db, clock);
        await service.RecordVisitAsync(principal, new("Asia/Dushanbe"), CancellationToken.None);

        clock.UtcNow = new(2026, 7, 18, 2, 0, 0, TimeSpan.Zero);
        var changed = await service.RecordVisitAsync(principal, new("America/Los_Angeles"), CancellationToken.None);

        var dates = await db.DailyActivities.Where(x => x.StudentId == student.Id).ToListAsync();
        Assert.Single(dates);
        Assert.Equal(new DateOnly(2026, 7, 17), dates[0].LocalDate);
        Assert.Equal("Asia/Dushanbe", dates[0].TimeZoneId);
        Assert.Equal("America/Los_Angeles", changed.Value!.TimeZoneId);
        Assert.Equal("America/Los_Angeles", student.Profile.TimeZoneId);
    }

    [Fact]
    public async Task Invalid_time_zone_and_period_return_validation_errors()
    {
        await using var db = CreateDb();
        var (_, principal) = await AddStudentAsync(db);
        var service = new StudentActivityService(db, new MutableClock(new(2026, 7, 17, 8, 0, 0, TimeSpan.Zero)));

        var invalidZone = await service.RecordVisitAsync(principal, new("Not/AZone"), CancellationToken.None);
        var invalidPeriod = await service.GetCalendarAsync(principal, 2026, 13, CancellationToken.None);

        Assert.True(invalidZone.IsFailure);
        Assert.Contains("timeZoneId", invalidZone.ValidationErrors!.Keys);
        Assert.True(invalidPeriod.IsFailure);
        Assert.Contains("month", invalidPeriod.ValidationErrors!.Keys);
    }

    [Theory]
    [InlineData(StudentStatus.Suspended, "student.suspended")]
    [InlineData(StudentStatus.Closed, "student.closed")]
    public async Task Non_active_student_cannot_record_activity(StudentStatus status, string expectedError)
    {
        await using var db = CreateDb();
        var (student, principal) = await AddStudentAsync(db);
        student.ChangeStatus(status, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        var service = new StudentActivityService(db, new MutableClock(DateTimeOffset.UtcNow));

        var result = await service.RecordVisitAsync(principal, new("Asia/Dushanbe"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error!.Code);
        Assert.Empty(db.DailyActivities);
    }

    [Fact]
    public async Task Calendar_calculates_current_longest_month_and_leap_year_streaks()
    {
        await using var db = CreateDb();
        var (student, principal) = await AddStudentAsync(db);
        var dates = new[]
        {
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 2),
            new DateOnly(2024, 2, 28),
            new DateOnly(2024, 2, 29),
            new DateOnly(2024, 3, 1),
            new DateOnly(2024, 3, 2),
        };
        db.DailyActivities.AddRange(dates.Select(date =>
            new StudentDailyActivity(student.Id, date, "Asia/Dushanbe", DateTimeOffset.UtcNow)));
        await db.SaveChangesAsync();
        var service = new StudentActivityService(db, new MutableClock(new(2024, 3, 2, 3, 0, 0, TimeSpan.Zero)));

        var result = await service.GetCalendarAsync(principal, 2024, 2, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.CurrentStreak);
        Assert.Equal(4, result.Value.LongestStreak);
        Assert.Equal(2, result.Value.ActiveDaysInMonth);
        Assert.Equal(6, result.Value.TotalActiveDays);
        Assert.Equal([new DateOnly(2024, 2, 28), new DateOnly(2024, 2, 29)], result.Value.Days.Select(x => x.Date));
    }

    [Fact]
    public async Task Current_streak_anchors_on_yesterday_before_today_is_active()
    {
        await using var db = CreateDb();
        var (student, principal) = await AddStudentAsync(db);
        db.DailyActivities.AddRange(
            new StudentDailyActivity(student.Id, new(2026, 7, 15), "Asia/Dushanbe", DateTimeOffset.UtcNow),
            new StudentDailyActivity(student.Id, new(2026, 7, 16), "Asia/Dushanbe", DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
        var service = new StudentActivityService(db, new MutableClock(new(2026, 7, 17, 3, 0, 0, TimeSpan.Zero)));

        var result = await service.GetCalendarAsync(principal, null, null, CancellationToken.None);

        Assert.Equal(2, result.Value!.CurrentStreak);
        Assert.Equal(2026, result.Value.Year);
        Assert.Equal(7, result.Value.Month);
    }

    [Fact]
    public async Task Empty_month_returns_stable_zero_summary()
    {
        await using var db = CreateDb();
        var (_, principal) = await AddStudentAsync(db);
        var service = new StudentActivityService(db, new MutableClock(new(2026, 7, 17, 3, 0, 0, TimeSpan.Zero)));

        var result = await service.GetCalendarAsync(principal, 2025, 12, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.CurrentStreak);
        Assert.Equal(0, result.Value.LongestStreak);
        Assert.Equal(0, result.Value.ActiveDaysInMonth);
        Assert.Equal(0, result.Value.TotalActiveDays);
        Assert.Empty(result.Value.Days);
    }

    private static StudentsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<StudentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StudentsDbContext(options);
    }

    private static async Task<(Student Student, System.Security.Claims.ClaimsPrincipal Principal)> AddStudentAsync(StudentsDbContext db)
    {
        var identityUserId = Guid.NewGuid();
        var student = new Student(Guid.NewGuid(), identityUserId, DateTimeOffset.UtcNow);
        db.Students.Add(student);
        await db.SaveChangesAsync();
        return (student, TestPrincipal.ForUser(identityUserId));
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
        public DateTimeOffset DushanbeNow => UtcNow.ToOffset(TimeSpan.FromHours(5));
        public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(5));
    }
}
