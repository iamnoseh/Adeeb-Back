using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence;

public static class MmtDatabaseInitializer
{
    public static async Task MigrateAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MmtDbContext>().Database.MigrateAsync(ct);
    }
}
