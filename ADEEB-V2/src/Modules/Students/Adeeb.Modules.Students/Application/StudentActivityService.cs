using System.Security.Claims;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Students.Domain.Students;
using Adeeb.Modules.Students.Infrastructure.Persistence;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Adeeb.Modules.Students.Application;

public sealed class StudentActivityService(StudentsDbContext db, IDateTimeProvider clock)
{
    public async Task<Result<StudentActivityCalendarResponse>> RecordVisitAsync(
        ClaimsPrincipal principal,
        StudentActivityVisitRequest request,
        CancellationToken cancellationToken)
    {
        var identityUserId = GetUserId(principal);
        if (identityUserId is null)
        {
            return Result<StudentActivityCalendarResponse>.Failure(StudentErrors.ProvisioningRequired);
        }

        var requestedTimeZoneId = string.IsNullOrWhiteSpace(request.TimeZoneId)
            ? StudentProfile.DefaultTimeZoneId
            : request.TimeZoneId.Trim();
        if (!TryGetTimeZone(requestedTimeZoneId, out var timeZone))
        {
            return InvalidTimeZone();
        }

        IDbContextTransaction? transaction = null;
        if (StudentActivityPersistence.UsesPostgres(db))
        {
            transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        }

        await using (transaction)
        {
            var student = await db.Students
                .Include(x => x.Profile)
                .SingleOrDefaultAsync(x => x.IdentityUserId == identityUserId.Value, cancellationToken);
            var accessError = AccessError(student);
            if (accessError is not null)
            {
                return Result<StudentActivityCalendarResponse>.Failure(accessError);
            }

            var now = clock.UtcNow;
            var localDate = LocalDate(now, timeZone!);
            student!.Profile.ChangeTimeZone(timeZone!.Id, now);
            await StudentActivityPersistence.UpsertAsync(
                db,
                student.Id,
                localDate,
                timeZone.Id,
                now,
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return Result<StudentActivityCalendarResponse>.Success(
                await BuildCalendarAsync(student.Id, timeZone.Id, localDate.Year, localDate.Month, localDate, cancellationToken));
        }
    }

    public async Task<Result<StudentActivityCalendarResponse>> GetCalendarAsync(
        ClaimsPrincipal principal,
        int? year,
        int? month,
        CancellationToken cancellationToken)
    {
        var identityUserId = GetUserId(principal);
        if (identityUserId is null)
        {
            return Result<StudentActivityCalendarResponse>.Failure(StudentErrors.ProvisioningRequired);
        }

        var student = await db.Students
            .AsNoTracking()
            .Include(x => x.Profile)
            .SingleOrDefaultAsync(x => x.IdentityUserId == identityUserId.Value, cancellationToken);
        var accessError = AccessError(student);
        if (accessError is not null)
        {
            return Result<StudentActivityCalendarResponse>.Failure(accessError);
        }

        if (!TryGetTimeZone(student!.Profile.TimeZoneId, out var timeZone))
        {
            return InvalidTimeZone();
        }

        var today = LocalDate(clock.UtcNow, timeZone!);
        var requestedYear = year ?? today.Year;
        var requestedMonth = month ?? today.Month;
        if (requestedYear is < 2000 or > 2100 || requestedMonth is < 1 or > 12)
        {
            return Result<StudentActivityCalendarResponse>.ValidationFailure(
                new Dictionary<string, IReadOnlyList<Error>>
                {
                    [year is < 2000 or > 2100 ? "year" : "month"] =
                    [Error.Validation("student.activity.period.invalid", "Student.Activity.InvalidPeriod")]
                });
        }

        return Result<StudentActivityCalendarResponse>.Success(
            await BuildCalendarAsync(student.Id, timeZone!.Id, requestedYear, requestedMonth, today, cancellationToken));
    }

    private async Task<StudentActivityCalendarResponse> BuildCalendarAsync(
        Guid studentId,
        string timeZoneId,
        int year,
        int month,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var dates = await db.DailyActivities
            .AsNoTracking()
            .Where(activity => activity.StudentId == studentId)
            .OrderBy(activity => activity.LocalDate)
            .Select(activity => activity.LocalDate)
            .ToListAsync(cancellationToken);
        var monthDays = dates
            .Where(date => date.Year == year && date.Month == month)
            .Select(date => new StudentActivityDayResponse(date))
            .ToArray();
        var (current, longest) = StudentStreakCalculator.Calculate(dates, today);
        return new(
            year,
            month,
            timeZoneId,
            today,
            current,
            longest,
            monthDays.Length,
            dates.Count,
            monthDays);
    }

    private static Error? AccessError(Student? student) => student?.Status switch
    {
        null => StudentErrors.NotFound,
        StudentStatus.Suspended => StudentErrors.Suspended,
        StudentStatus.Closed => StudentErrors.Closed,
        _ => null
    };

    private static DateOnly LocalDate(DateTimeOffset utcNow, TimeZoneInfo timeZone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, timeZone).DateTime);

    private static bool TryGetTimeZone(string value, out TimeZoneInfo? timeZone)
    {
        timeZone = null;
        if (value.Length > StudentProfile.TimeZoneIdMaxLength ||
            (!value.Contains('/', StringComparison.Ordinal) && !string.Equals(value, "UTC", StringComparison.Ordinal)))
        {
            return false;
        }

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(value);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static Result<StudentActivityCalendarResponse> InvalidTimeZone() =>
        Result<StudentActivityCalendarResponse>.ValidationFailure(
            new Dictionary<string, IReadOnlyList<Error>>
            {
                ["timeZoneId"] = [Error.Validation("student.activity.time_zone.invalid", "Student.Activity.InvalidTimeZone")]
            });

    private static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var userId)
            ? userId
            : null;
}

internal static class StudentStreakCalculator
{
    public static (int Current, int Longest) Calculate(IEnumerable<DateOnly> activityDates, DateOnly today)
    {
        var ordered = activityDates.Distinct().Order().ToArray();
        if (ordered.Length == 0)
        {
            return (0, 0);
        }

        var longest = 1;
        var running = 1;
        for (var index = 1; index < ordered.Length; index++)
        {
            running = ordered[index] == ordered[index - 1].AddDays(1) ? running + 1 : 1;
            longest = Math.Max(longest, running);
        }

        var set = ordered.ToHashSet();
        var cursor = set.Contains(today) ? today : today.AddDays(-1);
        var current = 0;
        while (set.Contains(cursor))
        {
            current++;
            cursor = cursor.AddDays(-1);
        }

        return (current, longest);
    }
}
