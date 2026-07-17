using Adeeb.Modules.Students.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.Students.Infrastructure.Persistence;

internal static class StudentActivityPersistence
{
    public static bool UsesPostgres(StudentsDbContext db) =>
        string.Equals(
            db.Database.ProviderName,
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            StringComparison.Ordinal);

    public static async Task UpsertAsync(
        StudentsDbContext db,
        Guid studentId,
        DateOnly localDate,
        string timeZoneId,
        DateTimeOffset seenAtUtc,
        CancellationToken cancellationToken)
    {
        if (UsesPostgres(db))
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO students.student_daily_activities
                    (student_id, local_date, time_zone_id, first_seen_at_utc, last_seen_at_utc)
                VALUES
                    ({studentId}, {localDate}, {timeZoneId}, {seenAtUtc}, {seenAtUtc})
                ON CONFLICT (student_id, local_date)
                DO UPDATE SET last_seen_at_utc = GREATEST(
                    students.student_daily_activities.last_seen_at_utc,
                    EXCLUDED.last_seen_at_utc)
                """, cancellationToken);
            return;
        }

        var existing = await db.DailyActivities
            .SingleOrDefaultAsync(
                activity => activity.StudentId == studentId && activity.LocalDate == localDate,
                cancellationToken);
        if (existing is null)
        {
            db.DailyActivities.Add(new StudentDailyActivity(studentId, localDate, timeZoneId, seenAtUtc));
        }
        else
        {
            existing.MarkSeen(seenAtUtc);
        }
    }
}
