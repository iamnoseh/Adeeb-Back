using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.Vocabulary.Infrastructure.Persistence;

public static class VocabularyDatabaseInitializer
{
    public static async Task MigrateAsync(IServiceProvider services)
    { using var scope = services.CreateScope(); await scope.ServiceProvider.GetRequiredService<VocabularyDbContext>().Database.MigrateAsync(); }
}
