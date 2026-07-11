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
    DateTimeOffset UpdatedAtUtc);

public sealed record UpdateStudentProfileRequest(
    string? DisplayName,
    string? AvatarUrl,
    DateOnly? DateOfBirth,
    string? Region,
    string? City,
    string? SchoolName,
    short? Grade);

public sealed record ChangeStudentStatusRequest(int Status, string? Reason);

public sealed record StudentReference(Guid StudentId, Guid IdentityUserId, string Status);

public interface IStudentLookup
{
    Task<StudentReference?> FindByIdentityUserIdAsync(Guid identityUserId, CancellationToken cancellationToken);
}
