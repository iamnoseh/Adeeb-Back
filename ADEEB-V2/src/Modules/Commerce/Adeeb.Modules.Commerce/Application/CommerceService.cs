using System.Security.Claims;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.Modules.Commerce.Domain.Entitlements;
using Adeeb.Modules.Commerce.Domain.Payments;
using Adeeb.Modules.Commerce.Domain.Tariffs;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Adeeb.Modules.Students.Contracts;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.Commerce.Application;

public sealed class CommerceService(
    CommerceDbContext db,
    IStudentLookup students,
    IDateTimeProvider clock)
{
    public async Task<Result<TariffResponse>> CreateTariffAsync(
        TariffFormRequest request,
        string? qrImageUrl,
        CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateTariff(request, qrImageUrl);
        if (validation.IsFailure)
        {
            return Result<TariffResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var now = clock.UtcNow;
        var tariff = new CommerceTariff(
            Guid.NewGuid(),
            request.Name!,
            request.Price!.Value,
            request.Currency!,
            request.DurationDays!.Value,
            qrImageUrl!,
            now);
        tariff.Update(
            request.Name!,
            request.Price.Value,
            request.Currency!,
            request.DurationDays.Value,
            qrImageUrl!,
            (CommerceTariffStatus)(request.Status ?? (int)CommerceTariffStatus.Active),
            now);
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync(cancellationToken);
        return Result<TariffResponse>.Success(ToResponse(tariff));
    }

    public async Task<Result<IReadOnlyList<TariffResponse>>> GetTariffsAsync(bool admin, CancellationToken cancellationToken)
    {
        var query = db.Tariffs.AsNoTracking();
        if (!admin)
        {
            query = query.Where(x => x.Status == CommerceTariffStatus.Active);
        }

        var tariffs = await query.OrderBy(x => x.Price).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<TariffResponse>>.Success(tariffs.Select(ToResponse).ToList());
    }

    public async Task<Result<PaymentReceiptResponse>> SubmitCurrentReceiptAsync(
        ClaimsPrincipal principal,
        Guid tariffId,
        string? receiptImageUrl,
        CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateReceiptImage(receiptImageUrl);
        if (validation.IsFailure)
        {
            return Result<PaymentReceiptResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var student = await GetActiveCurrentStudentAsync(principal, cancellationToken);
        if (student is null)
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.StudentRequired);
        }

        var tariff = await db.Tariffs.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == tariffId && x.Status == CommerceTariffStatus.Active, cancellationToken);
        if (tariff is null)
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.TariffNotFound);
        }

        var now = clock.UtcNow;
        var receipt = new PaymentReceipt(Guid.NewGuid(), student.StudentId, tariffId, receiptImageUrl!, now);
        db.PaymentReceipts.Add(receipt);
        await db.SaveChangesAsync(cancellationToken);
        return Result<PaymentReceiptResponse>.Success(ToResponse(receipt, tariff));
    }

    public async Task<Result<IReadOnlyList<PaymentReceiptResponse>>> GetPaymentReceiptsAsync(
        int? status,
        CancellationToken cancellationToken)
    {
        var query = from receipt in db.PaymentReceipts.AsNoTracking()
                    join tariff in db.Tariffs.AsNoTracking() on receipt.TariffId equals tariff.Id
                    select new { receipt, tariff };

        if (status is not null && Enum.IsDefined(typeof(PaymentReceiptStatus), status.Value))
        {
            var parsed = (PaymentReceiptStatus)status.Value;
            query = query.Where(x => x.receipt.Status == parsed);
        }

        var rows = await query
            .OrderByDescending(x => x.receipt.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<PaymentReceiptResponse>>.Success(rows.Select(x => ToResponse(x.receipt, x.tariff)).ToList());
    }

    public async Task<Result<PaymentReceiptResponse>> ApproveReceiptAsync(
        Guid receiptId,
        ReviewPaymentReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateReview(request);
        if (validation.IsFailure)
        {
            return Result<PaymentReceiptResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var receipt = await db.PaymentReceipts.SingleOrDefaultAsync(x => x.Id == receiptId, cancellationToken);
        if (receipt is null)
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.ReceiptNotFound);
        }

        var tariff = await db.Tariffs.AsNoTracking().SingleAsync(x => x.Id == receipt.TariffId, cancellationToken);
        try
        {
            var now = clock.UtcNow;
            receipt.Approve(now, request.Note);
            await EnsurePaymentEntitlementAsync(receipt.StudentId, tariff, receipt.Id, now, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.ReceiptAlreadyReviewed);
        }

        return Result<PaymentReceiptResponse>.Success(ToResponse(receipt, tariff));
    }

    public async Task<Result<PaymentReceiptResponse>> RejectReceiptAsync(
        Guid receiptId,
        ReviewPaymentReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateReview(request);
        if (validation.IsFailure)
        {
            return Result<PaymentReceiptResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var receipt = await db.PaymentReceipts.SingleOrDefaultAsync(x => x.Id == receiptId, cancellationToken);
        if (receipt is null)
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.ReceiptNotFound);
        }

        var tariff = await db.Tariffs.AsNoTracking().SingleAsync(x => x.Id == receipt.TariffId, cancellationToken);
        try
        {
            receipt.Reject(clock.UtcNow, request.Note);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.ReceiptAlreadyReviewed);
        }

        return Result<PaymentReceiptResponse>.Success(ToResponse(receipt, tariff));
    }

    public async Task<Result<StudentEntitlementResponse>> GrantPremiumAsync(
        Guid studentId,
        GrantPremiumEntitlementRequest request,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var validation = Validation.ValidateGrantPremium(request, now);
        if (validation.IsFailure)
        {
            return Result<StudentEntitlementResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var student = await students.FindByStudentIdAsync(studentId, cancellationToken);
        if (student is null || !string.Equals(student.Status, "Active", StringComparison.Ordinal))
        {
            return Result<StudentEntitlementResponse>.Failure(CommerceErrors.StudentNotFound);
        }

        var idempotencyKey = request.IdempotencyKey.Trim();
        var existing = await db.StudentEntitlements.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.StudentId != studentId)
            {
                return Result<StudentEntitlementResponse>.Failure(CommerceErrors.IdempotencyKeyInUse);
            }

            return Result<StudentEntitlementResponse>.Success(ToResponse(existing));
        }

        var entitlement = new StudentEntitlement(
            Guid.NewGuid(),
            studentId,
            CommerceEntitlementKind.Premium,
            CommerceEntitlementSource.ManualGrant,
            request.StartsAtUtc ?? now,
            request.ExpiresAtUtc,
            idempotencyKey,
            now);

        db.StudentEntitlements.Add(entitlement);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresExceptionHelper.IsUniqueViolation(ex, CommerceDatabaseConstraints.StudentEntitlementIdempotencyKeyUnique))
        {
            db.ChangeTracker.Clear();
            var raced = await db.StudentEntitlements.AsNoTracking()
                .SingleAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
            return raced.StudentId == studentId
                ? Result<StudentEntitlementResponse>.Success(ToResponse(raced))
                : Result<StudentEntitlementResponse>.Failure(CommerceErrors.IdempotencyKeyInUse);
        }

        return Result<StudentEntitlementResponse>.Success(ToResponse(entitlement));
    }

    public async Task<Result<StudentEntitlementResponse>> RevokeEntitlementAsync(
        Guid entitlementId,
        RevokeEntitlementRequest request,
        CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateRevoke(request);
        if (validation.IsFailure)
        {
            return Result<StudentEntitlementResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var entitlement = await db.StudentEntitlements.SingleOrDefaultAsync(x => x.Id == entitlementId, cancellationToken);
        if (entitlement is null)
        {
            return Result<StudentEntitlementResponse>.Failure(CommerceErrors.EntitlementNotFound);
        }

        entitlement.Revoke(clock.UtcNow, request.Reason);
        await db.SaveChangesAsync(cancellationToken);
        return Result<StudentEntitlementResponse>.Success(ToResponse(entitlement));
    }

    public async Task<Result<StudentEntitlementSummaryResponse>> GetCurrentEntitlementsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var student = await GetActiveCurrentStudentAsync(principal, cancellationToken);
        if (student is null)
        {
            return Result<StudentEntitlementSummaryResponse>.Failure(CommerceErrors.StudentRequired);
        }

        var now = clock.UtcNow;
        var premium = await db.StudentEntitlements.AsNoTracking()
            .Where(x =>
                x.StudentId == student.StudentId &&
                x.Kind == CommerceEntitlementKind.Premium &&
                x.Status == CommerceEntitlementStatus.Active &&
                x.StartsAtUtc <= now &&
                (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now))
            .OrderByDescending(x => x.ExpiresAtUtc ?? DateTimeOffset.MaxValue)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (premium is null)
        {
            return Result<StudentEntitlementSummaryResponse>.Success(new(
                student.StudentId,
                "Free",
                false,
                null,
                "default"));
        }

        return Result<StudentEntitlementSummaryResponse>.Success(new(
            student.StudentId,
            "Premium",
            true,
            premium.ExpiresAtUtc,
            premium.Source.ToString()));
    }

    private static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var userId)
            ? userId
            : null;

    private async Task<StudentReference?> GetActiveCurrentStudentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var identityUserId = GetUserId(principal);
        if (identityUserId is null)
        {
            return null;
        }

        var student = await students.FindByIdentityUserIdAsync(identityUserId.Value, cancellationToken);
        return student is not null && string.Equals(student.Status, "Active", StringComparison.Ordinal)
            ? student
            : null;
    }

    private async Task EnsurePaymentEntitlementAsync(
        Guid studentId,
        CommerceTariff tariff,
        Guid receiptId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"payment-receipt:{receiptId:N}";
        if (await db.StudentEntitlements.AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken))
        {
            return;
        }

        db.StudentEntitlements.Add(new StudentEntitlement(
            Guid.NewGuid(),
            studentId,
            CommerceEntitlementKind.Premium,
            CommerceEntitlementSource.Payment,
            now,
            now.AddDays(tariff.DurationDays),
            idempotencyKey,
            now));
    }

    private static StudentEntitlementResponse ToResponse(StudentEntitlement entitlement) =>
        new(
            entitlement.Id,
            entitlement.StudentId,
            entitlement.Kind.ToString(),
            entitlement.Status.ToString(),
            entitlement.Source.ToString(),
            entitlement.StartsAtUtc,
            entitlement.ExpiresAtUtc,
            entitlement.IdempotencyKey,
            entitlement.RevokeReason,
            entitlement.RevokedAtUtc,
            entitlement.CreatedAtUtc,
            entitlement.UpdatedAtUtc);

    private static TariffResponse ToResponse(CommerceTariff tariff) =>
        new(
            tariff.Id,
            tariff.Name,
            tariff.Price,
            tariff.Currency,
            tariff.DurationDays,
            tariff.QrImageUrl,
            tariff.Status.ToString(),
            tariff.CreatedAtUtc,
            tariff.UpdatedAtUtc);

    private static PaymentReceiptResponse ToResponse(PaymentReceipt receipt, CommerceTariff tariff) =>
        new(
            receipt.Id,
            receipt.StudentId,
            receipt.TariffId,
            tariff.Name,
            tariff.Price,
            tariff.Currency,
            tariff.DurationDays,
            receipt.ReceiptImageUrl,
            receipt.Status.ToString(),
            receipt.AdminNote,
            receipt.ReviewedAtUtc,
            receipt.CreatedAtUtc,
            receipt.UpdatedAtUtc);
}
