using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Adeeb.Api.Configuration;

public static class ProductionHttpExtensions
{
    private const string CorsPolicy = "adeeb-clients";

    public static IServiceCollection AddProductionHttp(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        foreach (var origin in origins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            {
                throw new InvalidOperationException($"Cors:AllowedOrigins contains invalid origin '{origin}'.");
            }
        }

        var allowedHosts = configuration["AllowedHosts"];
        if (environment.IsProduction() && string.Equals(allowedHosts?.Trim(), "*", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("AllowedHosts must be explicit in Production.");
        }

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
        {
            policy.WithOrigins(origins)
                .WithMethods("GET", "POST", "PUT", "DELETE")
                .WithHeaders("Authorization", "Content-Type", "X-Adeeb-Language", "X-Correlation-ID")
                .WithExposedHeaders("X-Correlation-ID")
                .SetPreflightMaxAge(TimeSpan.FromHours(1));
        }));
        return services;
    }

    public static IApplicationBuilder UseProductionHttp(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            const string header = "X-Correlation-ID";
            var incoming = context.Request.Headers[header].FirstOrDefault();
            var correlationId = IsSafeCorrelationId(incoming) ? incoming! : context.TraceIdentifier;
            context.TraceIdentifier = correlationId;
            context.Response.Headers[header] = correlationId;
            await next(context);
        });
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["X-Frame-Options"] = "DENY";

            var path = context.Request.Path.Value;
            var isSwagger = path != null && (
                path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase));

            if (isSwagger)
            {
                context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; frame-ancestors 'none'; base-uri 'none'";
            }
            else
            {
                context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
            }

            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            await next(context);
        });
        app.UseCors(CorsPolicy);
        return app;
    }

    internal static bool IsSafeCorrelationId(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 128 &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or ':');
}
