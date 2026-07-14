using System.Threading.RateLimiting;
using Adeeb.Application.Abstractions.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Adeeb.Api.Configuration;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddAdeebRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var settings = configuration.GetSection("RateLimits").Get<RateLimitSettings>() ?? new();
        settings.Validate();
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                RateLimitPartition.GetConcurrencyLimiter("global", _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = settings.GlobalConcurrency,
                    QueueLimit = 0
                }));
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
            options.AddPolicy("commerce-receipt-upload", http => Fixed(
                http,
                "commerce-receipt-upload",
                settings.ReceiptUploadPermits,
                TimeSpan.FromMinutes(settings.ReceiptUploadWindowMinutes)));
            options.AddPolicy("commerce-receipt-review", http => Fixed(
                http,
                "commerce-receipt-review",
                settings.ReceiptReviewPermits,
                TimeSpan.FromMinutes(settings.ReceiptReviewWindowMinutes)));
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

    private sealed class RateLimitSettings
    {
        public int GlobalConcurrency { get; init; } = 200;
        public int ReceiptUploadPermits { get; init; } = 5;
        public int ReceiptUploadWindowMinutes { get; init; } = 10;
        public int ReceiptReviewPermits { get; init; } = 30;
        public int ReceiptReviewWindowMinutes { get; init; } = 1;

        public void Validate()
        {
            if (GlobalConcurrency <= 0 ||
                ReceiptUploadPermits <= 0 ||
                ReceiptUploadWindowMinutes <= 0 ||
                ReceiptReviewPermits <= 0 ||
                ReceiptReviewWindowMinutes <= 0)
            {
                throw new InvalidOperationException("RateLimits values must be positive.");
            }
        }
    }
}
