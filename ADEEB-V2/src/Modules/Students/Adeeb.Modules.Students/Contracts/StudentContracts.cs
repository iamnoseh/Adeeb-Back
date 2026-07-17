namespace Adeeb.Modules.Students.Contracts;

public sealed record StudentResponse(
    Guid StudentId,
    Guid IdentityUserId,
    string Status,
    string OnboardingState,
    StudentProfileResponse Profile,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record StudentProfileResponse(
    string? DisplayName,
    string? AvatarUrl,
    DateOnly? DateOfBirth,
    string? Region,
    string? City,
    string? SchoolName,
    short? Grade,
    string TimeZoneId,
    DateTimeOffset UpdatedAtUtc);

public sealed record StudentActivityVisitRequest(string? TimeZoneId);

public sealed record StudentActivityDayResponse(DateOnly Date);

public sealed record StudentActivityCalendarResponse(
    int Year,
    int Month,
    string TimeZoneId,
    DateOnly TodayLocalDate,
    int CurrentStreak,
    int LongestStreak,
    int ActiveDaysInMonth,
    int TotalActiveDays,
    IReadOnlyList<StudentActivityDayResponse> Days);

public sealed record UpdateStudentProfileRequest(
    string? DisplayName,
    string? AvatarUrl,
    DateOnly? DateOfBirth,
    string? Region,
    string? City,
    string? SchoolName,
    short? Grade);

public sealed record ChangeStudentStatusRequest(int Status, string? Reason);

public sealed record StudentReference(Guid StudentId, Guid IdentityUserId, string Status, string TimeZoneId = "Asia/Dushanbe");

public interface IStudentLookup
{
    Task<StudentReference?> FindByIdentityUserIdAsync(Guid identityUserId, CancellationToken cancellationToken);
    Task<StudentReference?> FindByStudentIdAsync(Guid studentId, CancellationToken cancellationToken);
}
