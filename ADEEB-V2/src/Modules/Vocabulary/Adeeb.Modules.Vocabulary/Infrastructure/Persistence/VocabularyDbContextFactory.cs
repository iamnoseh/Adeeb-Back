using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Adeeb.Modules.Vocabulary.Infrastructure.Persistence;

public sealed class VocabularyDbContextFactory : IDesignTimeDbContextFactory<VocabularyDbContext>
{
    public VocabularyDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Vocabulary")
            ?? "Host=localhost;Port=5432;Database=adeeb;Username=postgres;Password=postgres";
        return new VocabularyDbContext(new DbContextOptionsBuilder<VocabularyDbContext>().UseNpgsql(connection).Options);
    }
}
