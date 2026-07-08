using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Infrastructure.Localization;
using Adeeb.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAdeebInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IMessageLocalizer, StaticMessageLocalizer>();
        return services;
    }
}
