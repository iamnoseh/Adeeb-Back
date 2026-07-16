using Adeeb.Application.Abstractions.Time;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.QuestionBank.Application;

public sealed record MonthlyExamAvailability(bool IsOpen, string? WindowKey, DateTimeOffset? OpensAtUtc, DateTimeOffset? ClosesAtUtc);

public interface IMonthlyExamAvailabilityService
{
    MonthlyExamAvailability Current();
}

internal sealed class MonthlyExamAvailabilityService(IDateTimeProvider clock, IOptions<StudentTestingOptions> options)
    : IMonthlyExamAvailabilityService
{
    private static readonly TimeSpan DushanbeOffset = TimeSpan.FromHours(5);

    public MonthlyExamAvailability Current()
    {
        var now = clock.DushanbeNow;
        foreach (var day in new[] { 16, 1 })
        {
            var open = new DateTimeOffset(now.Year, now.Month, day, 0, 0, 0, DushanbeOffset);
            if (open > now) open = open.AddMonths(-1);
            var close = open.AddHours(options.Value.MonthlyExamWindowHours);
            if (now >= open && now < close)
                return new(true, open.ToString("yyyy-MM-dd"), open.ToUniversalTime(), close.ToUniversalTime());
        }
        return new(false, null, null, null);
    }
}
