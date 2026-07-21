using Adeeb.Application.Abstractions.Progression;
using Adeeb.Modules.Progression.Application;
using Adeeb.Modules.Progression.Infrastructure.Files;
using Adeeb.Modules.Progression.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.Progression;

public static class DependencyInjection
{
    public static IServiceCollection AddProgressionModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = new[] { configuration.GetConnectionString("Progression"), configuration.GetConnectionString("Default"), configuration.GetConnectionString("Identity") }
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Progression database connection string is required.");
        services.AddDbContext<ProgressionDbContext>(options => options.UseNpgsql(connection));
        services.AddScoped<ProgressionService>();
        services.AddScoped<IXpGrantedIntegrationHandler>(sp => sp.GetRequiredService<ProgressionService>());
        services.AddScoped<LeagueAvatarStorage>();
        services.AddHostedService<ProgressionSeasonWorker>();
        return services;
    }
}
