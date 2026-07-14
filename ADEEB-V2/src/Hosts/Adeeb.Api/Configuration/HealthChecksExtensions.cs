using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Api.Configuration;

public static class HealthChecksExtensions
{
    public static IServiceCollection AddAdeebHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(RequiredConnectionString(configuration, "Identity"), name: "identity-db", tags: ["db", "ready"])
            .AddNpgSql(RequiredConnectionString(configuration, "AcademicCatalog"), name: "academic-db", tags: ["db", "ready"])
            .AddNpgSql(RequiredConnectionString(configuration, "QuestionBank"), name: "question-db", tags: ["db", "ready"])
            .AddNpgSql(RequiredConnectionString(configuration, "Students"), name: "students-db", tags: ["db", "ready"])
            .AddNpgSql(RequiredConnectionString(configuration, "Commerce"), name: "commerce-db", tags: ["db", "ready"])
            .AddNpgSql(RequiredConnectionString(configuration, "Mmt"), name: "mmt-db", tags: ["db", "ready"]);

        return services;
    }

    private static string RequiredConnectionString(IConfiguration configuration, string name)
    {
        var value = configuration.GetConnectionString(name)
            ?? configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{name} database connection string is required.");
        }

        return value;
    }
}
