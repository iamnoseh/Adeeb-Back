using Adeeb.Modules.Commerce.Domain.Entitlements;

namespace Adeeb.Commerce.Tests;

public sealed class CommerceDomainTests
{
    [Fact]
    public void Entitlement_requires_student_and_idempotency_key()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => new StudentEntitlement(
            Guid.NewGuid(),
            Guid.Empty,
            CommerceEntitlementKind.Premium,
            CommerceEntitlementSource.ManualGrant,
            now,
            null,
            "grant-1",
            now));

        Assert.Throws<ArgumentException>(() => new StudentEntitlement(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CommerceEntitlementKind.Premium,
            CommerceEntitlementSource.ManualGrant,
            now,
            null,
            "",
            now));
    }

    [Fact]
    public void Entitlement_active_window_is_time_bounded_and_revocable()
    {
        var now = DateTimeOffset.Parse("2026-07-11T08:00:00Z");
        var entitlement = new StudentEntitlement(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CommerceEntitlementKind.Premium,
            CommerceEntitlementSource.ManualGrant,
            now,
            now.AddDays(30),
            "grant-1",
            now);

        Assert.False(entitlement.IsActiveAt(now.AddSeconds(-1)));
        Assert.True(entitlement.IsActiveAt(now));
        Assert.False(entitlement.IsActiveAt(now.AddDays(30)));

        entitlement.Revoke(now.AddDays(1));

        Assert.False(entitlement.IsActiveAt(now.AddDays(2)));
        Assert.Equal(CommerceEntitlementStatus.Revoked, entitlement.Status);
    }
}
