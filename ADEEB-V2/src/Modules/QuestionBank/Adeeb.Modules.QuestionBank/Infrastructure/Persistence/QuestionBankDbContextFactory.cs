using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence;

public sealed class QuestionBankDbContextFactory : IDesignTimeDbContextFactory<QuestionBankDbContext>
{
    public QuestionBankDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ADEEB_QUESTION_BANK_MIGRATIONS_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=adeeb_v2;Username=postgres";
        var options = new DbContextOptionsBuilder<QuestionBankDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new QuestionBankDbContext(options);
    }
}
