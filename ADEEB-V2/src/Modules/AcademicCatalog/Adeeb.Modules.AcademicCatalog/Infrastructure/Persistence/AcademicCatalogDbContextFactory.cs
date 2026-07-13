using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Adeeb.Modules.AcademicCatalog.Infrastructure.Persistence;

public sealed class AcademicCatalogDbContextFactory : IDesignTimeDbContextFactory<AcademicCatalogDbContext>
{
    public AcademicCatalogDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ADEEB_ACADEMIC_MIGRATIONS_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=adeeb_v2;Username=postgres";
        var options = new DbContextOptionsBuilder<AcademicCatalogDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AcademicCatalogDbContext(options);
    }
}
