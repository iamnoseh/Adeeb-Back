using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.Progression.Domain;

public enum LeagueDefinitionStatus { Active = 1, Archived = 2 }
public enum LeagueSeasonStatus { Active = 1, Closed = 2 }
public enum LeagueMovementOutcome { Stayed = 1, Promoted = 2, Relegated = 3, TopTier = 4, BottomTier = 5, Ineligible = 6 }

public sealed class LeagueDefinition : Entity
{
    public const int NameMaxLength = 100;
    public const int AvatarUrlMaxLength = 512;
    private LeagueDefinition() { }
    public LeagueDefinition(Guid id, string nameTg, string nameRu, string? avatarUrl, long minXpUnits,
        long? maxXpUnits, int displayOrder, int configurationVersion, DateTimeOffset now)
    {
        Id = id;
        Apply(nameTg, nameRu, avatarUrl, minXpUnits, maxXpUnits, displayOrder, configurationVersion, now);
        Status = LeagueDefinitionStatus.Active;
        CreatedAtUtc = now;
    }
    public string NameTg { get; private set; } = string.Empty;
    public string NameRu { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public long MinLifetimeXpUnits { get; private set; }
    public long? MaxLifetimeXpUnits { get; private set; }
    public int DisplayOrder { get; private set; }
    public LeagueDefinitionStatus Status { get; private set; }
    public int ConfigurationVersion { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void UpdateCosmetic(string nameTg, string nameRu, string? avatarUrl, DateTimeOffset now) =>
        Apply(nameTg, nameRu, avatarUrl, MinLifetimeXpUnits, MaxLifetimeXpUnits, DisplayOrder,
            ConfigurationVersion, now);
    public void UpdateStructural(string nameTg, string nameRu, string? avatarUrl, long minXpUnits,
        long? maxXpUnits, int displayOrder, int configurationVersion, DateTimeOffset now) =>
        Apply(nameTg, nameRu, avatarUrl, minXpUnits, maxXpUnits, displayOrder, configurationVersion, now);
    public void Archive(DateTimeOffset now) { Status = LeagueDefinitionStatus.Archived; UpdatedAtUtc = now; Version++; }
    private void Apply(string nameTg, string nameRu, string? avatarUrl, long minXpUnits, long? maxXpUnits,
        int displayOrder, int configurationVersion, DateTimeOffset now)
    {
        nameTg = nameTg.Trim(); nameRu = nameRu.Trim();
        if (nameTg.Length is 0 or > NameMaxLength || nameRu.Length is 0 or > NameMaxLength)
            throw new ArgumentException("League names are invalid.");
        if (avatarUrl?.Length > AvatarUrlMaxLength || minXpUnits < 0 || maxXpUnits <= minXpUnits
            || displayOrder < 1 || configurationVersion < 1) throw new ArgumentException("League definition is invalid.");
        NameTg = nameTg; NameRu = nameRu; AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        MinLifetimeXpUnits = minXpUnits; MaxLifetimeXpUnits = maxXpUnits; DisplayOrder = displayOrder;
        ConfigurationVersion = configurationVersion; UpdatedAtUtc = now; Version++;
    }
}

public sealed class LeagueSeason : Entity
{
    public const int DurationDays = 10;
    private LeagueSeason() { }
    public LeagueSeason(Guid id, int number, DateTimeOffset startsAtUtc, int configurationVersion,
        bool autoStartNext, DateTimeOffset now)
    {
        if (number < 1 || configurationVersion < 1) throw new ArgumentOutOfRangeException(nameof(number));
        Id = id; Number = number; StartsAtUtc = startsAtUtc; EndsAtUtc = startsAtUtc.AddDays(DurationDays);
        ConfigurationVersion = configurationVersion; AutoStartNext = autoStartNext;
        Status = LeagueSeasonStatus.Active; CreatedAtUtc = now;
    }
    public int Number { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }
    public LeagueSeasonStatus Status { get; private set; }
    public bool AutoStartNext { get; private set; }
    public int ConfigurationVersion { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public void SetAutoStartNext(bool enabled) => AutoStartNext = enabled;
    public void Close(DateTimeOffset now) { if (Status != LeagueSeasonStatus.Active) return; Status = LeagueSeasonStatus.Closed; ClosedAtUtc = now; }
}

public sealed class LeagueMembership : Entity
{
    private LeagueMembership() { }
    public LeagueMembership(Guid id, Guid seasonId, Guid leagueId, Guid userId, long initialLifetimeXpUnits,
        DateTimeOffset joinedAtUtc)
    { Id = id; SeasonId = seasonId; LeagueId = leagueId; UserId = userId; InitialLifetimeXpUnits = initialLifetimeXpUnits; JoinedAtUtc = joinedAtUtc; }
    public Guid SeasonId { get; private set; }
    public Guid LeagueId { get; private set; }
    public Guid UserId { get; private set; }
    public long InitialLifetimeXpUnits { get; private set; }
    public long SeasonScoreUnits { get; private set; }
    public DateTimeOffset JoinedAtUtc { get; private set; }
    public DateTimeOffset? LastScoreAtUtc { get; private set; }
    public int? FinalRank { get; private set; }
    public LeagueMovementOutcome? Outcome { get; private set; }
    public void AddScore(int units, DateTimeOffset at) { if (units <= 0) return; SeasonScoreUnits = checked(SeasonScoreUnits + units); LastScoreAtUtc = at; }
    public void Reconcile(long units, DateTimeOffset? lastAt) { SeasonScoreUnits = Math.Max(0, units); LastScoreAtUtc = lastAt; }
    public void Finalize(int rank, LeagueMovementOutcome outcome) { FinalRank = rank; Outcome = outcome; }
}

public sealed class LeagueScoreEvent
{
    private LeagueScoreEvent() { }
    public LeagueScoreEvent(Guid ledgerEntryId, Guid membershipId, int amountUnits, DateTimeOffset occurredAtUtc)
    { LedgerEntryId = ledgerEntryId; MembershipId = membershipId; AmountUnits = amountUnits; OccurredAtUtc = occurredAtUtc; }
    public Guid LedgerEntryId { get; private set; }
    public Guid MembershipId { get; private set; }
    public int AmountUnits { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
}

public sealed class LeagueMovementResult : Entity
{
    private LeagueMovementResult() { }
    public LeagueMovementResult(Guid id, Guid seasonId, Guid userId, Guid fromLeagueId, Guid toLeagueId,
        int finalRank, LeagueMovementOutcome outcome, DateTimeOffset now)
    { Id = id; SeasonId = seasonId; UserId = userId; FromLeagueId = fromLeagueId; ToLeagueId = toLeagueId; FinalRank = finalRank; Outcome = outcome; CreatedAtUtc = now; }
    public Guid SeasonId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid FromLeagueId { get; private set; }
    public Guid ToLeagueId { get; private set; }
    public int FinalRank { get; private set; }
    public LeagueMovementOutcome Outcome { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
