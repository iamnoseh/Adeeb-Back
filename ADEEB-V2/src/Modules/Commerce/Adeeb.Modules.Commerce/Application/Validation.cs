using Adeeb.Modules.Commerce.Contracts;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;

namespace Adeeb.Modules.Commerce.Application;

internal static class Validation
{
    public static Result ValidateTariff(TariffFormRequest request, string? qrImageUrl)
    {
        var errors = new Dictionary<string, IReadOnlyList<Error>>();

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > Domain.Tariffs.CommerceTariff.NameMaxLength)
        {
            errors["name"] = [Error.Validation("commerce.tariff.name.invalid", "Commerce.Tariff.Name.Invalid")];
        }

        if (request.Price is null or <= 0)
        {
            errors["price"] = [Error.Validation("commerce.tariff.price.invalid", "Commerce.Tariff.Price.Invalid")];
        }

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Trim().Length != Domain.Tariffs.CommerceTariff.CurrencyMaxLength)
        {
            errors["currency"] = [Error.Validation("commerce.tariff.currency.invalid", "Commerce.Tariff.Currency.Invalid")];
        }

        if (request.DurationDays is null or <= 0)
        {
            errors["durationDays"] = [Error.Validation("commerce.tariff.duration.invalid", "Commerce.Tariff.Duration.Invalid")];
        }

        if (string.IsNullOrWhiteSpace(qrImageUrl))
        {
            errors["qrImage"] = [Error.Validation("commerce.tariff.qr_image.required", "Commerce.Tariff.QrImage.Required")];
        }

        if (request.Status is not null && !Enum.IsDefined(typeof(Domain.Tariffs.CommerceTariffStatus), request.Status.Value))
        {
            errors["status"] = [Error.Validation("commerce.tariff.status.invalid", "Commerce.Tariff.Status.Invalid")];
        }

        return errors.Count == 0 ? Result.Success() : Result.ValidationFailure(errors);
    }

    public static Result ValidateReceiptImage(string? receiptImageUrl)
    {
        if (string.IsNullOrWhiteSpace(receiptImageUrl))
        {
            return Result.ValidationFailure(new Dictionary<string, IReadOnlyList<Error>>
            {
                ["receiptImage"] = [Error.Validation("commerce.receipt.image.required", "Commerce.Receipt.Image.Required")]
            });
        }

        return Result.Success();
    }

    public static Result ValidateReview(ReviewPaymentReceiptRequest request)
    {
        if (request.Note is not null && request.Note.Trim().Length > Domain.Payments.PaymentReceipt.AdminNoteMaxLength)
        {
            return Result.ValidationFailure(new Dictionary<string, IReadOnlyList<Error>>
            {
                ["note"] = [Error.Validation("commerce.review_note.invalid", "Commerce.ReviewNote.Invalid")]
            });
        }

        return Result.Success();
    }

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
