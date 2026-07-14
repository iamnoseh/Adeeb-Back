using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Application.Abstractions.Storage;
using Adeeb.Modules.Commerce.Application.Storage;
using Adeeb.Modules.Commerce.Application.Auditing;
using Adeeb.Modules.Commerce.Application.Pagination;
using Adeeb.Modules.Commerce.Application.Caching;
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
    IDateTimeProvider clock,
    IReceiptImageProcessor imageProcessor,
    IPrivateFileStorage privateFiles,
    ICommerceAuditWriter audit,
    IActiveTariffCache tariffCache)
{
    public CommerceService(CommerceDbContext db, IStudentLookup students, IDateTimeProvider clock)
        : this(db, students, clock, new MissingReceiptImageProcessor(), new MissingPrivateFileStorage(), new NullCommerceAuditWriter(), new NullActiveTariffCache())
    {
    }

    public CommerceService(
        CommerceDbContext db,
        IStudentLookup students,
        IDateTimeProvider clock,
        IActiveTariffCache tariffCache)
        : this(db, students, clock, new MissingReceiptImageProcessor(), new MissingPrivateFileStorage(), new NullCommerceAuditWriter(), tariffCache)
    {
    }

    public CommerceService(
        CommerceDbContext db,
        IStudentLookup students,
        IDateTimeProvider clock,
        ICommerceAuditWriter audit)
        : this(db, students, clock, new MissingReceiptImageProcessor(), new MissingPrivateFileStorage(), audit, new NullActiveTariffCache())
    {
    }

    public CommerceService(
        CommerceDbContext db,
        IStudentLookup students,
        IDateTimeProvider clock,
        IReceiptImageProcessor imageProcessor,
        IPrivateFileStorage privateFiles,
        ICommerceAuditWriter audit)
        : this(db, students, clock, imageProcessor, privateFiles, audit, new NullActiveTariffCache())
    {
    }

    public async Task<Result<TariffResponse>> CreateTariffAsync(
        TariffFormRequest request,
        string? qrImageUrl,
        CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateTariff(request, qrImageUrl, requireQrImage: true);
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
        audit.Write(CommerceAuditActions.TariffCreated, "CommerceTariff", tariff.Id, newValues: new Dictionary<string, object?>
        {
            ["name"] = tariff.Name,
            ["price"] = tariff.Price,
            ["currency"] = tariff.Currency,
            ["durationDays"] = tariff.DurationDays,
            ["status"] = tariff.Status.ToString()
        });
        await db.SaveChangesAsync(cancellationToken);
        tariffCache.Invalidate();
        return Result<TariffResponse>.Success(ToResponse(tariff));
    }

    public async Task<Result<TariffResponse>> UpdateTariffAsync(
        Guid tariffId,
        TariffFormRequest request,
        string? qrImageUrl,
        CancellationToken cancellationToken)
    {
        var tariff = await db.Tariffs.SingleOrDefaultAsync(x => x.Id == tariffId, cancellationToken);
        if (tariff is null)
        {
            return Result<TariffResponse>.Failure(CommerceErrors.TariffNotFound);
        }

        var effectiveQrImageUrl = qrImageUrl ?? tariff.QrImageUrl;
        var validation = Validation.ValidateTariff(request, effectiveQrImageUrl, requireQrImage: false);
        if (validation.IsFailure)
        {
            return Result<TariffResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var oldValues = new Dictionary<string, object?>
        {
            ["name"] = tariff.Name,
            ["price"] = tariff.Price,
            ["currency"] = tariff.Currency,
            ["durationDays"] = tariff.DurationDays,
            ["status"] = tariff.Status.ToString()
        };
        tariff.Update(
            request.Name!,
            request.Price!.Value,
            request.Currency!,
            request.DurationDays!.Value,
            effectiveQrImageUrl,
            (CommerceTariffStatus)(request.Status ?? (int)tariff.Status),
            clock.UtcNow);
        audit.Write(CommerceAuditActions.TariffUpdated, "CommerceTariff", tariff.Id, oldValues: oldValues, newValues: new Dictionary<string, object?>
        {
            ["name"] = tariff.Name,
            ["price"] = tariff.Price,
            ["currency"] = tariff.Currency,
            ["durationDays"] = tariff.DurationDays,
            ["status"] = tariff.Status.ToString()
        });
        await db.SaveChangesAsync(cancellationToken);
        tariffCache.Invalidate();
        return Result<TariffResponse>.Success(ToResponse(tariff));
    }

    public async Task<Result<TariffResponse>> ArchiveTariffAsync(Guid tariffId, CancellationToken cancellationToken)
    {
        var tariff = await db.Tariffs.SingleOrDefaultAsync(x => x.Id == tariffId, cancellationToken);
        if (tariff is null)
        {
            return Result<TariffResponse>.Failure(CommerceErrors.TariffNotFound);
        }

        var previousStatus = tariff.Status.ToString();
        tariff.Archive(clock.UtcNow);
        audit.Write(
            CommerceAuditActions.TariffArchived,
            "CommerceTariff",
            tariff.Id,
            oldValues: new Dictionary<string, object?> { ["status"] = previousStatus },
            newValues: new Dictionary<string, object?> { ["status"] = tariff.Status.ToString() });
        await db.SaveChangesAsync(cancellationToken);
        tariffCache.Invalidate();
        return Result<TariffResponse>.Success(ToResponse(tariff));
    }

    public async Task<Result<IReadOnlyList<TariffResponse>>> GetTariffsAsync(bool admin, CancellationToken cancellationToken)
    {
        if (!admin && tariffCache.TryGet(out var cached))
        {
            return Result<IReadOnlyList<TariffResponse>>.Success(cached!);
        }

        var query = db.Tariffs.AsNoTracking();
        if (!admin)
        {
            query = query.Where(x => x.Status == CommerceTariffStatus.Active);
        }

        var tariffs = await query.OrderBy(x => x.Price).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        IReadOnlyList<TariffResponse> response = tariffs.Select(ToResponse).ToList();
        if (!admin)
        {
            tariffCache.Set(response);
        }

        return Result<IReadOnlyList<TariffResponse>>.Success(response);
    }

    public async Task<Result<PaymentReceiptResponse>> SubmitCurrentReceiptAsync(
        ClaimsPrincipal principal,
        Guid tariffId,
        SubmitPaymentReceiptFormRequest request,
        string? receiptImageUrl,
        CancellationToken cancellationToken,
        string? requestFingerprint = null)
    {
        var validation = Validation.ValidateReceiptSubmission(request, receiptImageUrl);
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

        var idempotencyKey = request.IdempotencyKey!.Trim();
        var effectiveFingerprint = requestFingerprint ?? ComputeFingerprint(student.StudentId, tariffId, "legacy");
        var existing = await db.PaymentReceipts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.StudentId == student.StudentId && x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.TariffId != tariffId || !string.Equals(existing.RequestFingerprint, effectiveFingerprint, StringComparison.Ordinal))
            {
                return Result<PaymentReceiptResponse>.Failure(CommerceErrors.IdempotencyPayloadMismatch);
            }

            return Result<PaymentReceiptResponse>.Success(ToResponse(existing));
        }

        var now = clock.UtcNow;
        var receipt = new PaymentReceipt(
            Guid.NewGuid(),
            student.StudentId,
            tariffId,
            tariff.Name,
            tariff.Price,
            tariff.Currency,
            tariff.DurationDays,
            receiptImageUrl!,
            idempotencyKey,
            now,
            effectiveFingerprint);
        db.PaymentReceipts.Add(receipt);
        audit.Write(CommerceAuditActions.ReceiptSubmitted, "PaymentReceipt", receipt.Id, receipt.StudentId, newValues: new Dictionary<string, object?>
        {
            ["tariffId"] = receipt.TariffId,
            ["status"] = receipt.Status.ToString()
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresExceptionHelper.IsUniqueViolation(ex, CommerceDatabaseConstraints.PaymentReceiptIdempotencyScopeUnique))
        {
            db.ChangeTracker.Clear();
            var raced = await db.PaymentReceipts.AsNoTracking()
                .SingleAsync(x => x.StudentId == student.StudentId && x.IdempotencyKey == idempotencyKey, cancellationToken);
            return raced.TariffId == tariffId && string.Equals(raced.RequestFingerprint, effectiveFingerprint, StringComparison.Ordinal)
                ? Result<PaymentReceiptResponse>.Success(ToResponse(raced))
                : Result<PaymentReceiptResponse>.Failure(CommerceErrors.IdempotencyPayloadMismatch);
        }

        return Result<PaymentReceiptResponse>.Success(ToResponse(receipt));
    }

    public async Task<Result<PaymentReceiptResponse>> SubmitCurrentReceiptAsync(
        ClaimsPrincipal principal,
        Guid tariffId,
        SubmitPaymentReceiptFormRequest request,
        Stream? receiptImage,
        long receiptImageLength,
        CancellationToken cancellationToken)
    {
        var keyValidation = Validation.ValidateReceiptSubmission(request, receiptImage is null ? null : "pending");
        if (keyValidation.IsFailure)
        {
            return Result<PaymentReceiptResponse>.ValidationFailure(keyValidation.ValidationErrors!);
        }

        var student = await GetActiveCurrentStudentAsync(principal, cancellationToken);
        if (student is null)
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.StudentRequired);
        }

        var processed = await imageProcessor.ProcessAsync(receiptImage!, receiptImageLength, cancellationToken);
        if (processed.IsFailure)
        {
            return Result<PaymentReceiptResponse>.Failure(processed.Error!);
        }

        var idempotencyKey = request.IdempotencyKey!.Trim();
        var fingerprint = ComputeFingerprint(student.StudentId, tariffId, processed.Value!.Sha256);
        var existing = await db.PaymentReceipts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.StudentId == student.StudentId && x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return existing.TariffId == tariffId && string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal)
                ? Result<PaymentReceiptResponse>.Success(ToResponse(existing))
                : Result<PaymentReceiptResponse>.Failure(CommerceErrors.IdempotencyPayloadMismatch);
        }

        var objectKey = $"commerce/payment-receipts/{student.StudentId:N}/{Guid.NewGuid():N}.webp";
        await using var content = new MemoryStream(processed.Value!.Content, writable: false);
        await privateFiles.SaveAsync(content, processed.Value.ContentType, objectKey, cancellationToken);
        try
        {
            var result = await SubmitCurrentReceiptAsync(principal, tariffId, request, objectKey, cancellationToken, fingerprint);
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
    }

    public async Task<Result<PrivateFileReadResult>> OpenReceiptImageAsync(Guid receiptId, CancellationToken cancellationToken)
    {
        var receipt = await db.PaymentReceipts.AsNoTracking()
            .Where(x => x.Id == receiptId)
            .Select(x => new { x.ReceiptImageObjectKey, x.StudentId })
            .SingleOrDefaultAsync(cancellationToken);
        if (receipt is null)
        {
            return Result<PrivateFileReadResult>.Failure(CommerceErrors.ReceiptNotFound);
        }

        audit.Write(CommerceAuditActions.ReceiptImageAccessed, "PaymentReceipt", receiptId, receipt.StudentId);
        await db.SaveChangesAsync(cancellationToken);
        var file = await privateFiles.OpenReadAsync(receipt.ReceiptImageObjectKey, cancellationToken);
        return file is null
            ? Result<PrivateFileReadResult>.Failure(CommerceErrors.ReceiptImageNotFound)
            : Result<PrivateFileReadResult>.Success(file);
    }

    public async Task<Result<IReadOnlyList<PaymentReceiptResponse>>> GetCurrentPaymentReceiptsAsync(
        ClaimsPrincipal principal,
        int? status,
        CancellationToken cancellationToken)
    {
        var page = await GetCurrentPaymentReceiptsPageAsync(
            principal,
            new StudentPaymentReceiptQuery { Status = status?.ToString(), Limit = 100 },
            cancellationToken);
        return page.IsFailure
            ? Result<IReadOnlyList<PaymentReceiptResponse>>.Failure(page.Error!)
            : Result<IReadOnlyList<PaymentReceiptResponse>>.Success(page.Value!.Items.Select(ToDetailsResponse).ToList());
    }

    public async Task<Result<CursorPageResponse<PaymentReceiptListItemResponse>>> GetCurrentPaymentReceiptsPageAsync(
        ClaimsPrincipal principal,
        StudentPaymentReceiptQuery request,
        CancellationToken cancellationToken)
    {
        var validation = ValidatePage(request.Limit, request.Cursor, request.Status, out var cursor, out var status);
        if (validation is not null)
        {
            return Result<CursorPageResponse<PaymentReceiptListItemResponse>>.Failure(validation);
        }

        var student = await GetActiveCurrentStudentAsync(principal, cancellationToken);
        if (student is null)
        {
            return Result<CursorPageResponse<PaymentReceiptListItemResponse>>.Failure(CommerceErrors.StudentRequired);
        }

        var query = db.PaymentReceipts.AsNoTracking().Where(x => x.StudentId == student.StudentId);

        if (status is not null)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (cursor is not null)
        {
            query = query.Where(x =>
                x.CreatedAtUtc < cursor.Value.CreatedAtUtc ||
                (x.CreatedAtUtc == cursor.Value.CreatedAtUtc && x.Id.CompareTo(cursor.Value.Id) < 0));
        }

        var rows = await ProjectReceiptList(query)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.ReceiptId)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);
        return Result<CursorPageResponse<PaymentReceiptListItemResponse>>.Success(ToPage(rows, request.Limit));
    }

    public async Task<Result<IReadOnlyList<PaymentReceiptResponse>>> GetPaymentReceiptsAsync(
        int? status,
        CancellationToken cancellationToken)
    {
        var page = await GetPaymentReceiptsPageAsync(
            new AdminPaymentReceiptQuery { Status = status?.ToString(), Limit = 100 },
            cancellationToken);
        return page.IsFailure
            ? Result<IReadOnlyList<PaymentReceiptResponse>>.Failure(page.Error!)
            : Result<IReadOnlyList<PaymentReceiptResponse>>.Success(page.Value!.Items.Select(ToDetailsResponse).ToList());
    }

    public async Task<Result<CursorPageResponse<PaymentReceiptListItemResponse>>> GetPaymentReceiptsPageAsync(
        AdminPaymentReceiptQuery request,
        CancellationToken cancellationToken)
    {
        var validation = ValidatePage(request.Limit, request.Cursor, request.Status, out var cursor, out var status);
        if (validation is not null)
        {
            return Result<CursorPageResponse<PaymentReceiptListItemResponse>>.Failure(validation);
        }

        if ((request.CreatedFrom is not null && request.CreatedTo is not null && request.CreatedFrom > request.CreatedTo) ||
            (request.ReviewedFrom is not null && request.ReviewedTo is not null && request.ReviewedFrom > request.ReviewedTo))
        {
            return Result<CursorPageResponse<PaymentReceiptListItemResponse>>.Failure(CommerceErrors.DateRangeInvalid);
        }

        var query = db.PaymentReceipts.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (request.StudentId is not null) query = query.Where(x => x.StudentId == request.StudentId);
        if (request.TariffId is not null) query = query.Where(x => x.TariffId == request.TariffId);
        if (request.ReviewedByUserId is not null) query = query.Where(x => x.ReviewedByUserId == request.ReviewedByUserId);
        if (request.CreatedFrom is not null) query = query.Where(x => x.CreatedAtUtc >= request.CreatedFrom);
        if (request.CreatedTo is not null) query = query.Where(x => x.CreatedAtUtc <= request.CreatedTo);
        if (request.ReviewedFrom is not null) query = query.Where(x => x.ReviewedAtUtc >= request.ReviewedFrom);
        if (request.ReviewedTo is not null) query = query.Where(x => x.ReviewedAtUtc <= request.ReviewedTo);
        if (cursor is not null)
        {
            query = query.Where(x =>
                x.CreatedAtUtc < cursor.Value.CreatedAtUtc ||
                (x.CreatedAtUtc == cursor.Value.CreatedAtUtc && x.Id.CompareTo(cursor.Value.Id) < 0));
        }

        var rows = await ProjectReceiptList(query)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.ReceiptId)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);
        return Result<CursorPageResponse<PaymentReceiptListItemResponse>>.Success(ToPage(rows, request.Limit));
    }

    public async Task<Result<PaymentReceiptResponse>> ApproveReceiptAsync(
        Guid receiptId,
        ClaimsPrincipal reviewer,
        ReviewPaymentReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateReview(request);
        if (validation.IsFailure)
        {
            return Result<PaymentReceiptResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var reviewerUserId = GetUserId(reviewer);
        if (reviewerUserId is null)
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.ReviewerRequired);
        }

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var receipt = await db.PaymentReceipts.SingleOrDefaultAsync(x => x.Id == receiptId, cancellationToken);
        if (receipt is null)
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.ReceiptNotFound);
        }

        await StudentEntitlementLock.AcquireAsync(db, receipt.StudentId, cancellationToken);
        var now = clock.UtcNow;
        var transition = receipt.Approve(reviewerUserId.Value, now, request.Note);
        if (transition.IsFailure)
        {
            return Result<PaymentReceiptResponse>.Failure(transition.Error!);
        }

        await EnsurePaymentEntitlementAsync(receipt, now, cancellationToken);
        audit.Write(CommerceAuditActions.ReceiptApproved, "PaymentReceipt", receipt.Id, receipt.StudentId, newValues: new Dictionary<string, object?>
        {
            ["status"] = receipt.Status.ToString(),
            ["reviewedByUserId"] = reviewerUserId
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.ReceiptConcurrencyConflict);
        }
        catch (DbUpdateException ex) when (PostgresExceptionHelper.IsUniqueViolation(ex, CommerceDatabaseConstraints.StudentEntitlementSourcePaymentReceiptUnique))
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.EntitlementAlreadyCreated);
        }

        return Result<PaymentReceiptResponse>.Success(ToResponse(receipt));
    }

    public async Task<Result<PaymentReceiptResponse>> RejectReceiptAsync(
        Guid receiptId,
        ClaimsPrincipal reviewer,
        ReviewPaymentReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateReview(request);
        if (validation.IsFailure)
        {
            return Result<PaymentReceiptResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var reviewerUserId = GetUserId(reviewer);
        if (reviewerUserId is null)
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.ReviewerRequired);
        }

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var receipt = await db.PaymentReceipts.SingleOrDefaultAsync(x => x.Id == receiptId, cancellationToken);
        if (receipt is null)
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.ReceiptNotFound);
        }

        var transition = receipt.Reject(reviewerUserId.Value, clock.UtcNow, request.Note);
        if (transition.IsFailure)
        {
            return Result<PaymentReceiptResponse>.Failure(transition.Error!);
        }

        audit.Write(CommerceAuditActions.ReceiptRejected, "PaymentReceipt", receipt.Id, receipt.StudentId, newValues: new Dictionary<string, object?>
        {
            ["status"] = receipt.Status.ToString(),
            ["reviewedByUserId"] = reviewerUserId
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<PaymentReceiptResponse>.Failure(CommerceErrors.ReceiptConcurrencyConflict);
        }

        return Result<PaymentReceiptResponse>.Success(ToResponse(receipt));
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

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        await StudentEntitlementLock.AcquireAsync(db, studentId, cancellationToken);

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
        audit.Write(CommerceAuditActions.EntitlementGranted, "StudentEntitlement", entitlement.Id, entitlement.StudentId, newValues: new Dictionary<string, object?>
        {
            ["kind"] = entitlement.Kind.ToString(),
            ["source"] = entitlement.Source.ToString(),
            ["startsAtUtc"] = entitlement.StartsAtUtc,
            ["expiresAtUtc"] = entitlement.ExpiresAtUtc
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateException ex) when (PostgresExceptionHelper.IsUniqueViolation(ex, CommerceDatabaseConstraints.StudentEntitlementIdempotencyKeyUnique))
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

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
        audit.Write(CommerceAuditActions.EntitlementRevoked, "StudentEntitlement", entitlement.Id, entitlement.StudentId, newValues: new Dictionary<string, object?>
        {
            ["status"] = entitlement.Status.ToString(),
            ["revokedAtUtc"] = entitlement.RevokedAtUtc
        });
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
        PaymentReceipt receipt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"payment-receipt:{receipt.Id:N}";
        if (await db.StudentEntitlements.AnyAsync(x => x.SourcePaymentReceiptId == receipt.Id, cancellationToken))
        {
            return;
        }

        var latestActiveExpiry = await db.StudentEntitlements
            .Where(x =>
                x.StudentId == receipt.StudentId &&
                x.Kind == CommerceEntitlementKind.Premium &&
                x.Status == CommerceEntitlementStatus.Active &&
                x.ExpiresAtUtc != null &&
                x.ExpiresAtUtc > now)
            .MaxAsync(x => (DateTimeOffset?)x.ExpiresAtUtc, cancellationToken);
        var startsAt = latestActiveExpiry ?? now;

        db.StudentEntitlements.Add(new StudentEntitlement(
            Guid.NewGuid(),
            receipt.StudentId,
            CommerceEntitlementKind.Premium,
            CommerceEntitlementSource.Payment,
            startsAt,
            startsAt.AddDays(receipt.DurationDaysSnapshot),
            idempotencyKey,
            now,
            receipt.Id));
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

    private static PaymentReceiptResponse ToResponse(PaymentReceipt receipt) =>
        new(
            receipt.Id,
            receipt.StudentId,
            receipt.TariffId,
            receipt.TariffNameSnapshot,
            receipt.PriceSnapshot,
            receipt.CurrencySnapshot,
            receipt.DurationDaysSnapshot,
            true,
            receipt.Status.ToString(),
            receipt.AdminNote,
            receipt.ReviewedByUserId,
            receipt.ReviewedAtUtc,
            receipt.CreatedAtUtc,
            receipt.UpdatedAtUtc);

    private static string ComputeFingerprint(Guid actorId, Guid tariffId, string fileHash)
    {
        var value = $"{actorId:N}:submit-payment-receipt:{tariffId:N}:{fileHash}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static IQueryable<ReceiptListRow> ProjectReceiptList(IQueryable<PaymentReceipt> query) =>
        query.Select(x => new ReceiptListRow(
            x.Id,
            x.StudentId,
            x.TariffId,
            x.TariffNameSnapshot,
            x.PriceSnapshot,
            x.CurrencySnapshot,
            x.DurationDaysSnapshot,
            x.Status,
            x.ReviewedByUserId,
            x.ReviewedAtUtc,
            x.CreatedAtUtc));

    private static CursorPageResponse<PaymentReceiptListItemResponse> ToPage(List<ReceiptListRow> rows, int limit)
    {
        var hasMore = rows.Count > limit;
        var selected = rows.Take(limit).Select(x => new PaymentReceiptListItemResponse(
            x.ReceiptId,
            x.StudentId,
            x.TariffId,
            x.TariffName,
            x.TariffPrice,
            x.Currency,
            x.DurationDays,
            true,
            x.Status.ToString(),
            x.ReviewedByUserId,
            x.ReviewedAtUtc,
            x.CreatedAtUtc)).ToList();
        var last = selected.LastOrDefault();
        var nextCursor = hasMore && last is not null
            ? PaymentReceiptCursor.Encode(last.CreatedAtUtc, last.ReceiptId)
            : null;
        return new CursorPageResponse<PaymentReceiptListItemResponse>(selected, nextCursor, hasMore);
    }

    private static PaymentReceiptResponse ToDetailsResponse(PaymentReceiptListItemResponse item) => new(
        item.ReceiptId,
        item.StudentId,
        item.TariffId,
        item.TariffName,
        item.TariffPrice,
        item.Currency,
        item.DurationDays,
        item.ReceiptImageAvailable,
        item.Status,
        null,
        item.ReviewedByUserId,
        item.ReviewedAtUtc,
        item.CreatedAtUtc,
        item.CreatedAtUtc);

    private static Adeeb.SharedKernel.Errors.Error? ValidatePage(
        int limit,
        string? cursorValue,
        string? statusValue,
        out PaymentReceiptCursor? cursor,
        out PaymentReceiptStatus? status)
    {
        cursor = null;
        status = null;
        if (limit is < 1 or > 100)
        {
            return CommerceErrors.PaginationLimitInvalid;
        }

        if (!string.IsNullOrWhiteSpace(cursorValue))
        {
            if (!PaymentReceiptCursor.TryDecode(cursorValue, out var parsedCursor))
            {
                return CommerceErrors.PaginationCursorInvalid;
            }

            cursor = parsedCursor;
        }

        if (!string.IsNullOrWhiteSpace(statusValue))
        {
            if (int.TryParse(statusValue, out var numericStatus) && Enum.IsDefined(typeof(PaymentReceiptStatus), numericStatus))
            {
                status = (PaymentReceiptStatus)numericStatus;
            }
            else if (Enum.TryParse<PaymentReceiptStatus>(statusValue, true, out var namedStatus) && Enum.IsDefined(namedStatus))
            {
                status = namedStatus;
            }
            else
            {
                return CommerceErrors.ReceiptStatusInvalid;
            }
        }

        return null;
    }

    private sealed record ReceiptListRow(
        Guid ReceiptId,
        Guid StudentId,
        Guid TariffId,
        string TariffName,
        decimal TariffPrice,
        string Currency,
        short DurationDays,
        PaymentReceiptStatus Status,
        Guid? ReviewedByUserId,
        DateTimeOffset? ReviewedAtUtc,
        DateTimeOffset CreatedAtUtc);

    private sealed class MissingReceiptImageProcessor : IReceiptImageProcessor
    {
        public Task<Result<ValidatedReceiptImage>> ProcessAsync(Stream input, long declaredLength, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Secure receipt image processor is not configured.");
    }

    private sealed class MissingPrivateFileStorage : IPrivateFileStorage
    {
        public Task<StoredFile> SaveAsync(Stream stream, string contentType, string objectKey, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Private file storage is not configured.");
        public Task<PrivateFileReadResult?> OpenReadAsync(string objectKey, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Private file storage is not configured.");
        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Private file storage is not configured.");
        public Task<string> CreateSignedReadUrlAsync(string objectKey, TimeSpan lifetime, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Private file storage is not configured.");
    }
}
