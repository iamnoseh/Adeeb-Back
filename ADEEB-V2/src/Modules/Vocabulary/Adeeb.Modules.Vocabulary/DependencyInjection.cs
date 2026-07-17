using Adeeb.Modules.Vocabulary.Application;
using Adeeb.Modules.Vocabulary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.Vocabulary;

public static class DependencyInjection
{
    public static IServiceCollection AddVocabularyModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Vocabulary") ?? configuration.GetConnectionString("Default") ?? configuration.GetConnectionString("Identity");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Vocabulary database connection string is required.");
        services.AddDbContext<VocabularyDbContext>(options => options.UseNpgsql(connection));
        services.AddScoped<VocabularyAdminService>(); services.AddScoped<VocabularyStudentService>();
        return services;
    }
}
