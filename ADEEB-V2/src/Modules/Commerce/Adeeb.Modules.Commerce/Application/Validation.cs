using Adeeb.Modules.Commerce.Contracts;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;

namespace Adeeb.Modules.Commerce.Application;

internal static class Validation
{
    public static Result ValidateGrantPremium(GrantPremiumEntitlementRequest request, DateTimeOffset now)
    {
        var errors = new Dictionary<string, IReadOnlyList<Error>>();

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Trim().Length > 128)
        {
            errors["idempotencyKey"] = [Error.Validation("commerce.idempotency_key.invalid", "Commerce.IdempotencyKey.Invalid")];
        }

        var startsAt = request.StartsAtUtc ?? now;
        if (request.ExpiresAtUtc is not null && request.ExpiresAtUtc <= startsAt)
        {
            errors["expiresAtUtc"] = [Error.Validation("commerce.expires_at.invalid", "Commerce.ExpiresAt.Invalid")];
        }

        return errors.Count == 0 ? Result.Success() : Result.ValidationFailure(errors);
    }

    public static Result ValidateRevoke(RevokeEntitlementRequest request)
    {
        if (request.Reason is not null && request.Reason.Trim().Length > 256)
        {
            return Result.ValidationFailure(new Dictionary<string, IReadOnlyList<Error>>
            {
                ["reason"] = [Error.Validation("commerce.revoke_reason.invalid", "Commerce.RevokeReason.Invalid")]
            });
        }

        return Result.Success();
    }
}
