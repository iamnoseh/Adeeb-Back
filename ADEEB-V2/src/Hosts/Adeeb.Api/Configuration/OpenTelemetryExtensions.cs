using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Adeeb.Api.Configuration;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddAdeebOpenTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("Adeeb.Api"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());
                
        return services;
    }
}
