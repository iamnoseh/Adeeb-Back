using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.Students.Domain.Students;

public sealed class Student : Entity
{
    private Student() { }

    public Student(Guid id, Guid identityUserId, DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Student id is required.", nameof(id));
        }

        if (identityUserId == Guid.Empty)
        {
            throw new ArgumentException("Identity user id is required.", nameof(identityUserId));
        }

        Id = id;
        IdentityUserId = identityUserId;
        Status = StudentStatus.Active;
        OnboardingState = OnboardingState.NotStarted;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
        Profile = new StudentProfile(id, now);
    }

    public Guid IdentityUserId { get; private set; }
    public StudentStatus Status { get; private set; }
    public OnboardingState OnboardingState { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public StudentProfile Profile { get; private set; } = null!;

    public void UpdateProfile(
        string? displayName,
        string? avatarUrl,
        DateOnly? dateOfBirth,
        string? region,
        string? city,
        string? schoolName,
        short? grade,
        DateTimeOffset now)
    {
        EnsureOpen();
        Profile.Update(displayName, avatarUrl, dateOfBirth, region, city, schoolName, grade, now);
        if (OnboardingState == OnboardingState.NotStarted && Profile.HasMeaningfulData())
        {
            OnboardingState = OnboardingState.InProgress;
        }

        UpdatedAtUtc = now;
    }

    public bool CanAccessStudentFeatures() => Status == StudentStatus.Active;

    public void ChangeStatus(StudentStatus newStatus, DateTimeOffset now)
    {
        if (Status == StudentStatus.Closed)
        {
            throw new InvalidOperationException("Closed student personas cannot be reopened in Phase 1.");
        }

        if (newStatus == Status)
        {
            return;
        }

        Status = newStatus switch
        {
            StudentStatus.Active when Status == StudentStatus.Suspended => StudentStatus.Active,
            StudentStatus.Suspended when Status == StudentStatus.Active => StudentStatus.Suspended,
            StudentStatus.Closed when Status is StudentStatus.Active or StudentStatus.Suspended => StudentStatus.Closed,
            _ => throw new InvalidOperationException($"Invalid student status transition from {Status} to {newStatus}.")
        };
        UpdatedAtUtc = now;
    }

    private void EnsureOpen()
    {
        if (Status == StudentStatus.Closed)
        {
            throw new InvalidOperationException("Closed student personas cannot be modified.");
        }
    }
}
