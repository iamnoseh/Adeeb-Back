using System.Globalization;
using Adeeb.Application.Abstractions.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Api.Configuration;

public static class LocalizationExtensions
{
    public static IServiceCollection AddAdeebLocalization(this IServiceCollection services)
    {
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var cultures = SupportedLanguageExtensions.SupportedCultures.Select(x => new CultureInfo(x)).ToList();
            options.DefaultRequestCulture = new RequestCulture(SupportedLanguageExtensions.DefaultCulture);
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
            options.RequestCultureProviders =
            [
                new CustomRequestCultureProvider(context =>
                {
                    var headerValue = context.Request.Headers["X-Adeeb-Language"].FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(headerValue)
                        && SupportedLanguageExtensions.TryParseCulture(headerValue, out var headerLanguage))
                    {
                        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(headerLanguage.ToCultureCode()));
                    }

                    var claimValue = context.User.FindFirst("lang")?.Value;
                    return !string.IsNullOrWhiteSpace(claimValue)
                        && SupportedLanguageExtensions.TryParseCulture(claimValue, out var claimLanguage)
                        ? Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(claimLanguage.ToCultureCode()))
                        : Task.FromResult<ProviderCultureResult?>(null);
                }),
                new AcceptLanguageHeaderRequestCultureProvider()
            ];
        });

        return services;
    }
}
