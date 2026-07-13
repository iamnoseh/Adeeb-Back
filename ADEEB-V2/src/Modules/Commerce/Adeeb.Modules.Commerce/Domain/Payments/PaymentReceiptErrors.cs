using Adeeb.SharedKernel.Errors;

namespace Adeeb.Modules.Commerce.Domain.Payments;

public static class PaymentReceiptErrors
{
    public static readonly Error AlreadyReviewed = Error.Conflict(
        "commerce.receipt_already_reviewed",
        "Commerce.ReceiptAlreadyReviewed");

    public static readonly Error ReviewerRequired = Error.Unauthorized(
        "commerce.reviewer_required",
        "Commerce.ReviewerRequired");
}
