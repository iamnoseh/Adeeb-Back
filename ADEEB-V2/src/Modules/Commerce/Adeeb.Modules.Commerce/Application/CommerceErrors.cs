using Adeeb.SharedKernel.Errors;

namespace Adeeb.Modules.Commerce.Application;

public static class CommerceErrors
{
    public static readonly Error StudentRequired = Error.Conflict("commerce.student_required", "Commerce.StudentRequired");
    public static readonly Error StudentNotFound = Error.NotFound("commerce.student_not_found", "Commerce.StudentNotFound");
    public static readonly Error EntitlementNotFound = Error.NotFound("commerce.entitlement_not_found", "Commerce.EntitlementNotFound");
    public static readonly Error IdempotencyKeyInUse = Error.Conflict("commerce.idempotency_key.in_use", "Commerce.IdempotencyKey.InUse");
}
