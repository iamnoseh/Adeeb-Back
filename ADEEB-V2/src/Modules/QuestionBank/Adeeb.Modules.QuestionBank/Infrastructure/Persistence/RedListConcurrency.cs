using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence;

internal static class RedListConcurrency
{
    public static async Task AcquireAsync(QuestionBankDbContext db, Guid userId, Guid questionId,
        CancellationToken cancellationToken)
    {
        if (!db.Database.IsNpgsql()) return;
        var key = $"{userId:N}:{questionId:N}";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0));", cancellationToken);
    }
}
