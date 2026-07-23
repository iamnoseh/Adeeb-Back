using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Students.Application;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Students.Domain.Students;
using Adeeb.Modules.Students.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adeeb.Students.Tests;

public sealed class StudentsServiceTests
{
    [Fact]
    public async Task Provisioning_same_identity_user_returns_same_student()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var identityUserId = Guid.NewGuid();

        var first = await service.ProvisionForIdentityUserAsync(identityUserId, CancellationToken.None);
        var second = await service.ProvisionForIdentityUserAsync(identityUserId, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.StudentId, second.Value!.StudentId);
        Assert.Equal(1, await db.Students.CountAsync());
    }

    [Fact]
    public async Task Lookup_by_identity_user_id_returns_minimal_reference()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var identityUserId = Guid.NewGuid();
        await service.ProvisionForIdentityUserAsync(identityUserId, CancellationToken.None);

        var reference = await service.FindByIdentityUserIdAsync(identityUserId, CancellationToken.None);

        Assert.NotNull(reference);
        Assert.Equal(identityUserId, reference!.IdentityUserId);
        Assert.Equal(StudentStatus.Active.ToString(), reference.Status);
    }

    [Fact]
    public async Task Profile_update_persists_trimmed_values()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var identityUserId = Guid.NewGuid();
        await service.ProvisionForIdentityUserAsync(identityUserId, CancellationToken.None);
        var principal = TestPrincipal.ForUser(identityUserId);

        var result = await service.UpdateCurrentProfileAsync(
            principal,
            new UpdateStudentProfileRequest(" Learner ", null, null, " Dushanbe ", null, null, 7, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Learner", result.Value!.Profile.DisplayName);
        Assert.Equal("Dushanbe", result.Value.Profile.Region);
        Assert.Equal("InProgress", result.Value.OnboardingState);
    }

    [Fact]
    public async Task Invalid_profile_date_of_birth_fails_validation()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.UpdateCurrentProfileAsync(
            TestPrincipal.ForUser(Guid.NewGuid()),
            new UpdateStudentProfileRequest(null, null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("dateOfBirth", result.ValidationErrors!.Keys);
    }

    [Fact]
    public async Task Suspended_and_closed_students_cannot_update_profile()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var identityUserId = Guid.NewGuid();
        var provisioned = await service.ProvisionForIdentityUserAsync(identityUserId, CancellationToken.None);
        await service.ChangeStatusAsync(provisioned.Value!.StudentId, TestPrincipal.ForUser(Guid.NewGuid()), new ChangeStudentStatusRequest((int)StudentStatus.Suspended, null), CancellationToken.None);

        var suspended = await service.UpdateCurrentProfileAsync(TestPrincipal.ForUser(identityUserId), new UpdateStudentProfileRequest("Name", null, null, null, null, null, null, null), CancellationToken.None);
        Assert.Equal(StudentErrors.Suspended.Code, suspended.Error!.Code);

        await service.ChangeStatusAsync(provisioned.Value.StudentId, TestPrincipal.ForUser(Guid.NewGuid()), new ChangeStudentStatusRequest((int)StudentStatus.Closed, null), CancellationToken.None);
        var closed = await service.UpdateCurrentProfileAsync(TestPrincipal.ForUser(identityUserId), new UpdateStudentProfileRequest("Name", null, null, null, null, null, null, null), CancellationToken.None);
        Assert.Equal(StudentErrors.Closed.Code, closed.Error!.Code);
    }

    private static StudentsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<StudentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StudentsDbContext(options);
    }

    private static StudentsService CreateService(StudentsDbContext db) =>
        new(db, new FixedClock(), NullLogger<StudentsService>.Instance);

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 7, 11, 8, 0, 0, TimeSpan.Zero);
        public DateTimeOffset DushanbeNow => UtcNow.ToOffset(TimeSpan.FromHours(5));
        public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(5));
    }
}
