namespace Adeeb.Application.Abstractions.Time;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
    DateTimeOffset DushanbeNow { get; }
    DateTimeOffset ToDushanbeTime(DateTimeOffset value);
}
