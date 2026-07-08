using Adeeb.Application.Abstractions.Time;

namespace Adeeb.Infrastructure.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    private static readonly TimeSpan DushanbeOffset = TimeSpan.FromHours(5);

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateTimeOffset DushanbeNow => ToDushanbeTime(UtcNow);

    public DateTimeOffset ToDushanbeTime(DateTimeOffset value) =>
        value.ToUniversalTime().ToOffset(DushanbeOffset);
}
