using System.Security.Claims;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Commerce.Application;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.Modules.Commerce.Domain.Entitlements;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Adeeb.Modules.Students.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Commerce.Tests;

public sealed class CommerceServiceTests
{
    [Fact]
    public async Task Active_student_without_entitlement_gets_free_summary()
    {
        var studentId = Guid.NewGuid();
        await using var db = CreateDb();
        var service = CreateService(db, new FakeStudentLookup(new StudentReference(studentId, Guid.NewGuid(), "Active")));

        var result = await service.GetCurrentEntitlementsAsync(Principal(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(studentId, result.Value!.StudentId);
        Assert.Equal("Free", result.Value.AccessLevel);
        Assert.False(result.Value.PremiumActive);
        Assert.Null(result.Value.PremiumUntilUtc);
        Assert.Equal("default", result.Value.Source);
    }

    [Fact]
    public async Task Active_premium_entitlement_produces_premium_summary()
    {
        var studentId = Guid.NewGuid();
        var now = FixedClock.Now;
        await using var db = CreateDb();
        db.StudentEntitlements.Add(new StudentEntitlement(
            Guid.NewGuid(),
            studentId,
            CommerceEntitlementKind.Premium,
            CommerceEntitlementSource.Trial,
            now.AddDays(-1),
            now.AddDays(14),
            "trial-1",
            now.AddDays(-1)));
        await db.SaveChangesAsync();
        var service = CreateService(db, new FakeStudentLookup(new StudentReference(studentId, Guid.NewGuid(), "Active")));

        var result = await service.GetCurrentEntitlementsAsync(Principal(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Premium", result.Value!.AccessLevel);
        Assert.True(result.Value.PremiumActive);
        Assert.Equal(now.AddDays(14), result.Value.PremiumUntilUtc);
        Assert.Equal("Trial", result.Value.Source);
    }

    [Fact]
    public async Task Expired_and_revoked_entitlements_do_not_grant_premium()
    {
        var studentId = Guid.NewGuid();
        var now = FixedClock.Now;
        await using var db = CreateDb();
        var revoked = new StudentEntitlement(
            Guid.NewGuid(),
            studentId,
            CommerceEntitlementKind.Premium,
            CommerceEntitlementSource.ManualGrant,
            now.AddDays(-1),
            now.AddDays(10),
            "grant-1",
            now.AddDays(-1));
        revoked.Revoke(now, "test");
        db.StudentEntitlements.AddRange(
            new StudentEntitlement(
                Guid.NewGuid(),
                studentId,
                CommerceEntitlementKind.Premium,
                CommerceEntitlementSource.Payment,
                now.AddDays(-30),
                now.AddDays(-1),
                "payment-1",
                now.AddDays(-30)),
            revoked);
        await db.SaveChangesAsync();
        var service = CreateService(db, new FakeStudentLookup(new StudentReference(studentId, Guid.NewGuid(), "Active")));

        var result = await service.GetCurrentEntitlementsAsync(Principal(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Free", result.Value!.AccessLevel);
        Assert.False(result.Value.PremiumActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Suspended")]
    [InlineData("Closed")]
    public async Task Missing_or_inactive_student_cannot_read_commerce_entitlements(string? status)
    {
        await using var db = CreateDb();
        var lookup = status is null
            ? new FakeStudentLookup()
            : new FakeStudentLookup(new StudentReference(Guid.NewGuid(), Guid.NewGuid(), status));
        var service = CreateService(db, lookup);

        var result = await service.GetCurrentEntitlementsAsync(Principal(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CommerceErrors.StudentRequired.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Admin_grant_premium_is_idempotent_by_key()
    {
        var studentId = Guid.NewGuid();
        await using var db = CreateDb();
        var service = CreateService(db, new FakeStudentLookup(new StudentReference(studentId, Guid.NewGuid(), "Active")));
        var request = new GrantPremiumEntitlementRequest(null, FixedClock.Now.AddDays(30), "manual-grant-1");

        var first = await service.GrantPremiumAsync(studentId, request, CancellationToken.None);
        var second = await service.GrantPremiumAsync(studentId, request, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.EntitlementId, second.Value!.EntitlementId);
        Assert.Equal(1, await db.StudentEntitlements.CountAsync());
        Assert.Equal("Premium", (await service.GetCurrentEntitlementsAsync(Principal(Guid.NewGuid()), CancellationToken.None)).Value!.AccessLevel);
    }

    [Fact]
    public async Task Admin_revoke_removes_premium_from_current_summary()
    {
        var studentId = Guid.NewGuid();
        await using var db = CreateDb();
        var service = CreateService(db, new FakeStudentLookup(new StudentReference(studentId, Guid.NewGuid(), "Active")));
        var granted = await service.GrantPremiumAsync(
            studentId,
            new GrantPremiumEntitlementRequest(null, FixedClock.Now.AddDays(30), "manual-grant-1"),
            CancellationToken.None);

        var revoked = await service.RevokeEntitlementAsync(
            granted.Value!.EntitlementId,
            new RevokeEntitlementRequest("admin correction"),
            CancellationToken.None);
        var summary = await service.GetCurrentEntitlementsAsync(Principal(Guid.NewGuid()), CancellationToken.None);

        Assert.True(revoked.IsSuccess);
        Assert.Equal("Revoked", revoked.Value!.Status);
        Assert.Equal("admin correction", revoked.Value.RevokeReason);
        Assert.Equal("Free", summary.Value!.AccessLevel);
    }

    [Fact]
    public async Task Admin_grant_rejects_missing_student_and_invalid_expiration()
    {
        await using var db = CreateDb();
        var service = CreateService(db, new FakeStudentLookup());

        var invalid = await service.GrantPremiumAsync(
            Guid.NewGuid(),
            new GrantPremiumEntitlementRequest(FixedClock.Now, FixedClock.Now, "manual-grant-1"),
            CancellationToken.None);
        var missing = await service.GrantPremiumAsync(
            Guid.NewGuid(),
            new GrantPremiumEntitlementRequest(null, null, "manual-grant-2"),
            CancellationToken.None);

        Assert.True(invalid.IsFailure);
        Assert.NotNull(invalid.ValidationErrors);
        Assert.True(missing.IsFailure);
        Assert.Equal(CommerceErrors.StudentNotFound.Code, missing.Error!.Code);
    }

    [Fact]
    public async Task Admin_grant_rejects_idempotency_key_reuse_for_different_student()
    {
        var firstStudentId = Guid.NewGuid();
        var secondStudentId = Guid.NewGuid();
        await using var db = CreateDb();
        var service = CreateService(db, new FakeStudentLookup(
            new StudentReference(firstStudentId, Guid.NewGuid(), "Active"),
            new StudentReference(secondStudentId, Guid.NewGuid(), "Active")));

        var first = await service.GrantPremiumAsync(
            firstStudentId,
            new GrantPremiumEntitlementRequest(null, null, "manual-grant-1"),
            CancellationToken.None);
        var second = await service.GrantPremiumAsync(
            secondStudentId,
            new GrantPremiumEntitlementRequest(null, null, "manual-grant-1"),
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal(CommerceErrors.IdempotencyKeyInUse.Code, second.Error!.Code);
    }

    private static CommerceService CreateService(CommerceDbContext db, IStudentLookup students) =>
        new(db, students, new FixedClock());

    private static CommerceDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CommerceDbContext(options);
    }

    private static ClaimsPrincipal Principal(Guid userId) =>
        new(new ClaimsIdentity([new Claim("sub", userId.ToString())], "Test"));

    private sealed class FakeStudentLookup : IStudentLookup
    {
        private readonly IReadOnlyList<StudentReference> _students;

        public FakeStudentLookup(params StudentReference[] students)
        {
            _students = students;
        }

        public Task<StudentReference?> FindByIdentityUserIdAsync(Guid identityUserId, CancellationToken cancellationToken) =>
            Task.FromResult(_students.FirstOrDefault());

        public Task<StudentReference?> FindByStudentIdAsync(Guid studentId, CancellationToken cancellationToken) =>
            Task.FromResult(_students.SingleOrDefault(x => x.StudentId == studentId));
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-11T08:00:00Z");
        public DateTimeOffset UtcNow => Now;
        public DateTimeOffset DushanbeNow => ToDushanbeTime(Now);
        public DateTimeOffset ToDushanbeTime(DateTimeOffset utc) => utc.ToOffset(TimeSpan.FromHours(5));
    }
}
