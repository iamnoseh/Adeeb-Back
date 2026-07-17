using Adeeb.Modules.Students.Domain.Students;

namespace Adeeb.Students.Tests;

public sealed class StudentDomainTests
{
    [Fact]
    public void Student_creation_requires_independent_ids_and_sets_initial_state()
    {
        var identityUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var student = new Student(Guid.NewGuid(), identityUserId, now);

        Assert.NotEqual(identityUserId, student.Id);
        Assert.Equal(identityUserId, student.IdentityUserId);
        Assert.Equal(StudentStatus.Active, student.Status);
        Assert.Equal(OnboardingState.NotStarted, student.OnboardingState);
        Assert.NotNull(student.Profile);
        Assert.Equal(StudentProfile.DefaultTimeZoneId, student.Profile.TimeZoneId);
    }

    [Fact]
    public void Student_rejects_empty_identity_user_id()
    {
        Assert.Throws<ArgumentException>(() => new Student(Guid.NewGuid(), Guid.Empty, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void First_meaningful_profile_update_moves_onboarding_to_in_progress()
    {
        var student = new Student(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        student.UpdateProfile(" Learner ", null, null, null, null, null, null, DateTimeOffset.UtcNow);

        Assert.Equal("Learner", student.Profile.DisplayName);
        Assert.Equal(OnboardingState.InProgress, student.OnboardingState);
    }

    [Fact]
    public void Status_transitions_allow_suspend_reactivate_and_close()
    {
        var student = new Student(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        student.ChangeStatus(StudentStatus.Suspended, DateTimeOffset.UtcNow);
        student.ChangeStatus(StudentStatus.Active, DateTimeOffset.UtcNow);
        student.ChangeStatus(StudentStatus.Closed, DateTimeOffset.UtcNow);

        Assert.Equal(StudentStatus.Closed, student.Status);
        Assert.False(student.CanAccessStudentFeatures());
    }

    [Fact]
    public void Closed_status_is_terminal()
    {
        var student = new Student(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        student.ChangeStatus(StudentStatus.Closed, DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => student.ChangeStatus(StudentStatus.Active, DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => student.UpdateProfile("Name", null, null, null, null, null, null, DateTimeOffset.UtcNow));
    }
}
