using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Adeeb.Application.Abstractions.Storage;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Commerce.Application.Auditing;
using Adeeb.Modules.Commerce.Application.Observability;
using Adeeb.Modules.Commerce.Application.Pagination;
using Adeeb.Modules.Commerce.Application.Storage;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.Modules.Commerce.Domain.Entitlements;
using Adeeb.Modules.Commerce.Domain.Payments;
using Adeeb.Modules.Commerce.Domain.Tariffs;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Adeeb.Modules.Students.Contracts;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.Commerce.Application.PaymentReceipts;

public sealed class PaymentReceiptUseCases(
    CommerceDbContext db,
    IStudentLookup students,
    IDateTimeProvider clock,
    IReceiptImageProcessor imageProcessor,
    IPrivateFileStorage privateFiles,
    ICommerceAuditWriter audit)
{
    public async Task<Result<CursorPageResponse<PaymentReceiptListItemResponse>>> ListCurrentAsync(
        ClaimsPrincipal principal,
        StudentPaymentReceiptQuery request,
        CancellationToken cancellationToken)
    {
        var validation = ValidatePage(request.Limit, request.Cursor, request.Status, out var cursor, out var status);
        if (validation is not null) return Result<CursorPageResponse<PaymentReceiptListItemResponse>>.Failure(validation);
        var student = await GetActiveStudentAsync(principal, cancellationToken);
        if (student is null) return Result<CursorPageResponse<PaymentReceiptListItemResponse>>.Failure(CommerceErrors.StudentRequired);
        var query = db.PaymentReceipts.AsNoTracking().Where(x => x.StudentId == student.StudentId);
        if (status is not null) query = query.Where(x => x.Status == status.Value);
        query = ApplyCursor(query, cursor);
        var rows = await Project(query).OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.ReceiptId)
            .Take(request.Limit + 1).ToListAsync(cancellationToken);
        return Result<CursorPageResponse<PaymentReceiptListItemResponse>>.Success(ToPage(rows, request.Limit));
    }

    public async Task<Result<CursorPageResponse<PaymentReceiptListItemResponse>>> ListAdminAsync(
        AdminPaymentReceiptQuery request,
        CancellationToken cancellationToken)
    {
        var validation = ValidatePage(request.Limit, request.Cursor, request.Status, out var cursor, out var status);
        if (validation is not null) return Result<CursorPageResponse<PaymentReceiptListItemResponse>>.Failure(validation);
        if ((request.CreatedFrom is not null && request.CreatedTo is not null && request.CreatedFrom > request.CreatedTo) ||
            (request.ReviewedFrom is not null && request.ReviewedTo is not null && request.ReviewedFrom > request.ReviewedTo))
            return Result<CursorPageResponse<PaymentReceiptListItemResponse>>.Failure(CommerceErrors.DateRangeInvalid);
        var query = db.PaymentReceipts.AsNoTracking();
        if (status is not null) query = query.Where(x => x.Status == status.Value);
        if (request.StudentId is not null) query = query.Where(x => x.StudentId == request.StudentId);
        if (request.TariffId is not null) query = query.Where(x => x.TariffId == request.TariffId);
        if (request.ReviewedByUserId is not null) query = query.Where(x => x.ReviewedByUserId == request.ReviewedByUserId);
        if (request.CreatedFrom is not null) query = query.Where(x => x.CreatedAtUtc >= request.CreatedFrom);
        if (request.CreatedTo is not null) query = query.Where(x => x.CreatedAtUtc <= request.CreatedTo);
        if (request.ReviewedFrom is not null) query = query.Where(x => x.ReviewedAtUtc >= request.ReviewedFrom);
        if (request.ReviewedTo is not null) query = query.Where(x => x.ReviewedAtUtc <= request.ReviewedTo);
        query = ApplyCursor(query, cursor);
        var rows = await Project(query).OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.ReceiptId)
            .Take(request.Limit + 1).ToListAsync(cancellationToken);
        return Result<CursorPageResponse<PaymentReceiptListItemResponse>>.Success(ToPage(rows, request.Limit));
    }

    public Task<Result<PaymentReceiptResponse>> SubmitAsync(
        ClaimsPrincipal principal,
        Guid tariffId,
        SubmitPaymentReceiptFormRequest request,
        Stream? image,
        long imageLength,
        CancellationToken cancellationToken) => ObserveAsync("submit", async () =>
    {
        var validation = Validation.ValidateReceiptSubmission(request, image is null ? null : "pending");
        if (validation.IsFailure) return Result<PaymentReceiptResponse>.ValidationFailure(validation.ValidationErrors!);
        var student = await GetActiveStudentAsync(principal, cancellationToken);
        if (student is null) return Result<PaymentReceiptResponse>.Failure(CommerceErrors.StudentRequired);
        var processed = await imageProcessor.ProcessAsync(image!, imageLength, cancellationToken);
        if (processed.IsFailure) return Result<PaymentReceiptResponse>.Failure(processed.Error!);
        var key = request.IdempotencyKey!.Trim();
        var fingerprint = Fingerprint(student.StudentId, tariffId, processed.Value!.Sha256);
        var existing = await db.PaymentReceipts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.StudentId == student.StudentId && x.IdempotencyKey == key, cancellationToken);
        if (existing is not null) return Matches(existing, tariffId, fingerprint)
            ? Result<PaymentReceiptResponse>.Success(ToResponse(existing))
            : Result<PaymentReceiptResponse>.Failure(CommerceErrors.IdempotencyPayloadMismatch);
        var objectKey = $"commerce/payment-receipts/{student.StudentId:N}/{Guid.NewGuid():N}.webp";
        await using var content = new MemoryStream(processed.Value.Content, writable: false);
        await privateFiles.SaveAsync(content, processed.Value.ContentType, objectKey, cancellationToken);
        try
        {
            var result = await PersistSubmissionAsync(student, tariffId, request, objectKey, fingerprint, cancellationToken);
            if (result.IsFailure)
            {
                await privateFiles.DeleteAsync(objectKey, CancellationToken.None);
                return result;
            }

            var persistedKey = await db.PaymentReceipts.AsNoTracking()
                .Where(x => x.Id == result.Value!.ReceiptId)
                .Select(x => x.ReceiptImageObjectKey)
                .SingleAsync(CancellationToken.None);
            if (!string.Equals(persistedKey, objectKey, StringComparison.Ordinal))
            {
                await privateFiles.DeleteAsync(objectKey, CancellationToken.None);
            }

            return result;
        }
        catch
        {
            await privateFiles.DeleteAsync(objectKey, CancellationToken.None);
            throw;
        }
    });

    public async Task<Result<PrivateFileReadResult>> OpenImageAsync(Guid receiptId, CancellationToken cancellationToken)
    {
        var receipt = await db.PaymentReceipts.AsNoTracking().Where(x => x.Id == receiptId)
            .Select(x => new { x.ReceiptImageObjectKey, x.StudentId }).SingleOrDefaultAsync(cancellationToken);
        if (receipt is null) return Result<PrivateFileReadResult>.Failure(CommerceErrors.ReceiptNotFound);
        audit.Write(CommerceAuditActions.ReceiptImageAccessed, "PaymentReceipt", receiptId, receipt.StudentId);
        await db.SaveChangesAsync(cancellationToken);
        var file = await privateFiles.OpenReadAsync(receipt.ReceiptImageObjectKey, cancellationToken);
        return file is null
            ? Result<PrivateFileReadResult>.Failure(CommerceErrors.ReceiptImageNotFound)
            : Result<PrivateFileReadResult>.Success(file);
    }

    public Task<Result<PaymentReceiptResponse>> ApproveAsync(
        Guid receiptId,
        ClaimsPrincipal reviewer,
        ReviewPaymentReceiptRequest request,
        CancellationToken cancellationToken) => ObserveAsync("approve", () => ReviewAsync(receiptId, reviewer, request, true, cancellationToken));

    public Task<Result<PaymentReceiptResponse>> RejectAsync(
        Guid receiptId,
        ClaimsPrincipal reviewer,
        ReviewPaymentReceiptRequest request,
        CancellationToken cancellationToken) => ObserveAsync("reject", () => ReviewAsync(receiptId, reviewer, request, false, cancellationToken));

    private async Task<Result<PaymentReceiptResponse>> PersistSubmissionAsync(
        StudentReference student,
        Guid tariffId,
        SubmitPaymentReceiptFormRequest request,
        string objectKey,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var tariff = await db.Tariffs.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == tariffId && x.Status == CommerceTariffStatus.Active, cancellationToken);
        if (tariff is null) return Result<PaymentReceiptResponse>.Failure(CommerceErrors.TariffNotFound);
        var key = request.IdempotencyKey!.Trim();
        var receipt = new PaymentReceipt(
            Guid.NewGuid(), student.StudentId, tariff.Id, tariff.Name, tariff.Price, tariff.Currency, tariff.DurationDays,
            objectKey, key, clock.UtcNow, fingerprint);
        db.PaymentReceipts.Add(receipt);
        audit.Write(CommerceAuditActions.ReceiptSubmitted, "PaymentReceipt", receipt.Id, receipt.StudentId,
            newValues: new Dictionary<string, object?> { ["tariffId"] = tariff.Id, ["status"] = receipt.Status.ToString() });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (PostgresExceptionHelper.IsUniqueViolation(
            exception, CommerceDatabaseConstraints.PaymentReceiptIdempotencyScopeUnique))
        {
            db.ChangeTracker.Clear();
            var raced = await db.PaymentReceipts.AsNoTracking()
                .SingleAsync(x => x.StudentId == student.StudentId && x.IdempotencyKey == key, cancellationToken);
            return Matches(raced, tariffId, fingerprint)
                ? Result<PaymentReceiptResponse>.Success(ToResponse(raced))
                : Result<PaymentReceiptResponse>.Failure(CommerceErrors.IdempotencyPayloadMismatch);
        }

        return Result<PaymentReceiptResponse>.Success(ToResponse(receipt));
    }

    private async Task<Result<PaymentReceiptResponse>> ReviewAsync(
        Guid receiptId,
        ClaimsPrincipal reviewer,
        ReviewPaymentReceiptRequest request,
        bool approve,
        CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateReview(request);
        if (validation.IsFailure) return Result<PaymentReceiptResponse>.ValidationFailure(validation.ValidationErrors!);
        var reviewerId = UserId(reviewer);
        if (reviewerId is null) return Result<PaymentReceiptResponse>.Failure(CommerceErrors.ReviewerRequired);
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var receipt = await db.PaymentReceipts.SingleOrDefaultAsync(x => x.Id == receiptId, cancellationToken);
        if (receipt is null) return Result<PaymentReceiptResponse>.Failure(CommerceErrors.ReceiptNotFound);
        if (approve) await StudentEntitlementLock.AcquireAsync(db, receipt.StudentId, cancellationToken);
        var now = clock.UtcNow;
        var transition = approve
            ? receipt.Approve(reviewerId.Value, now, request.Note)
            : receipt.Reject(reviewerId.Value, now, request.Note);
        if (transition.IsFailure) return Result<PaymentReceiptResponse>.Failure(transition.Error!);
        if (approve) await AddPaymentEntitlementAsync(receipt, now, cancellationToken);
        audit.Write(
            approve ? CommerceAuditActions.ReceiptApproved : CommerceAuditActions.ReceiptRejected,
            "PaymentReceipt", receipt.Id, receipt.StudentId,
            newValues: new Dictionary<string, object?> { ["status"] = receipt.Status.ToString(), ["reviewedByUserId"] = reviewerId });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.ReceiptConcurrencyConflict);
        }
        catch (DbUpdateException exception) when (PostgresExceptionHelper.IsUniqueViolation(
            exception, CommerceDatabaseConstraints.StudentEntitlementSourcePaymentReceiptUnique))
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.EntitlementAlreadyCreated);
        }

        return Result<PaymentReceiptResponse>.Success(ToResponse(receipt));
    }

    private async Task AddPaymentEntitlementAsync(PaymentReceipt receipt, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await db.StudentEntitlements.AnyAsync(x => x.SourcePaymentReceiptId == receipt.Id, cancellationToken)) return;
        var latest = await db.StudentEntitlements.Where(x =>
                x.StudentId == receipt.StudentId && x.Kind == CommerceEntitlementKind.Premium &&
                x.Status == CommerceEntitlementStatus.Active && x.ExpiresAtUtc != null && x.ExpiresAtUtc > now)
            .MaxAsync(x => (DateTimeOffset?)x.ExpiresAtUtc, cancellationToken);
        var startsAt = latest ?? now;
        db.StudentEntitlements.Add(new StudentEntitlement(
            Guid.NewGuid(), receipt.StudentId, CommerceEntitlementKind.Premium, CommerceEntitlementSource.Payment,
            startsAt, startsAt.AddDays(receipt.DurationDaysSnapshot), $"payment-receipt:{receipt.Id:N}", now, receipt.Id));
    }

    private async Task<StudentReference?> GetActiveStudentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var id = UserId(principal);
        if (id is null) return null;
        var student = await students.FindByIdentityUserIdAsync(id.Value, cancellationToken);
        return student is not null && string.Equals(student.Status, "Active", StringComparison.Ordinal) ? student : null;
    }

    private static Guid? UserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) ? id : null;

    private static string Fingerprint(Guid studentId, Guid tariffId, string hash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{studentId:N}:submit-payment-receipt:{tariffId:N}:{hash}"))).ToLowerInvariant();

    private static bool Matches(PaymentReceipt receipt, Guid tariffId, string fingerprint) =>
        receipt.TariffId == tariffId && string.Equals(receipt.RequestFingerprint, fingerprint, StringComparison.Ordinal);

    private static IQueryable<PaymentReceipt> ApplyCursor(IQueryable<PaymentReceipt> query, PaymentReceiptCursor? cursor) =>
        cursor is null ? query : query.Where(x => x.CreatedAtUtc < cursor.Value.CreatedAtUtc ||
            (x.CreatedAtUtc == cursor.Value.CreatedAtUtc && x.Id.CompareTo(cursor.Value.Id) < 0));

    private static IQueryable<ReceiptRow> Project(IQueryable<PaymentReceipt> query) => query.Select(x => new ReceiptRow(
        x.Id, x.StudentId, x.TariffId, x.TariffNameSnapshot, x.PriceSnapshot, x.CurrencySnapshot,
        x.DurationDaysSnapshot, x.Status, x.ReviewedByUserId, x.ReviewedAtUtc, x.CreatedAtUtc));

    private static CursorPageResponse<PaymentReceiptListItemResponse> ToPage(List<ReceiptRow> rows, int limit)
    {
        var hasMore = rows.Count > limit;
        var items = rows.Take(limit).Select(x => new PaymentReceiptListItemResponse(
            x.ReceiptId, x.StudentId, x.TariffId, x.TariffName, x.Price, x.Currency, x.DurationDays,
            true, x.Status.ToString(), x.ReviewerId, x.ReviewedAtUtc, x.CreatedAtUtc)).ToList();
        var last = items.LastOrDefault();
        return new(items, hasMore && last is not null ? PaymentReceiptCursor.Encode(last.CreatedAtUtc, last.ReceiptId) : null, hasMore);
    }

    private static Error? ValidatePage(
        int limit, string? cursorValue, string? statusValue,
        out PaymentReceiptCursor? cursor, out PaymentReceiptStatus? status)
    {
        cursor = null;
        status = null;
        if (limit is < 1 or > 100) return CommerceErrors.PaginationLimitInvalid;
        if (!string.IsNullOrWhiteSpace(cursorValue))
        {
            if (!PaymentReceiptCursor.TryDecode(cursorValue, out var decoded)) return CommerceErrors.PaginationCursorInvalid;
            cursor = decoded;
        }
        if (!string.IsNullOrWhiteSpace(statusValue))
        {
            if (int.TryParse(statusValue, out var numeric) && Enum.IsDefined(typeof(PaymentReceiptStatus), numeric))
                status = (PaymentReceiptStatus)numeric;
            else if (Enum.TryParse<PaymentReceiptStatus>(statusValue, true, out var named) && Enum.IsDefined(named)) status = named;
            else return CommerceErrors.ReceiptStatusInvalid;
        }
        return null;
    }

    private static PaymentReceiptResponse ToResponse(PaymentReceipt receipt) => new(
        receipt.Id, receipt.StudentId, receipt.TariffId, receipt.TariffNameSnapshot, receipt.PriceSnapshot,
        receipt.CurrencySnapshot, receipt.DurationDaysSnapshot, true, receipt.Status.ToString(), receipt.AdminNote,
        receipt.ReviewedByUserId, receipt.ReviewedAtUtc, receipt.CreatedAtUtc, receipt.UpdatedAtUtc);

    private static async Task<Result<PaymentReceiptResponse>> ObserveAsync(
        string operation, Func<Task<Result<PaymentReceiptResponse>>> action)
    {
        using var activity = CommerceTelemetry.Activities.StartActivity($"commerce.receipt.{operation}");
        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = await action();
            var outcome = result.IsSuccess ? "success" : "failure";
            CommerceTelemetry.ReceiptOperations.Add(1, new("operation", operation), new("outcome", outcome));
            activity?.SetTag("commerce.outcome", outcome);
            activity?.SetTag("commerce.error.code", result.Error?.Code);
            return result;
        }
        catch (Exception exception)
        {
            CommerceTelemetry.ReceiptOperations.Add(1, new("operation", operation), new("outcome", "exception"));
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            throw;
        }
        finally
        {
            CommerceTelemetry.ReceiptOperationDuration.Record(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", operation));
        }
    }

    private sealed record ReceiptRow(
        Guid ReceiptId, Guid StudentId, Guid TariffId, string TariffName, decimal Price, string Currency,
        short DurationDays, PaymentReceiptStatus Status, Guid? ReviewerId, DateTimeOffset? ReviewedAtUtc, DateTimeOffset CreatedAtUtc);
}
