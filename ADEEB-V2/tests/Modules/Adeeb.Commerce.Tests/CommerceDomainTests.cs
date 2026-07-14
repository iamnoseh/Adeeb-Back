using Adeeb.Modules.Commerce.Domain.Entitlements;
using Adeeb.Modules.Commerce.Domain.Payments;
using Adeeb.Modules.Commerce.Domain.Tariffs;

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

        entitlement.Revoke(now.AddDays(1), "manual");

        Assert.False(entitlement.IsActiveAt(now.AddDays(2)));
        Assert.Equal(CommerceEntitlementStatus.Revoked, entitlement.Status);
        Assert.Equal("manual", entitlement.RevokeReason);
        Assert.Equal(now.AddDays(1), entitlement.RevokedAtUtc);
    }

    [Fact]
    public void Tariff_requires_positive_price_duration_and_qr()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => new CommerceTariff(Guid.NewGuid(), "Premium", 0, "TJS", 30, "/qr.png", now));
        Assert.Throws<ArgumentException>(() => new CommerceTariff(Guid.NewGuid(), "Premium", 20, "TJS", 0, "/qr.png", now));
        Assert.Throws<ArgumentException>(() => new CommerceTariff(Guid.NewGuid(), "Premium", 20, "TJS", 30, "", now));
    }

    [Fact]
    public void Payment_receipt_can_be_reviewed_once()
    {
        var now = DateTimeOffset.Parse("2026-07-11T08:00:00Z");
        var reviewerId = Guid.NewGuid();
        var receipt = new PaymentReceipt(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Premium", 25, "TJS", 30, "/receipt.png", "receipt-1", now);

        var approved = receipt.Approve(reviewerId, now.AddMinutes(5), "paid");

        Assert.Equal(PaymentReceiptStatus.Approved, receipt.Status);
        Assert.Equal("paid", receipt.AdminNote);
        Assert.Equal(reviewerId, receipt.ReviewedByUserId);
        Assert.True(approved.IsSuccess);
        var rejected = receipt.Reject(reviewerId, now.AddMinutes(6), "no");
        Assert.True(rejected.IsFailure);
        Assert.Equal("commerce.receipt_already_reviewed", rejected.Error!.Code);
    }
}
