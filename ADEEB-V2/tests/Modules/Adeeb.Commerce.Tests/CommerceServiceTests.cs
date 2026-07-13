using System.Security.Claims;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Commerce.Application;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.Modules.Commerce.Domain.Entitlements;
using Adeeb.Modules.Commerce.Domain.Payments;
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

    [Fact]
    public async Task Admin_creates_tariff_and_student_sees_only_active_tariffs()
    {
        await using var db = CreateDb();
        var service = CreateService(db, new FakeStudentLookup());

        var active = await service.CreateTariffAsync(
            new TariffFormRequest { Name = "Premium 30", Price = 25, Currency = "tjs", DurationDays = 30, Status = 1 },
            "/uploads/commerce/qr/active.png",
            CancellationToken.None);
        await service.CreateTariffAsync(
            new TariffFormRequest { Name = "Draft", Price = 10, Currency = "TJS", DurationDays = 7, Status = 0 },
            "/uploads/commerce/qr/draft.png",
            CancellationToken.None);

        var studentList = await service.GetTariffsAsync(admin: false, CancellationToken.None);
        var adminList = await service.GetTariffsAsync(admin: true, CancellationToken.None);

        Assert.True(active.IsSuccess);
        Assert.Equal("TJS", active.Value!.Currency);
        Assert.Single(studentList.Value!);
        Assert.Equal(2, adminList.Value!.Count);
    }

    [Fact]
    public async Task Admin_updates_tariff_preserving_or_replacing_qr_and_archives_it()
    {
        await using var db = CreateDb();
        var service = CreateService(db, new FakeStudentLookup());
        var created = await service.CreateTariffAsync(
            new TariffFormRequest { Name = "Premium 30", Price = 25, Currency = "TJS", DurationDays = 30, Status = 1 },
            "/uploads/commerce/qr/original.png",
            CancellationToken.None);

        var preserved = await service.UpdateTariffAsync(
            created.Value!.TariffId,
            new TariffFormRequest { Name = "Premium 60", Price = 40, Currency = "TJS", DurationDays = 60, Status = 1 },
            null,
            CancellationToken.None);
        var replaced = await service.UpdateTariffAsync(
            created.Value.TariffId,
            new TariffFormRequest { Name = "Premium 90", Price = 55, Currency = "TJS", DurationDays = 90, Status = 1 },
            "/uploads/commerce/qr/new.png",
            CancellationToken.None);
        var archived = await service.ArchiveTariffAsync(created.Value.TariffId, CancellationToken.None);
        var studentList = await service.GetTariffsAsync(admin: false, CancellationToken.None);
        var adminList = await service.GetTariffsAsync(admin: true, CancellationToken.None);

        Assert.Equal("/uploads/commerce/qr/original.png", preserved.Value!.QrImageUrl);
        Assert.Equal("/uploads/commerce/qr/new.png", replaced.Value!.QrImageUrl);
        Assert.Equal("Archived", archived.Value!.Status);
        Assert.Empty(studentList.Value!);
        Assert.Single(adminList.Value!);
    }

    [Fact]
    public async Task Student_submits_receipt_and_admin_approval_grants_premium()
    {
        var studentId = Guid.NewGuid();
        await using var db = CreateDb();
        var service = CreateService(db, new FakeStudentLookup(new StudentReference(studentId, Guid.NewGuid(), "Active")));
        var tariff = await service.CreateTariffAsync(
            new TariffFormRequest { Name = "Premium 30", Price = 25, Currency = "TJS", DurationDays = 30, Status = 1 },
            "/uploads/commerce/qr/qr.png",
            CancellationToken.None);

        var submitted = await service.SubmitCurrentReceiptAsync(
            Principal(Guid.NewGuid()),
            tariff.Value!.TariffId,
            new SubmitPaymentReceiptFormRequest { IdempotencyKey = "receipt-1" },
            "/uploads/commerce/receipts/check.png",
            CancellationToken.None);
        await service.UpdateTariffAsync(
            tariff.Value.TariffId,
            new TariffFormRequest { Name = "Premium 60", Price = 40, Currency = "USD", DurationDays = 60, Status = 1 },
            null,
            CancellationToken.None);
        var reviewerId = Guid.NewGuid();
        var approved = await service.ApproveReceiptAsync(
            submitted.Value!.ReceiptId,
            Principal(reviewerId),
            new ReviewPaymentReceiptRequest("accepted"),
            CancellationToken.None);
        var summary = await service.GetCurrentEntitlementsAsync(Principal(Guid.NewGuid()), CancellationToken.None);

        Assert.True(submitted.IsSuccess);
        Assert.Equal("Pending", submitted.Value.Status);
        Assert.Equal("Premium 30", submitted.Value.TariffName);
        Assert.Equal(25, submitted.Value.TariffPrice);
        Assert.Equal("TJS", submitted.Value.Currency);
        Assert.Equal(30, submitted.Value.DurationDays);
        Assert.True(approved.IsSuccess);
        Assert.Equal("Approved", approved.Value!.Status);
        Assert.Equal(reviewerId, approved.Value.ReviewedByUserId);
        Assert.Equal("Premium", summary.Value!.AccessLevel);
        Assert.True(summary.Value.PremiumActive);
        Assert.Equal(FixedClock.Now.AddDays(30), summary.Value.PremiumUntilUtc);
        Assert.Equal(1, await db.StudentEntitlements.CountAsync());
    }

    [Fact]
    public async Task Tariff_rejects_unsupported_currency()
    {
        await using var db = CreateDb();
        var service = CreateService(db, new FakeStudentLookup());

        var result = await service.CreateTariffAsync(
            new TariffFormRequest { Name = "Premium", Price = 25, Currency = "EUR", DurationDays = 30, Status = 1 },
            "/uploads/commerce/qr/qr.png",
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("commerce.tariff.currency.unsupported", result.ValidationErrors!["currency"].Single().Code);
    }

    [Fact]
    public async Task Admin_rejection_does_not_grant_premium_and_review_is_single_use()
    {
        var studentId = Guid.NewGuid();
        await using var db = CreateDb();
        var service = CreateService(db, new FakeStudentLookup(new StudentReference(studentId, Guid.NewGuid(), "Active")));
        var tariff = await service.CreateTariffAsync(
            new TariffFormRequest { Name = "Premium 30", Price = 25, Currency = "TJS", DurationDays = 30, Status = 1 },
            "/uploads/commerce/qr/qr.png",
            CancellationToken.None);
        var submitted = await service.SubmitCurrentReceiptAsync(
            Principal(Guid.NewGuid()),
            tariff.Value!.TariffId,
            new SubmitPaymentReceiptFormRequest { IdempotencyKey = "receipt-1" },
            "/uploads/commerce/receipts/check.png",
            CancellationToken.None);

        var reviewerId = Guid.NewGuid();
        var rejected = await service.RejectReceiptAsync(
            submitted.Value!.ReceiptId,
            Principal(reviewerId),
            new ReviewPaymentReceiptRequest("not paid"),
            CancellationToken.None);
        var secondReview = await service.ApproveReceiptAsync(
            submitted.Value.ReceiptId,
            Principal(reviewerId),
            new ReviewPaymentReceiptRequest("late approve"),
            CancellationToken.None);
        var summary = await service.GetCurrentEntitlementsAsync(Principal(Guid.NewGuid()), CancellationToken.None);

        Assert.True(rejected.IsSuccess);
        Assert.Equal("Rejected", rejected.Value!.Status);
        Assert.Equal(reviewerId, rejected.Value.ReviewedByUserId);
        Assert.True(secondReview.IsFailure);
        Assert.Equal(CommerceErrors.ReceiptAlreadyReviewed.Code, secondReview.Error!.Code);
        Assert.Equal("Free", summary.Value!.AccessLevel);
    }

    [Fact]
    public async Task Student_receipt_history_is_owned_and_filterable()
    {
        var firstStudentId = Guid.NewGuid();
        var secondStudentId = Guid.NewGuid();
        var firstIdentityId = Guid.NewGuid();
        var secondIdentityId = Guid.NewGuid();
        await using var db = CreateDb();
        var firstPrincipal = Principal(firstIdentityId);
        var secondPrincipal = Principal(secondIdentityId);
        var service = CreateService(db, new FakeStudentLookup(
            new StudentReference(firstStudentId, firstIdentityId, "Active"),
            new StudentReference(secondStudentId, secondIdentityId, "Active")));
        var tariff = await service.CreateTariffAsync(
            new TariffFormRequest { Name = "Premium 30", Price = 25, Currency = "TJS", DurationDays = 30, Status = 1 },
            "/uploads/commerce/qr/qr.png",
            CancellationToken.None);

        await service.SubmitCurrentReceiptAsync(
            firstPrincipal,
            tariff.Value!.TariffId,
            new SubmitPaymentReceiptFormRequest { IdempotencyKey = "receipt-1" },
            "/uploads/commerce/receipts/first.png",
            CancellationToken.None);
        await service.SubmitCurrentReceiptAsync(
            secondPrincipal,
            tariff.Value.TariffId,
            new SubmitPaymentReceiptFormRequest { IdempotencyKey = "receipt-2" },
            "/uploads/commerce/receipts/second.png",
            CancellationToken.None);

        var firstHistory = await service.GetCurrentPaymentReceiptsAsync(firstPrincipal, null, CancellationToken.None);
        var firstPending = await service.GetCurrentPaymentReceiptsAsync(firstPrincipal, 1, CancellationToken.None);
        var firstApproved = await service.GetCurrentPaymentReceiptsAsync(firstPrincipal, 2, CancellationToken.None);

        Assert.Single(firstHistory.Value!);
        Assert.Equal(firstStudentId, firstHistory.Value![0].StudentId);
        Assert.Single(firstPending.Value!);
        Assert.Empty(firstApproved.Value!);
    }

    [Fact]
    public async Task Receipt_upload_is_idempotent_scoped_to_student_and_rejects_payload_mismatch()
    {
        var firstStudentId = Guid.NewGuid();
        var secondStudentId = Guid.NewGuid();
        var firstIdentityId = Guid.NewGuid();
        var secondIdentityId = Guid.NewGuid();
        await using var db = CreateDb();
        var firstPrincipal = Principal(firstIdentityId);
        var secondPrincipal = Principal(secondIdentityId);
        var service = CreateService(db, new FakeStudentLookup(
            new StudentReference(firstStudentId, firstIdentityId, "Active"),
            new StudentReference(secondStudentId, secondIdentityId, "Active")));
        var firstTariff = await service.CreateTariffAsync(
            new TariffFormRequest { Name = "Premium 30", Price = 25, Currency = "TJS", DurationDays = 30, Status = 1 },
            "/uploads/commerce/qr/first.png",
            CancellationToken.None);
        var secondTariff = await service.CreateTariffAsync(
            new TariffFormRequest { Name = "Premium 60", Price = 40, Currency = "TJS", DurationDays = 60, Status = 1 },
            "/uploads/commerce/qr/second.png",
            CancellationToken.None);

        var first = await service.SubmitCurrentReceiptAsync(
            firstPrincipal,
            firstTariff.Value!.TariffId,
            new SubmitPaymentReceiptFormRequest { IdempotencyKey = "receipt-1" },
            "/uploads/commerce/receipts/first.png",
            CancellationToken.None);
        var repeat = await service.SubmitCurrentReceiptAsync(
            firstPrincipal,
            firstTariff.Value.TariffId,
            new SubmitPaymentReceiptFormRequest { IdempotencyKey = "receipt-1" },
            "/uploads/commerce/receipts/retry.png",
            CancellationToken.None);
        var otherTariff = await service.SubmitCurrentReceiptAsync(
            firstPrincipal,
            secondTariff.Value!.TariffId,
            new SubmitPaymentReceiptFormRequest { IdempotencyKey = "receipt-1" },
            "/uploads/commerce/receipts/other-tariff.png",
            CancellationToken.None);
        var otherStudent = await service.SubmitCurrentReceiptAsync(
            secondPrincipal,
            firstTariff.Value.TariffId,
            new SubmitPaymentReceiptFormRequest { IdempotencyKey = "receipt-1" },
            "/uploads/commerce/receipts/other-student.png",
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(repeat.IsSuccess);
        Assert.Equal(first.Value!.ReceiptId, repeat.Value!.ReceiptId);
        Assert.True(otherTariff.IsFailure);
        Assert.Equal(CommerceErrors.IdempotencyPayloadMismatch.Code, otherTariff.Error!.Code);
        Assert.True(otherStudent.IsSuccess);
        Assert.Equal(2, await db.PaymentReceipts.CountAsync());
    }

    [Fact]
    public async Task Receipt_cursor_pagination_is_stable_when_rows_share_a_timestamp()
    {
        var studentId = Guid.NewGuid();
        var identityId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        await using var db = CreateDb();
        for (var index = 0; index < 5; index++)
        {
            db.PaymentReceipts.Add(new PaymentReceipt(
                Guid.NewGuid(),
                studentId,
                tariffId,
                "Premium 30",
                25,
                "TJS",
                30,
                $"receipts/{index}.webp",
                $"receipt-{index}",
                FixedClock.Now,
                $"fingerprint-{index}"));
        }

        await db.SaveChangesAsync();
        var service = CreateService(db, new FakeStudentLookup(new StudentReference(studentId, identityId, "Active")));

        var first = await service.GetCurrentPaymentReceiptsPageAsync(
            Principal(identityId),
            new StudentPaymentReceiptQuery { Limit = 2 },
            CancellationToken.None);
        var second = await service.GetCurrentPaymentReceiptsPageAsync(
            Principal(identityId),
            new StudentPaymentReceiptQuery { Limit = 2, Cursor = first.Value!.NextCursor },
            CancellationToken.None);
        var third = await service.GetCurrentPaymentReceiptsPageAsync(
            Principal(identityId),
            new StudentPaymentReceiptQuery { Limit = 2, Cursor = second.Value!.NextCursor },
            CancellationToken.None);

        var ids = first.Value.Items.Concat(second.Value!.Items).Concat(third.Value!.Items).Select(x => x.ReceiptId).ToList();
        Assert.Equal(5, ids.Count);
        Assert.Equal(5, ids.Distinct().Count());
        Assert.True(first.Value.HasMore);
        Assert.True(second.Value.HasMore);
        Assert.False(third.Value.HasMore);
        Assert.Null(third.Value.NextCursor);
    }

    [Theory]
    [InlineData(0, null, null, "pagination.limit.invalid")]
    [InlineData(101, null, null, "pagination.limit.invalid")]
    [InlineData(30, "not-base64", null, "pagination.cursor.invalid")]
    [InlineData(30, null, "unknown", "commerce.receipt.status.invalid")]
    public async Task Receipt_queries_reject_invalid_pagination_and_status(
        int limit,
        string? cursor,
        string? status,
        string expectedCode)
    {
        await using var db = CreateDb();
        var service = CreateService(db, new FakeStudentLookup());

        var result = await service.GetPaymentReceiptsPageAsync(
            new AdminPaymentReceiptQuery { Limit = limit, Cursor = cursor, Status = status },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error!.Code);
    }

    [Fact]
    public async Task Admin_receipt_query_rejects_inverted_date_ranges()
    {
        await using var db = CreateDb();
        var service = CreateService(db, new FakeStudentLookup());

        var result = await service.GetPaymentReceiptsPageAsync(
            new AdminPaymentReceiptQuery
            {
                CreatedFrom = FixedClock.Now,
                CreatedTo = FixedClock.Now.AddDays(-1)
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("date_range.invalid", result.Error!.Code);
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
            Task.FromResult(_students.Count == 1
                ? _students[0]
                : _students.SingleOrDefault(x => x.IdentityUserId == identityUserId));

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
