using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Api.Configuration;

public static class HealthChecksExtensions
{
    public static IServiceCollection AddAdeebHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Identity") ?? string.Empty, name: "identity-db", tags: ["db", "ready"])
            .AddNpgSql(configuration.GetConnectionString("AcademicCatalog") ?? string.Empty, name: "academic-db", tags: ["db", "ready"])
            .AddNpgSql(configuration.GetConnectionString("QuestionBank") ?? string.Empty, name: "question-db", tags: ["db", "ready"]);

        return services;
    }
}
