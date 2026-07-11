using Adeeb.Modules.Commerce.Application;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.Commerce;

public static class DependencyInjection
{
    public static IServiceCollection AddCommerceModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Commerce")
            ?? configuration.GetConnectionString("Default")
            ?? configuration.GetConnectionString("Identity");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Commerce database connection string is required.");
        }

        services.AddDbContext<CommerceDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<CommerceService>();
        return services;
    }
}
