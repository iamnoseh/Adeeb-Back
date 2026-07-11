using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.Commerce.Infrastructure.Persistence;

public static class CommerceDatabaseInitializer
{
    public static async Task MigrateAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        await db.Database.MigrateAsync();
    }
}
