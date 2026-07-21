using Adeeb.Application.Abstractions.Students;
using Adeeb.Modules.Students.Application;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Students.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.Students;

public static class DependencyInjection
{
    public static IServiceCollection AddStudentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Students")
            ?? configuration.GetConnectionString("Default")
            ?? configuration.GetConnectionString("Identity");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Students database connection string is required.");
        }

        services.AddDbContext<StudentsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<StudentsService>();
        services.AddScoped<StudentActivityService>();
        services.AddScoped<IStudentLookup>(sp => sp.GetRequiredService<StudentsService>());
        services.AddScoped<IStudentCompetitionDirectory>(sp => sp.GetRequiredService<StudentsService>());
        services.AddScoped<IStudentRegistrationProvisioner>(sp => sp.GetRequiredService<StudentsService>());
        return services;
    }
}
