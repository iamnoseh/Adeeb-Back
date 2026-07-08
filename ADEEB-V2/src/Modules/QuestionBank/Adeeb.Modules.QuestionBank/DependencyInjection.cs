using Adeeb.Modules.QuestionBank.Application;
using Adeeb.Modules.QuestionBank.Infrastructure.Files;
using Adeeb.Modules.QuestionBank.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.QuestionBank;

public static class DependencyInjection
{
    public static IServiceCollection AddQuestionBankModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("QuestionBank")
            ?? configuration.GetConnectionString("Default")
            ?? configuration.GetConnectionString("Identity")
            ?? throw new InvalidOperationException("QuestionBank database connection string is required.");

        services.AddDbContext<QuestionBankDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<QuestionBankService>();
        services.AddScoped<QuestionImageStorage>();
        return services;
    }
}
