using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Progression;
using Adeeb.SharedKernel.Results;

namespace Adeeb.Application.Abstractions.Progression;

public sealed record XpGrantRequest(
    Guid UserId,
    XpSourceType SourceType,
    string SourceId,
    int AmountUnits,
    string IdempotencyKey,
    XpEntryType EntryType,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record XpGrantResult(
    Guid LedgerEntryId,
    long PreviousBalanceUnits,
    long NewBalanceUnits,
    int GrantedUnits,
    bool WasAlreadyProcessed);

public interface IStudentXpService
{
    Task<Result<XpGrantResult>> GrantAsync(XpGrantRequest request, CancellationToken cancellationToken);
}

public sealed record XpGrantedIntegrationEvent(
    Guid LedgerEntryId,
    Guid UserId,
    XpSourceType SourceType,
    XpEntryType EntryType,
    int AmountUnits,
    long NewBalanceUnits,
    DateTimeOffset CreatedAtUtc);

public sealed record StudentXpRankSnapshot(long TotalXpUnits, long? GlobalRank, long RankedStudentsCount,
    DateTimeOffset? UpdatedAtUtc);

public sealed record StudentXpBalanceSnapshot(Guid UserId, long TotalXpUnits, DateTimeOffset UpdatedAtUtc);

public sealed record StudentSeasonXpAggregate(Guid UserId, long TotalXpUnits, DateTimeOffset? LastEarnedAtUtc);

public interface IXpGrantedIntegrationHandler
{
    Task HandleAsync(XpGrantedIntegrationEvent message, CancellationToken cancellationToken);
}

public interface IStudentXpReadService
{
    Task<StudentXpRankSnapshot> GetRankAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudentXpBalanceSnapshot>> GetPositiveBalancesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<StudentSeasonXpAggregate>> GetSeasonAggregatesAsync(DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc, CancellationToken cancellationToken);
}

public static class XpErrors
{
    public static readonly Error InvalidAmount = Error.Validation("xp.invalid_amount", "XP.InvalidAmount");
    public static readonly Error InvalidSource = Error.Validation("xp.invalid_source", "XP.InvalidSource");
    public static readonly Error DuplicateEntry = Error.Conflict("xp.duplicate_entry", "XP.DuplicateEntry");
    public static readonly Error BalanceOverflow = Error.Conflict("xp.balance_overflow", "XP.BalanceOverflow");
    public static readonly Error PersistenceFailed = Error.Conflict("xp.persistence_failed", "XP.PersistenceFailed");
    public static readonly Error SourceConflict = Error.Conflict("xp.source_conflict", "XP.SourceConflict");
}
