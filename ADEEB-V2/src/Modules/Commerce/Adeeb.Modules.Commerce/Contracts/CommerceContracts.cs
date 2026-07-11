namespace Adeeb.Modules.Commerce.Contracts;

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
