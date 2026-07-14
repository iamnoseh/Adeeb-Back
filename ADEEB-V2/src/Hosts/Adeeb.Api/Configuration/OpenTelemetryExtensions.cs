using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Adeeb.Api.Configuration;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddAdeebOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var version = typeof(OpenTelemetryExtensions).Assembly.GetName().Version?.ToString() ?? "unknown";
        Uri? endpointUri = null;
        var endpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        if (!string.IsNullOrWhiteSpace(endpoint) && !Uri.TryCreate(endpoint, UriKind.Absolute, out endpointUri))
        {
            throw new InvalidOperationException("OpenTelemetry:OtlpEndpoint must be an absolute URI.");
        }

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: "Adeeb.Api",
                serviceVersion: version,
                serviceInstanceId: Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment.name"] = environment.EnvironmentName
                }))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("Adeeb.Commerce");
                if (endpointUri is not null)
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = endpointUri);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Adeeb.Commerce");
                if (endpointUri is not null)
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = endpointUri);
                }
            });

        return services;
    }
}
