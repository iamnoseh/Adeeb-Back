using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence;

public static class QuestionBankDatabaseInitializer
{
    public static async Task MigrateAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuestionBankDbContext>();
        await db.Database.MigrateAsync();
    }
}
