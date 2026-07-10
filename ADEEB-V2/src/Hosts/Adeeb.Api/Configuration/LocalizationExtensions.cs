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
                    var value = context.Request.Headers["X-Adeeb-Language"].FirstOrDefault();
                    if (string.IsNullOrEmpty(value))
                    {
                        value = context.User.FindFirst("lang")?.Value;
                    }
                    return SupportedLanguageExtensions.TryParseCulture(value, out var language)
                        ? Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(language.ToCultureCode()))
                        : Task.FromResult<ProviderCultureResult?>(null);
                }),
                new AcceptLanguageHeaderRequestCultureProvider()
            ];
        });

        return services;
    }
}
