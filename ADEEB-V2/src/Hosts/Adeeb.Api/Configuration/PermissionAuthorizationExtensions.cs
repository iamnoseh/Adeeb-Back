using Adeeb.Application.Abstractions.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Api.Configuration;

public static class PermissionAuthorizationExtensions
{
    public static IServiceCollection AddAdeebAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(permission, policy => policy.RequireAssertion(context =>
                    context.User.HasClaim(AdeebClaimNames.Permission, permission) ||
                    context.User.IsInRole("SuperAdmin")));
            }
        });
        return services;
    }
}
