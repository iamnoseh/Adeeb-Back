namespace Adeeb.Modules.Commerce.Domain;

public static class SupportedCurrencies
{
    private static readonly HashSet<string> Values = new(StringComparer.Ordinal)
    {
        "TJS",
        "USD",
        "RUB"
    };

    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        return Values.Contains(normalized);
    }
}
