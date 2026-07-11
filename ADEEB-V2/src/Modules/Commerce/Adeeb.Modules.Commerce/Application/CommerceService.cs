using System.Security.Claims;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.Modules.Commerce.Domain.Entitlements;
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
        var identityUserId = GetUserId(principal);
        if (identityUserId is null)
        {
            return Result<StudentEntitlementSummaryResponse>.Failure(CommerceErrors.StudentRequired);
        }

        var student = await students.FindByIdentityUserIdAsync(identityUserId.Value, cancellationToken);
        if (student is null || !string.Equals(student.Status, "Active", StringComparison.Ordinal))
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
}
