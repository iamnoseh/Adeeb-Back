namespace Adeeb.Modules.Commerce.Contracts;

public sealed record StudentEntitlementSummaryResponse(
    Guid StudentId,
    string AccessLevel,
    bool PremiumActive,
    DateTimeOffset? PremiumUntilUtc,
    string Source);
