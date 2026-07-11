using Adeeb.SharedKernel.Errors;

namespace Adeeb.Modules.Commerce.Application;

public static class CommerceErrors
{
    public static readonly Error StudentRequired = Error.Conflict("commerce.student_required", "Commerce.StudentRequired");
    public static readonly Error StudentNotFound = Error.NotFound("commerce.student_not_found", "Commerce.StudentNotFound");
    public static readonly Error EntitlementNotFound = Error.NotFound("commerce.entitlement_not_found", "Commerce.EntitlementNotFound");
    public static readonly Error IdempotencyKeyInUse = Error.Conflict("commerce.idempotency_key.in_use", "Commerce.IdempotencyKey.InUse");
    public static readonly Error TariffNotFound = Error.NotFound("commerce.tariff_not_found", "Commerce.TariffNotFound");
    public static readonly Error ReceiptNotFound = Error.NotFound("commerce.receipt_not_found", "Commerce.ReceiptNotFound");
    public static readonly Error ReceiptAlreadyReviewed = Error.Conflict("commerce.receipt_already_reviewed", "Commerce.ReceiptAlreadyReviewed");
    public static readonly Error ReviewerRequired = Error.Unauthorized("commerce.reviewer_required", "Commerce.ReviewerRequired");
}
