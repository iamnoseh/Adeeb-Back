using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Adeeb.Modules.Students.Application;

internal sealed record SchoolSearchTerms(string NormalizedQuery, int? Number, string? TypeHint);

internal static partial class EducationNormalization
{
    private static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["мактаби"] = "school",
        ["мактаб"] = "school",
        ["мтму"] = "school",
        ["муассисаи таҳсилоти миёнаи умумӣ"] = "school",
        ["школа"] = "school",
        ["сш"] = "school",
        ["средняя школа"] = "school",
        ["лицей"] = "lyceum",
        ["литсей"] = "lyceum",
        ["гимназия"] = "gymnasium"
    };

    public static string Key(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Normalize(NormalizationForm.FormC).ToLowerInvariant();
        normalized = NumberPrefix().Replace(normalized.Replace('№', ' '), " ");
        normalized = NonLetterOrNumber().Replace(normalized, " ");
        normalized = Whitespace().Replace(normalized, " ").Trim();
        foreach (var alias in Aliases.OrderByDescending(x => x.Key.Length))
        {
            normalized = normalized.Replace(alias.Key, alias.Value, StringComparison.Ordinal);
        }
        return Whitespace().Replace(normalized, " ").Trim();
    }

    public static SchoolSearchTerms ParseSearch(string? query)
    {
        var normalized = Key(query);
        var numberMatch = NumberToken().Match(normalized);
        int? number = numberMatch.Success && int.TryParse(numberMatch.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
        var typeHint = normalized.Contains("lyceum", StringComparison.Ordinal) ? "lyceum"
            : normalized.Contains("gymnasium", StringComparison.Ordinal) ? "gymnasium"
            : null;
        return new(normalized, number, typeHint);
    }

    public static string SearchText(string? nameTg, string? nameRu, string? shortName, int? number) =>
        string.Join(' ', new[] { Key(nameTg), Key(nameRu), Key(shortName), number?.ToString(CultureInfo.InvariantCulture) }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

    [GeneratedRegex("[^\\p{L}\\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonLetterOrNumber();

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();

    [GeneratedRegex("\\d+", RegexOptions.CultureInvariant)]
    private static partial Regex NumberToken();

    [GeneratedRegex("\\b(?:no|n)\\s*(?=\\d)", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPrefix();
}
