using Adeeb.Application.Abstractions.Localization;

namespace Adeeb.Identity.Tests;

public sealed class SupportedLanguageTests
{
    [Theory]
    [InlineData(null, SupportedLanguage.Tajik, true)]
    [InlineData("tg-TJ", SupportedLanguage.Tajik, true)]
    [InlineData("ru-RU", SupportedLanguage.Russian, true)]
    [InlineData("en-US", SupportedLanguage.English, true)]
    [InlineData("fr-FR", SupportedLanguage.Tajik, false)]
    public void Culture_parsing_is_restricted_to_supported_languages(string? culture, SupportedLanguage expected, bool isValid)
    {
        var result = SupportedLanguageExtensions.TryParseCulture(culture, out var language);

        Assert.Equal(isValid, result);
        Assert.Equal(expected, language);
    }
}
