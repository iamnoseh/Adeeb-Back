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
        services.AddOptions<MmtOptions>()
            .Bind(configuration.GetSection("Mmt"))
            .Validate(options => !options.CurrentAdmissionYear.HasValue || options.CurrentAdmissionYear is >= 2000 and <= 2100,
                "Mmt:CurrentAdmissionYear must be between 2000 and 2100.")
            .ValidateOnStart();
        services.AddScoped<MmtCatalogService>();
        services.AddScoped<AdmissionProgramService>();
        services.AddScoped<MmtSimulatorService>();
        services.AddScoped<MmtImportService>();
        services.AddSingleton<MmtSpreadsheet>();
        return services;
    }
}
