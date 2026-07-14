using Adeeb.Modules.Mmt.Application;
using Adeeb.Modules.Mmt.Application.Import;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.Mmt;

public static class DependencyInjection
{
    public static IServiceCollection AddMmtModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Mmt") ?? configuration.GetConnectionString("Default") ?? configuration.GetConnectionString("Identity");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Mmt database connection string is required.");
        services.AddDbContext<MmtDbContext>(options => options.UseNpgsql(connection));
        services.AddScoped<MmtCatalogService>();
        services.AddScoped<AdmissionProgramService>();
        services.AddScoped<MmtImportService>();
        services.AddSingleton<MmtSpreadsheet>();
        return services;
    }
}
