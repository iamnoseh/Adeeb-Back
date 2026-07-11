using System.Threading.RateLimiting;
using Adeeb.Application.Abstractions.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Adeeb.Api.Configuration;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddAdeebRateLimiting(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                var localizer = context.HttpContext.RequestServices.GetRequiredService<IMessageLocalizer>();
                await Results.Problem(
                    title: localizer["RateLimit.TooManyRequests"],
                    statusCode: StatusCodes.Status429TooManyRequests,
                    type: "https://api.adeeb.tj/errors/rate-limit",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "rate_limit.too_many_requests",
                        ["traceId"] = context.HttpContext.TraceIdentifier
                    }).ExecuteAsync(context.HttpContext);
            };

            options.AddPolicy("auth-login", http => Fixed(http, "login", 5, TimeSpan.FromMinutes(1)));
            var registerPermits = environment.IsEnvironment("Testing") ? 100 : 3;
            options.AddPolicy("auth-register", http => Fixed(http, "register", registerPermits, TimeSpan.FromMinutes(5)));
            options.AddPolicy("auth-refresh", http => Fixed(http, "refresh", 20, TimeSpan.FromMinutes(1)));
            options.AddPolicy("auth-change-password", http => Fixed(http, "change-password", 3, TimeSpan.FromMinutes(10)));
            options.AddPolicy("student-provision", http => Fixed(http, "student-provision", 5, TimeSpan.FromMinutes(5)));
        });

        return services;
    }

    private static RateLimitPartition<string> Fixed(HttpContext http, string name, int permits, TimeSpan window)
    {
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var user = http.User.Identity?.IsAuthenticated == true
            ? http.User.FindFirst("sub")?.Value ?? "anonymous"
            : "anonymous";
        var partition = $"{name}:{ip}:{user}";
        return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permits,
            QueueLimit = 0,
            Window = window,
            AutoReplenishment = true
        });
    }
}
