using Adeeb.Modules.AcademicCatalog.Application;
using Adeeb.Modules.AcademicCatalog.Contracts;
using Adeeb.Modules.AcademicCatalog.Infrastructure.Files;
using Adeeb.Modules.AcademicCatalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.AcademicCatalog;

public static class DependencyInjection
{
    public static IServiceCollection AddAcademicCatalogModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AcademicCatalog")
            ?? configuration.GetConnectionString("Default")
            ?? configuration.GetConnectionString("Identity")
            ?? throw new InvalidOperationException("AcademicCatalog database connection string is required.");

        services.AddDbContext<AcademicCatalogDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<AcademicCatalogService>();
        services.AddScoped<IAcademicCatalogLookup>(sp => sp.GetRequiredService<AcademicCatalogService>());
        services.AddScoped<AcademicFileStorage>();
        return services;
    }
}
