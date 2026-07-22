using Adeeb.Application.Abstractions.Progression;
using Adeeb.SharedKernel.Progression;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence;

internal sealed class StudentXpReadService(QuestionBankDbContext db) : IStudentXpReadService
{
    public async Task<StudentXpRankSnapshot> GetRankAsync(Guid userId, CancellationToken ct)
    {
        var values = await db.StudentXpBalances.AsNoTracking().Where(x => x.TotalXpUnits > 0)
            .OrderByDescending(x => x.TotalXpUnits).ThenBy(x => x.UpdatedAtUtc).ThenBy(x => x.UserId).ToListAsync(ct);
        var index = values.FindIndex(x => x.UserId == userId);
        return index < 0 ? new(0, null, values.Count, null)
            : new(values[index].TotalXpUnits, index + 1, values.Count, values[index].UpdatedAtUtc);
    }
    public async Task<IReadOnlyList<StudentXpBalanceSnapshot>> GetPositiveBalancesAsync(CancellationToken ct) =>
        await db.StudentXpBalances.AsNoTracking().Where(x => x.TotalXpUnits > 0)
            .Select(x => new StudentXpBalanceSnapshot(x.UserId, x.TotalXpUnits, x.UpdatedAtUtc)).ToListAsync(ct);
    public async Task<IReadOnlyList<StudentSeasonXpAggregate>> GetSeasonAggregatesAsync(DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc, CancellationToken ct) => await db.XpLedgerEntries.AsNoTracking()
        .Where(x => x.CreatedAtUtc >= startsAtUtc && x.CreatedAtUtc < endsAtUtc && x.EntryType == XpEntryType.Credit
            && x.SourceType != XpSourceType.AdminAdjustment)
        .GroupBy(x => x.UserId).Select(x => new StudentSeasonXpAggregate(x.Key, x.Sum(y => (long)y.AmountUnits),
            x.Max(y => (DateTimeOffset?)y.CreatedAtUtc))).ToListAsync(ct);
}
