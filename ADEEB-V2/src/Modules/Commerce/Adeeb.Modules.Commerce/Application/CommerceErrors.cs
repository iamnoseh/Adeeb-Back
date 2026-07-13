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
    public static readonly Error ReceiptAlreadyReviewed = Domain.Payments.PaymentReceiptErrors.AlreadyReviewed;
    public static readonly Error ReceiptConcurrencyConflict = Error.Conflict("commerce.receipt_concurrency_conflict", "Commerce.ReceiptConcurrencyConflict");
    public static readonly Error EntitlementAlreadyCreated = Error.Conflict("commerce.entitlement_already_created", "Commerce.EntitlementAlreadyCreated");
    public static readonly Error ReviewerRequired = Domain.Payments.PaymentReceiptErrors.ReviewerRequired;
    public static readonly Error ReceiptImageRequired = Error.Validation("commerce.receipt.image.required", "Commerce.Receipt.Image.Required");
    public static readonly Error ReceiptImageInvalidType = Error.Validation("commerce.receipt.image.invalid_type", "Commerce.Receipt.Image.InvalidType");
    public static readonly Error ReceiptImageCorrupted = Error.Validation("commerce.receipt.image.corrupted", "Commerce.Receipt.Image.Corrupted");
    public static readonly Error ImageTooLarge = Error.Validation("commerce.image.too_large", "Commerce.Image.TooLarge");
    public static readonly Error ImageDimensionsInvalid = Error.Validation("commerce.image.dimensions.invalid", "Commerce.Image.Dimensions.Invalid");
    public static readonly Error ReceiptImageNotFound = Error.NotFound("commerce.receipt.image_not_found", "Commerce.Receipt.ImageNotFound");
}
