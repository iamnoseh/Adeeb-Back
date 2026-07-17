namespace Adeeb.Modules.Students.Domain.Students;

public sealed class StudentDailyActivity
{
    private StudentDailyActivity() { }

    public StudentDailyActivity(
        Guid studentId,
        DateOnly localDate,
        string timeZoneId,
        DateTimeOffset seenAtUtc)
    {
        StudentId = studentId;
        LocalDate = localDate;
        TimeZoneId = timeZoneId;
        FirstSeenAtUtc = seenAtUtc;
        LastSeenAtUtc = seenAtUtc;
    }

    public Guid StudentId { get; private set; }
    public DateOnly LocalDate { get; private set; }
    public string TimeZoneId { get; private set; } = StudentProfile.DefaultTimeZoneId;
    public DateTimeOffset FirstSeenAtUtc { get; private set; }
    public DateTimeOffset LastSeenAtUtc { get; private set; }

    public void MarkSeen(DateTimeOffset seenAtUtc)
    {
        if (seenAtUtc > LastSeenAtUtc)
        {
            LastSeenAtUtc = seenAtUtc;
        }
    }
}
