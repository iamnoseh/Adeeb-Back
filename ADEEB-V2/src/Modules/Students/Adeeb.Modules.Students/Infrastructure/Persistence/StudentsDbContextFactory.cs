using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Adeeb.Modules.Students.Infrastructure.Persistence;

public sealed class StudentsDbContextFactory : IDesignTimeDbContextFactory<StudentsDbContext>
{
    public StudentsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ADEEB_STUDENTS_MIGRATIONS_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=adeeb_v2;Username=postgres";

        var options = new DbContextOptionsBuilder<StudentsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new StudentsDbContext(options);
    }
}
