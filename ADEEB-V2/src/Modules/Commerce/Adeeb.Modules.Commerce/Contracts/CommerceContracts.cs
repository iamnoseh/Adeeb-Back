namespace Adeeb.Modules.Commerce.Contracts;

using Microsoft.AspNetCore.Http;

public sealed record StudentEntitlementSummaryResponse(
    Guid StudentId,
    string AccessLevel,
    bool PremiumActive,
    DateTimeOffset? PremiumUntilUtc,
    string Source);

public sealed record GrantPremiumEntitlementRequest(
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string IdempotencyKey);

public sealed record RevokeEntitlementRequest(string? Reason);

public sealed record StudentEntitlementResponse(
    Guid EntitlementId,
    Guid StudentId,
    string Kind,
    string Status,
    string Source,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string IdempotencyKey,
    string? RevokeReason,
    DateTimeOffset? RevokedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed class TariffFormRequest
{
    public string? Name { get; init; }
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
    public short? DurationDays { get; init; }
    public int? Status { get; init; }
    public IFormFile? QrImage { get; init; }
}

public sealed class SubmitPaymentReceiptFormRequest
{
    public string? IdempotencyKey { get; init; }
    public IFormFile? ReceiptImage { get; init; }
}

public sealed record ReviewPaymentReceiptRequest(string? Note);

public sealed record CursorPageResponse<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore);

public sealed class StudentPaymentReceiptQuery
{
    public string? Status { get; init; }
    public int Limit { get; init; } = 30;
    public string? Cursor { get; init; }
}

public sealed class AdminPaymentReceiptQuery
{
    public string? Status { get; init; }
    public Guid? StudentId { get; init; }
    public Guid? TariffId { get; init; }
    public Guid? ReviewedByUserId { get; init; }
    public DateTimeOffset? CreatedFrom { get; init; }
    public DateTimeOffset? CreatedTo { get; init; }
    public DateTimeOffset? ReviewedFrom { get; init; }
    public DateTimeOffset? ReviewedTo { get; init; }
    public int Limit { get; init; } = 30;
    public string? Cursor { get; init; }
}

public sealed record PaymentReceiptListItemResponse(
    Guid ReceiptId,
    Guid StudentId,
    Guid TariffId,
    string TariffName,
    decimal TariffPrice,
    string Currency,
    short DurationDays,
    bool ReceiptImageAvailable,
    string Status,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAtUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record TariffResponse(
    Guid TariffId,
    string Name,
    decimal Price,
    string Currency,
    short DurationDays,
    string QrImageUrl,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PaymentReceiptResponse(
    Guid ReceiptId,
    Guid StudentId,
    Guid TariffId,
    string TariffName,
    decimal TariffPrice,
    string Currency,
    short DurationDays,
    bool ReceiptImageAvailable,
    string Status,
    string? AdminNote,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
