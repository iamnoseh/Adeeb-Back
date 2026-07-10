namespace Adeeb.Api.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class DatabaseInitializationOptions
{
    public bool AutoMigrate { get; set; } = false;
    public bool Seed { get; set; } = false;
}

public static class DatabaseInitializationOptionsExtensions
{
    public static IServiceCollection AddDatabaseInitializationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseInitializationOptions>()
            .Bind(configuration.GetSection("DatabaseInitialization"))
            .ValidateOnStart();

        return services;
    }
}
