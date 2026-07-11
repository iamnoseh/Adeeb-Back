using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.Commerce.Application;
using Adeeb.Modules.Commerce.Contracts;
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

        var admin = app.MapGroup("/api/v2/admin/commerce")
            .WithTags("Commerce Admin")
            .RequireAuthorization("ContentAdmin");

        admin.MapPost("/students/{studentId:guid}/premium-grants", async (
            Guid studentId,
            GrantPremiumEntitlementRequest request,
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await service.GrantPremiumAsync(studentId, request, ct)).ToHttpResult(context, localizer));

        admin.MapPost("/entitlements/{entitlementId:guid}/revoke", async (
            Guid entitlementId,
            RevokeEntitlementRequest request,
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await service.RevokeEntitlementAsync(entitlementId, request, ct)).ToHttpResult(context, localizer));

        return app;
    }
}
