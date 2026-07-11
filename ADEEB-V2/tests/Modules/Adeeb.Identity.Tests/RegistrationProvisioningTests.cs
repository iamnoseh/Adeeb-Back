using Adeeb.Application.Abstractions.Students;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Identity.Application;
using Adeeb.Modules.Identity.Contracts;
using Adeeb.Modules.Identity.Domain.Sessions;
using Adeeb.Modules.Identity.Domain.Users;
using Adeeb.Modules.Identity.Infrastructure.Authentication;
using Adeeb.Modules.Identity.Infrastructure.Configuration;
using Adeeb.Modules.Identity.Infrastructure.Passwords;
using Adeeb.Modules.Identity.Infrastructure.Persistence;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Adeeb.Identity.Tests;

public sealed class RegistrationProvisioningTests
{
    [Fact]
    public async Task Missing_student_provisioner_returns_failure_not_empty_success()
    {
        var provisioner = new MissingStudentRegistrationProvisioner();

        var result = await provisioner.ProvisionForIdentityUserAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("student.provisioning_unavailable", result.Error!.Code);
    }

    [Fact]
    public async Task Registration_failure_after_identity_creation_revokes_new_session_and_returns_no_tokens()
    {
        await using var db = CreateDb();
        var service = CreateService(db, new FailingStudentProvisioner());

        var result = await service.RegisterAsync(
            new RegisterRequest(
                "student-failure@adeeb.tj",
                null,
                "Strong123",
                "Test",
                "User",
                "tg-TJ",
                new DeviceRequest("device-1", "Device", "web", null)),
            new ClientContext("127.0.0.1", "tests", null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
        var user = await db.Users.SingleAsync();
        var session = await db.AuthSessions.SingleAsync();
        Assert.Equal(user.Id, session.UserId);
        Assert.NotNull(session.RevokedAtUtc);
        Assert.Equal("student_provisioning_failed", session.RevokeReason);
        Assert.False(session.IsActive(DateTimeOffset.UtcNow.AddMinutes(1)));
    }

    private static IdentityDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IdentityDbContext(options);
    }

    private static IdentityService CreateService(IdentityDbContext db, IStudentRegistrationProvisioner provisioner) =>
        new(
            db,
            new PasswordHasher<User>(),
            new PasswordPolicy(Options.Create(new PasswordPolicyOptions())),
            new RefreshTokenGenerator(Options.Create(new RefreshTokenOptions())),
            new FakeAccessTokenGenerator(),
            provisioner,
            new FixedClock(),
            Options.Create(new RefreshTokenOptions()),
            NullLogger<IdentityService>.Instance);

    private sealed class FailingStudentProvisioner : IStudentRegistrationProvisioner
    {
        private static readonly Error Error = Error.Conflict("student.provisioning_unavailable", "Student.ProvisioningUnavailable");

        public Task<Result<StudentProvisioningReference>> ProvisionForIdentityUserAsync(Guid identityUserId, CancellationToken cancellationToken) =>
            Task.FromResult(Result<StudentProvisioningReference>.Failure(Error));
    }

    private sealed class FakeAccessTokenGenerator : IAccessTokenGenerator
    {
        public AccessTokenResult Generate(User user, AuthSession session, DateTimeOffset now) =>
            new("should-not-be-issued", now.AddMinutes(10));
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 7, 11, 8, 0, 0, TimeSpan.Zero);
        public DateTimeOffset DushanbeNow => UtcNow.ToOffset(TimeSpan.FromHours(5));
        public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(5));
    }
}
