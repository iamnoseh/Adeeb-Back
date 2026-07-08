namespace Adeeb.Application.Abstractions.Localization;

public enum SupportedLanguage
{
    Tajik = 0,
    Russian = 1,
    English = 2
}

public static class SupportedLanguageExtensions
{
    public const string DefaultCulture = "tg-TJ";

    public static string ToCultureCode(this SupportedLanguage language) =>
        language switch
        {
            SupportedLanguage.Tajik => "tg-TJ",
            SupportedLanguage.Russian => "ru-RU",
            SupportedLanguage.English => "en-US",
            _ => DefaultCulture
        };

    public static bool TryParseCulture(string? value, out SupportedLanguage language)
    {
        language = SupportedLanguage.Tajik;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        language = value.Trim().ToLowerInvariant() switch
        {
            "tg" or "tg-tj" => SupportedLanguage.Tajik,
            "ru" or "ru-ru" => SupportedLanguage.Russian,
            "en" or "en-us" => SupportedLanguage.English,
            _ => language
        };

        return value.Trim().ToLowerInvariant() is "tg" or "tg-tj" or "ru" or "ru-ru" or "en" or "en-us";
    }

    public static IReadOnlyList<string> SupportedCultures { get; } = ["tg-TJ", "ru-RU", "en-US"];
}
