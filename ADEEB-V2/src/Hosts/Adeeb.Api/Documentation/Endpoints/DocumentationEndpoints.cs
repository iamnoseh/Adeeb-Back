using Adeeb.Api.Documentation.Configuration;
using Adeeb.Api.Documentation.Rendering;
using Adeeb.Api.Documentation.Services;
using Microsoft.Extensions.Options;

namespace Adeeb.Api.Documentation.Endpoints;

public static class DocumentationEndpoints
{
    public static IEndpointRouteBuilder MapAdeebDocumentation(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("");
        var options = app.ServiceProvider.GetRequiredService<IOptions<DocumentationOptions>>().Value;

        if (!options.Enabled)
        {
            return app;
        }

        var docs = group.MapGroup("/docs");
        if (options.RequireAuthorization)
        {
            docs.RequireAuthorization();
        }

        docs.MapGet("", (IDocumentationCatalog catalog, DocumentationPageRenderer renderer) =>
            Results.Content(renderer.RenderHome(catalog.GetAll()), "text/html; charset=utf-8"))
            .ExcludeFromDescription();

        docs.MapGet("/{**slug}", (string slug, IDocumentationCatalog catalog, DocumentationPageRenderer renderer) =>
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Results.Content(renderer.RenderHome(catalog.GetAll()), "text/html; charset=utf-8");
            }

            var item = catalog.GetBySlug(slug);
            return item is null
                ? Results.Content(renderer.RenderNotFound(catalog.GetAll(), slug), "text/html; charset=utf-8", statusCode: StatusCodes.Status404NotFound)
                : Results.Content(renderer.RenderItem(catalog.GetAll(), item), "text/html; charset=utf-8");
        })
        .ExcludeFromDescription();

        return app;
    }
}
