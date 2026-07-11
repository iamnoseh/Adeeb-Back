using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Adeeb.Modules.Commerce.Infrastructure.Persistence;

public sealed class CommerceDbContextFactory : IDesignTimeDbContextFactory<CommerceDbContext>
{
    public CommerceDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ADEEB_COMMERCE_MIGRATIONS_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=adeeb_v2;Username=postgres";

        var options = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CommerceDbContext(options);
    }
}
