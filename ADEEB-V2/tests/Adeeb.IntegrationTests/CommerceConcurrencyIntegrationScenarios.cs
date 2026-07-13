using System.Security.Claims;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Commerce.Application;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.Modules.Commerce.Domain.Entitlements;
using Adeeb.Modules.Commerce.Domain.Payments;
using Adeeb.Modules.Commerce.Domain.Tariffs;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Adeeb.Modules.Students.Contracts;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.IntegrationTests;

public sealed class CommerceConcurrencyIntegrationScenarios(AdeebApiFactory factory) : IClassFixture<AdeebApiFactory>
{
    [Fact]
    public async Task ConcurrentApprovals_OnlyOneShouldSucceedAndCreateOneEntitlement()
    {
        var receiptId = await SeedPendingReceiptAsync();

        var first = ReviewAsync(receiptId, approve: true, Guid.NewGuid());
        var second = ReviewAsync(receiptId, approve: true, Guid.NewGuid());
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, x => x.IsSuccess);
        Assert.Single(results, x => x.IsFailure);
        AssertConflict(results.Single(x => x.IsFailure));

        await using var verification = CreateDb();
        var receipt = await verification.PaymentReceipts.AsNoTracking().SingleAsync(x => x.Id == receiptId);
        var entitlements = await verification.StudentEntitlements.AsNoTracking()
            .Where(x => x.SourcePaymentReceiptId == receiptId)
            .ToListAsync();
        Assert.Equal(PaymentReceiptStatus.Approved, receipt.Status);
        Assert.Single(entitlements);
    }

    [Fact]
    public async Task ConcurrentApproveAndReject_OnlyOneShouldSucceed()
    {
        var receiptId = await SeedPendingReceiptAsync();

        var approve = ReviewAsync(receiptId, approve: true, Guid.NewGuid());
        var reject = ReviewAsync(receiptId, approve: false, Guid.NewGuid());
        var results = await Task.WhenAll(approve, reject);

        Assert.Single(results, x => x.IsSuccess);
        Assert.Single(results, x => x.IsFailure);
        AssertConflict(results.Single(x => x.IsFailure));

        await using var verification = CreateDb();
        var receipt = await verification.PaymentReceipts.AsNoTracking().SingleAsync(x => x.Id == receiptId);
        var entitlementCount = await verification.StudentEntitlements.CountAsync(x => x.SourcePaymentReceiptId == receiptId);
        Assert.Contains(receipt.Status, new[] { PaymentReceiptStatus.Approved, PaymentReceiptStatus.Rejected });
        Assert.Equal(receipt.Status == PaymentReceiptStatus.Approved ? 1 : 0, entitlementCount);
    }

    [Fact]
    public async Task ReceiptSnapshotAndEntitlementDuration_SurviveTariffChange()
    {
        var receiptId = await SeedPendingReceiptAsync();
        await using (var update = CreateDb())
        {
            var receipt = await update.PaymentReceipts.SingleAsync(x => x.Id == receiptId);
            var tariff = await update.Tariffs.SingleAsync(x => x.Id == receipt.TariffId);
            tariff.Update("Premium 90", 70, "USD", 90, tariff.QrImageUrl, CommerceTariffStatus.Active, FixedClock.Now.AddMinutes(1));
            await update.SaveChangesAsync();
        }

        var approved = await ReviewAsync(receiptId, approve: true, Guid.NewGuid());

        Assert.True(approved.IsSuccess);
        Assert.Equal("Premium 30", approved.Value!.TariffName);
        Assert.Equal(25, approved.Value.TariffPrice);
        Assert.Equal("TJS", approved.Value.Currency);
        Assert.Equal(30, approved.Value.DurationDays);
        await using var verification = CreateDb();
        var entitlement = await verification.StudentEntitlements.AsNoTracking()
            .SingleAsync(x => x.SourcePaymentReceiptId == receiptId);
        Assert.Equal(FixedClock.Now.AddDays(30), entitlement.ExpiresAtUtc);
    }

    private async Task<Guid> SeedPendingReceiptAsync()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        var now = FixedClock.Now;
        var tariff = new CommerceTariff(Guid.NewGuid(), "Premium 30", 25, "TJS", 30, "private/qr.webp", now);
        var receipt = new PaymentReceipt(
            Guid.NewGuid(),
            Guid.NewGuid(),
            tariff.Id,
            tariff.Name,
            tariff.Price,
            tariff.Currency,
            tariff.DurationDays,
            "commerce/payment-receipts/test/receipt.webp",
            $"receipt-{Guid.NewGuid():N}",
            now);
        db.AddRange(tariff, receipt);
        await db.SaveChangesAsync();
        return receipt.Id;
    }

    private async Task<Result<PaymentReceiptResponse>> ReviewAsync(Guid receiptId, bool approve, Guid reviewerId)
    {
        await using var db = CreateDb();
        var service = new CommerceService(db, new EmptyStudentLookup(), new FixedClock());
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", reviewerId.ToString())], "Test"));
        return approve
            ? await service.ApproveReceiptAsync(receiptId, principal, new ReviewPaymentReceiptRequest("approved"), CancellationToken.None)
            : await service.RejectReceiptAsync(receiptId, principal, new ReviewPaymentReceiptRequest("rejected"), CancellationToken.None);
    }

    private CommerceDbContext CreateDb() => new(
        new DbContextOptionsBuilder<CommerceDbContext>()
            .UseNpgsql(factory.ConnectionString)
            .Options);

    private static void AssertConflict(Result result) =>
        Assert.Contains(result.Error!.Code, new[]
        {
            CommerceErrors.ReceiptAlreadyReviewed.Code,
            CommerceErrors.ReceiptConcurrencyConflict.Code,
            CommerceErrors.EntitlementAlreadyCreated.Code
        });

    private sealed class EmptyStudentLookup : IStudentLookup
    {
        public Task<StudentReference?> FindByIdentityUserIdAsync(Guid identityUserId, CancellationToken cancellationToken) =>
            Task.FromResult<StudentReference?>(null);

        public Task<StudentReference?> FindByStudentIdAsync(Guid studentId, CancellationToken cancellationToken) =>
            Task.FromResult<StudentReference?>(null);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-13T08:00:00Z");
        public DateTimeOffset UtcNow => Now;
        public DateTimeOffset DushanbeNow => ToDushanbeTime(Now);
        public DateTimeOffset ToDushanbeTime(DateTimeOffset utc) => utc.ToOffset(TimeSpan.FromHours(5));
    }
}
