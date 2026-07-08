using Adeeb.Api.Documentation.Configuration;
using Adeeb.Api.Documentation.Rendering;
using Adeeb.Api.Documentation.Services;

namespace Adeeb.Api.Documentation;

public static class DependencyInjection
{
    public static IServiceCollection AddAdeebDocumentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DocumentationOptions>()
            .Bind(configuration.GetSection(DocumentationOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<SafeMarkdownRenderer>();
        services.AddSingleton<DocumentationPageRenderer>();
        services.AddSingleton<IDocumentationCatalog, FileDocumentationCatalog>();
        return services;
    }
}
