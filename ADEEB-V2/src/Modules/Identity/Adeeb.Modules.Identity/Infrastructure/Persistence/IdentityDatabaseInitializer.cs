using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Adeeb.Modules.Identity.Infrastructure.Persistence;

public static class IdentityDatabaseInitializer
{
    public static async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Adeeb.IdentityDatabase");

        logger.LogInformation("identity.database.migration.started");
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("identity.database.migration.completed");
    }
}
