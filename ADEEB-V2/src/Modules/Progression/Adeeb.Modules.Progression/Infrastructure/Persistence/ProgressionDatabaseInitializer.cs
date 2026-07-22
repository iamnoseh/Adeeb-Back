using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.Progression.Infrastructure.Persistence;

public static class ProgressionDatabaseInitializer
{
    public static async Task MigrateAsync(IServiceProvider services)
    { using var scope = services.CreateScope(); await scope.ServiceProvider.GetRequiredService<ProgressionDbContext>().Database.MigrateAsync(); }
}
