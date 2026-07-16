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
    public MonthlyExamAvailability Current()
    {
        var now = clock.UtcNow;
        foreach (var day in new[] { 16, 1 })
        {
            var candidateMonth = day == 16 || now.Day >= 1 ? now : now.AddMonths(-1);
            var open = new DateTimeOffset(candidateMonth.Year, candidateMonth.Month, day, 0, 0, 0, TimeSpan.Zero);
            if (open > now) open = open.AddMonths(-1);
            var close = open.AddHours(options.Value.MonthlyExamWindowHours);
            if (now >= open && now < close)
                return new(true, open.ToString("yyyy-MM-dd"), open, close);
        }
        return new(false, null, null, null);
    }
}
