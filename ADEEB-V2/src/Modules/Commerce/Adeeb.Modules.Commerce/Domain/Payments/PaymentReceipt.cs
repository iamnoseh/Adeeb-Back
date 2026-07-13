using Adeeb.SharedKernel.Domain;
using Adeeb.SharedKernel.Results;
using Adeeb.Modules.Commerce.Domain;

namespace Adeeb.Modules.Commerce.Domain.Payments;

public sealed class PaymentReceipt : Entity
{
    public const int ReceiptImageObjectKeyMaxLength = 512;
    public const int AdminNoteMaxLength = 512;
    public const int IdempotencyKeyMaxLength = 128;

    private PaymentReceipt() { }

    public PaymentReceipt(
        Guid id,
        Guid studentId,
        Guid tariffId,
        string tariffNameSnapshot,
        decimal priceSnapshot,
        string currencySnapshot,
        short durationDaysSnapshot,
        string receiptImageObjectKey,
        string idempotencyKey,
        DateTimeOffset now,
        string requestFingerprint = "legacy")
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Payment receipt id is required.", nameof(id));
        }

        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("Student id is required.", nameof(studentId));
        }

        if (tariffId == Guid.Empty)
        {
            throw new ArgumentException("Tariff id is required.", nameof(tariffId));
        }

        if (string.IsNullOrWhiteSpace(tariffNameSnapshot) || tariffNameSnapshot.Trim().Length > Tariffs.CommerceTariff.NameMaxLength)
        {
            throw new ArgumentException("Tariff snapshot name is invalid.", nameof(tariffNameSnapshot));
        }

        if (priceSnapshot <= 0)
        {
            throw new ArgumentException("Tariff snapshot price must be positive.", nameof(priceSnapshot));
        }

        if (!SupportedCurrencies.TryNormalize(currencySnapshot, out var normalizedCurrency))
        {
            throw new ArgumentException("Tariff snapshot currency is unsupported.", nameof(currencySnapshot));
        }

        if (durationDaysSnapshot <= 0)
        {
            throw new ArgumentException("Tariff snapshot duration must be positive.", nameof(durationDaysSnapshot));
        }

        if (string.IsNullOrWhiteSpace(receiptImageObjectKey) || receiptImageObjectKey.Trim().Length > ReceiptImageObjectKeyMaxLength)
        {
            throw new ArgumentException("Receipt image object key is invalid.", nameof(receiptImageObjectKey));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > IdempotencyKeyMaxLength)
        {
            throw new ArgumentException("Idempotency key is invalid.", nameof(idempotencyKey));
        }

        if (string.IsNullOrWhiteSpace(requestFingerprint) || requestFingerprint.Trim().Length > 128)
        {
            throw new ArgumentException("Request fingerprint is invalid.", nameof(requestFingerprint));
        }

        Id = id;
        StudentId = studentId;
        TariffId = tariffId;
        TariffNameSnapshot = tariffNameSnapshot.Trim();
        PriceSnapshot = priceSnapshot;
        CurrencySnapshot = normalizedCurrency;
        DurationDaysSnapshot = durationDaysSnapshot;
        ReceiptImageObjectKey = receiptImageObjectKey.Trim();
        IdempotencyKey = idempotencyKey.Trim();
        RequestFingerprint = requestFingerprint.Trim();
        Status = PaymentReceiptStatus.Pending;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid StudentId { get; private set; }
    public Guid TariffId { get; private set; }
    public string TariffNameSnapshot { get; private set; } = string.Empty;
    public decimal PriceSnapshot { get; private set; }
    public string CurrencySnapshot { get; private set; } = "TJS";
    public short DurationDaysSnapshot { get; private set; }
    public string ReceiptImageObjectKey { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string RequestFingerprint { get; private set; } = string.Empty;
    public PaymentReceiptStatus Status { get; private set; }
    public string? AdminNote { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public uint Version { get; private set; }

    public Result Approve(Guid reviewerUserId, DateTimeOffset now, string? note)
    {
        var canReview = EnsurePending();
        if (canReview.IsFailure)
        {
            return canReview;
        }

        if (reviewerUserId == Guid.Empty)
        {
            return Result.Failure(PaymentReceiptErrors.ReviewerRequired);
        }

        Status = PaymentReceiptStatus.Approved;
        AdminNote = NormalizeNote(note);
        ReviewedByUserId = reviewerUserId;
        ReviewedAtUtc = now;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result Reject(Guid reviewerUserId, DateTimeOffset now, string? note)
    {
        var canReview = EnsurePending();
        if (canReview.IsFailure)
        {
            return canReview;
        }

        if (reviewerUserId == Guid.Empty)
        {
            return Result.Failure(PaymentReceiptErrors.ReviewerRequired);
        }

        Status = PaymentReceiptStatus.Rejected;
        AdminNote = NormalizeNote(note);
        ReviewedByUserId = reviewerUserId;
        ReviewedAtUtc = now;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    private Result EnsurePending()
    {
        if (Status != PaymentReceiptStatus.Pending)
        {
            return Result.Failure(PaymentReceiptErrors.AlreadyReviewed);
        }

        return Result.Success();
    }

    private static string? NormalizeNote(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim();
}
