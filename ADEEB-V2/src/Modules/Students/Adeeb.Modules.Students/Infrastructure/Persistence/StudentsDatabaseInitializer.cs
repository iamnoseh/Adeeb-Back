using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.Students.Infrastructure.Persistence;

public static class StudentsDatabaseInitializer
{
    public static async Task MigrateAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StudentsDbContext>();
        await db.Database.MigrateAsync();
    }
}
