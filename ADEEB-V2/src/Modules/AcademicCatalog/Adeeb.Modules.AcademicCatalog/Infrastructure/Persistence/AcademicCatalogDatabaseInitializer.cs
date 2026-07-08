using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.AcademicCatalog.Infrastructure.Persistence;

public static class AcademicCatalogDatabaseInitializer
{
    public static async Task MigrateAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademicCatalogDbContext>();
        await db.Database.MigrateAsync();
    }
}
