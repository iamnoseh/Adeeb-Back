using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.Commerce.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Adeeb.Modules.Commerce.Endpoints;

public static class CommerceEndpoints
{
    public static IEndpointRouteBuilder MapCommerceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/commerce").WithTags("Commerce");

        group.MapGet("/me/entitlements", async (
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await service.GetCurrentEntitlementsAsync(context.User, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        return app;
    }
}
