using System.Security.Claims;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Commerce.Application.Auditing;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.Modules.Commerce.Domain.Entitlements;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Adeeb.Modules.Students.Contracts;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.Commerce.Application.Entitlements;

public sealed class EntitlementUseCases(
    CommerceDbContext db,
    IStudentLookup students,
    IDateTimeProvider clock,
    ICommerceAuditWriter audit)
{
    public async Task<Result<StudentEntitlementSummaryResponse>> GetCurrentAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var student = await GetActiveStudentAsync(principal, cancellationToken);
        if (student is null) return Result<StudentEntitlementSummaryResponse>.Failure(CommerceErrors.StudentRequired);
        var now = clock.UtcNow;
        var premium = await db.StudentEntitlements.AsNoTracking()
            .Where(x => x.StudentId == student.StudentId &&
                        x.Kind == CommerceEntitlementKind.Premium &&
                        x.Status == CommerceEntitlementStatus.Active &&
                        x.StartsAtUtc <= now &&
                        (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now))
            .OrderByDescending(x => x.ExpiresAtUtc ?? DateTimeOffset.MaxValue)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return premium is null
            ? Result<StudentEntitlementSummaryResponse>.Success(new(student.StudentId, "Free", false, null, "default"))
            : Result<StudentEntitlementSummaryResponse>.Success(new(
                student.StudentId, "Premium", true, premium.ExpiresAtUtc, premium.Source.ToString()));
    }

    public async Task<Result<StudentEntitlementResponse>> GrantPremiumAsync(
        Guid studentId,
        GrantPremiumEntitlementRequest request,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var validation = Validation.ValidateGrantPremium(request, now);
        if (validation.IsFailure) return Result<StudentEntitlementResponse>.ValidationFailure(validation.ValidationErrors!);
        var student = await students.FindByStudentIdAsync(studentId, cancellationToken);
        if (student is null || !string.Equals(student.Status, "Active", StringComparison.Ordinal))
            return Result<StudentEntitlementResponse>.Failure(CommerceErrors.StudentNotFound);

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        await StudentEntitlementLock.AcquireAsync(db, studentId, cancellationToken);
        var key = request.IdempotencyKey.Trim();
        var existing = await db.StudentEntitlements.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
        if (existing is not null)
            return existing.StudentId == studentId
                ? Result<StudentEntitlementResponse>.Success(ToResponse(existing))
                : Result<StudentEntitlementResponse>.Failure(CommerceErrors.IdempotencyKeyInUse);

        var entitlement = new StudentEntitlement(
            Guid.NewGuid(), studentId, CommerceEntitlementKind.Premium, CommerceEntitlementSource.ManualGrant,
            request.StartsAtUtc ?? now, request.ExpiresAtUtc, key, now);
        db.StudentEntitlements.Add(entitlement);
        audit.Write(CommerceAuditActions.EntitlementGranted, "StudentEntitlement", entitlement.Id, studentId, newValues: new Dictionary<string, object?>
        {
            ["kind"] = entitlement.Kind.ToString(),
            ["source"] = entitlement.Source.ToString(),
            ["startsAtUtc"] = entitlement.StartsAtUtc,
            ["expiresAtUtc"] = entitlement.ExpiresAtUtc
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (PostgresExceptionHelper.IsUniqueViolation(
            exception, CommerceDatabaseConstraints.StudentEntitlementIdempotencyKeyUnique))
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            var raced = await db.StudentEntitlements.AsNoTracking().SingleAsync(x => x.IdempotencyKey == key, cancellationToken);
            return raced.StudentId == studentId
                ? Result<StudentEntitlementResponse>.Success(ToResponse(raced))
                : Result<StudentEntitlementResponse>.Failure(CommerceErrors.IdempotencyKeyInUse);
        }

        return Result<StudentEntitlementResponse>.Success(ToResponse(entitlement));
    }

    public async Task<Result<StudentEntitlementResponse>> RevokeAsync(
        Guid entitlementId,
        RevokeEntitlementRequest request,
        CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateRevoke(request);
        if (validation.IsFailure) return Result<StudentEntitlementResponse>.ValidationFailure(validation.ValidationErrors!);
        var entitlement = await db.StudentEntitlements.SingleOrDefaultAsync(x => x.Id == entitlementId, cancellationToken);
        if (entitlement is null) return Result<StudentEntitlementResponse>.Failure(CommerceErrors.EntitlementNotFound);
        entitlement.Revoke(clock.UtcNow, request.Reason);
        audit.Write(CommerceAuditActions.EntitlementRevoked, "StudentEntitlement", entitlement.Id, entitlement.StudentId, newValues: new Dictionary<string, object?>
        {
            ["status"] = entitlement.Status.ToString(),
            ["revokedAtUtc"] = entitlement.RevokedAtUtc
        });
        await db.SaveChangesAsync(cancellationToken);
        return Result<StudentEntitlementResponse>.Success(ToResponse(entitlement));
    }

    private async Task<StudentReference?> GetActiveStudentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var id = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var parsed)
            ? parsed
            : (Guid?)null;
        if (id is null) return null;
        var student = await students.FindByIdentityUserIdAsync(id.Value, cancellationToken);
        return student is not null && string.Equals(student.Status, "Active", StringComparison.Ordinal) ? student : null;
    }

    private static StudentEntitlementResponse ToResponse(StudentEntitlement entitlement) => new(
        entitlement.Id, entitlement.StudentId, entitlement.Kind.ToString(), entitlement.Status.ToString(),
        entitlement.Source.ToString(), entitlement.StartsAtUtc, entitlement.ExpiresAtUtc, entitlement.IdempotencyKey,
        entitlement.RevokeReason, entitlement.RevokedAtUtc, entitlement.CreatedAtUtc, entitlement.UpdatedAtUtc);
}
