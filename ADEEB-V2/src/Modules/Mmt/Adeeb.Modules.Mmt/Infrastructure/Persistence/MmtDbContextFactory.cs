using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence;

public sealed class MmtDbContextFactory : IDesignTimeDbContextFactory<MmtDbContext>
{
    public MmtDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ADEEB_MMT_MIGRATIONS_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=adeeb_v2;Username=postgres";
        return new MmtDbContext(new DbContextOptionsBuilder<MmtDbContext>().UseNpgsql(connection).Options);
    }
}
