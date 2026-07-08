using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.Identity.Application;
using Adeeb.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Adeeb.Modules.Identity.Endpoints;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/auth").WithTags("Identity");

        group.MapPost("/register", async (RegisterRequest request, IdentityService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.RegisterAsync(request, ToClientContext(context), ct)).ToHttpResult(context, localizer))
            .RequireRateLimiting("auth-register");

        group.MapPost("/login", async (LoginRequest request, IdentityService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.LoginAsync(request, ToClientContext(context), ct)).ToHttpResult(context, localizer))
            .RequireRateLimiting("auth-login");

        group.MapPost("/refresh", async (RefreshTokenRequest request, IdentityService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.RefreshAsync(request, ToClientContext(context), ct)).ToHttpResult(context, localizer))
            .RequireRateLimiting("auth-refresh");

        group.MapPost("/logout", async (IdentityService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.LogoutAsync(context.User, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapPost("/logout-all", async (IdentityService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.LogoutAllAsync(context.User, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapGet("/sessions", async (IdentityService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetSessionsAsync(context.User, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapDelete("/sessions/{sessionId:guid}", async (Guid sessionId, IdentityService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.RevokeSessionAsync(context.User, sessionId, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapGet("/me", async (IdentityService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetCurrentUserAsync(context.User, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapPost("/change-password", async (ChangePasswordRequest request, IdentityService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.ChangePasswordAsync(context.User, request, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization()
            .RequireRateLimiting("auth-change-password");

        return app;
    }

    private static ClientContext ToClientContext(HttpContext context) =>
        new(context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString());
}
