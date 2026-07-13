using Adeeb.SharedKernel.Domain;
using Adeeb.SharedKernel.Results;

namespace Adeeb.Modules.Commerce.Domain.Payments;

public sealed class PaymentReceipt : Entity
{
    public const int ReceiptImageUrlMaxLength = 512;
    public const int AdminNoteMaxLength = 512;
    public const int IdempotencyKeyMaxLength = 128;

    private PaymentReceipt() { }

    public PaymentReceipt(Guid id, Guid studentId, Guid tariffId, string receiptImageUrl, string idempotencyKey, DateTimeOffset now)
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

        if (string.IsNullOrWhiteSpace(receiptImageUrl) || receiptImageUrl.Trim().Length > ReceiptImageUrlMaxLength)
        {
            throw new ArgumentException("Receipt image URL is invalid.", nameof(receiptImageUrl));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > IdempotencyKeyMaxLength)
        {
            throw new ArgumentException("Idempotency key is invalid.", nameof(idempotencyKey));
        }

        Id = id;
        StudentId = studentId;
        TariffId = tariffId;
        ReceiptImageUrl = receiptImageUrl.Trim();
        IdempotencyKey = idempotencyKey.Trim();
        Status = PaymentReceiptStatus.Pending;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid StudentId { get; private set; }
    public Guid TariffId { get; private set; }
    public string ReceiptImageUrl { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
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
