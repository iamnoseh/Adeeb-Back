using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.Commerce.Infrastructure.Persistence;

internal static class StudentEntitlementLock
{
    private const long LockNamespace = 0x4144454542;

    public static async Task AcquireAsync(
        CommerceDbContext db,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational())
        {
            return;
        }

        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("A database transaction is required before acquiring an entitlement lock.");
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({studentId.ToString("N")}, {LockNamespace}))",
            cancellationToken);
    }
}
