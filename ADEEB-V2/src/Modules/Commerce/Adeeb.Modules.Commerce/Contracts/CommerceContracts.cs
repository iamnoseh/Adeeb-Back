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
    public IFormFile? ReceiptImage { get; init; }
}

public sealed record ReviewPaymentReceiptRequest(string? Note);

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
    string ReceiptImageUrl,
    string Status,
    string? AdminNote,
    DateTimeOffset? ReviewedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
