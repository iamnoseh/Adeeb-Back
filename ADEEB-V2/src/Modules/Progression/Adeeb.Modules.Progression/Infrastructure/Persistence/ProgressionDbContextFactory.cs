using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Adeeb.Modules.Progression.Infrastructure.Persistence;

public sealed class ProgressionDbContextFactory : IDesignTimeDbContextFactory<ProgressionDbContext>
{
    public ProgressionDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<ProgressionDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__Progression") ?? "Host=localhost;Database=adeeb;Username=postgres;Password=postgres").Options);
}
