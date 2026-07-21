using System.Globalization;
using System.Security.Claims;
using Adeeb.Application.Abstractions.Progression;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Progression.Contracts;
using Adeeb.Modules.Progression.Domain;
using Adeeb.Modules.Progression.Infrastructure.Persistence;
using Adeeb.Modules.Students.Contracts;
using Adeeb.SharedKernel.Progression;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.Progression.Application;

public sealed class ProgressionService(ProgressionDbContext db, IStudentXpReadService xp,
    IStudentCompetitionDirectory students, IDateTimeProvider clock) : IXpGrantedIntegrationHandler
{
    public async Task<IReadOnlyList<LeagueDto>> GetLeaguesAsync(CancellationToken ct)
    {
        var locked = await HasActiveSeasonAsync(ct);
        var values = await db.Leagues.AsNoTracking()
            .Where(x => x.Status == LeagueDefinitionStatus.Active)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(ct);
        return values.Select(x => ToDto(x, locked)).ToList();
    }

    public async Task<Result<LeagueDto>> CreateLeagueAsync(LeagueFormRequest request, string? avatarUrl, CancellationToken ct)
    {
        if (await HasActiveSeasonAsync(ct)) return Result<LeagueDto>.Failure(ProgressionErrors.StructuralLocked);
        var values = await db.Leagues.Where(x => x.Status == LeagueDefinitionStatus.Active).ToListAsync(ct);
        var version = values.Select(x => x.ConfigurationVersion).DefaultIfEmpty(0).Max() + 1;
        var candidate = BuildCandidate(null, request, avatarUrl, version);
        if (candidate is null || !request.IsActive) return Result<LeagueDto>.Failure(ProgressionErrors.InvalidLeague);
        values.Add(candidate);
        if (!ValidDraftRanges(values)) return Result<LeagueDto>.Failure(ProgressionErrors.InvalidThresholds);
        db.Leagues.Add(candidate); await db.SaveChangesAsync(ct);
        return Result<LeagueDto>.Success(ToDto(candidate, false));
    }

    public async Task<Result<LeagueDto>> UpdateLeagueAsync(Guid id, LeagueFormRequest request, string? avatarUrl,
        CancellationToken ct)
    {
        var entity = await db.Leagues.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return Result<LeagueDto>.Failure(ProgressionErrors.NotFound);
        var active = await HasActiveSeasonAsync(ct);
        var effectiveAvatar = request.RemoveAvatar ? null : avatarUrl ?? entity.AvatarUrl;
        try
        {
            if (active)
            {
                var structuralChanged = ToUnits(request.MinXp) != entity.MinLifetimeXpUnits
                    || ToNullableUnits(request.MaxXp) != entity.MaxLifetimeXpUnits
                    || request.DisplayOrder != entity.DisplayOrder || !request.IsActive;
                if (structuralChanged) return Result<LeagueDto>.Failure(ProgressionErrors.StructuralLocked);
                entity.UpdateCosmetic(request.NameTg ?? string.Empty, request.NameRu ?? string.Empty, effectiveAvatar, clock.UtcNow);
            }
            else
            {
                var all = await db.Leagues.Where(x => x.Status == LeagueDefinitionStatus.Active && x.Id != id).ToListAsync(ct);
                var version = all.Select(x => x.ConfigurationVersion).Append(entity.ConfigurationVersion).Max() + 1;
                entity.UpdateStructural(request.NameTg ?? string.Empty, request.NameRu ?? string.Empty, effectiveAvatar,
                    ToUnits(request.MinXp), ToNullableUnits(request.MaxXp), request.DisplayOrder ?? 0, version, clock.UtcNow);
                if (!request.IsActive) entity.Archive(clock.UtcNow); else all.Add(entity);
                if (!ValidDraftRanges(all)) return Result<LeagueDto>.Failure(ProgressionErrors.InvalidThresholds);
            }
            await db.SaveChangesAsync(ct);
            return Result<LeagueDto>.Success(ToDto(entity, active));
        }
        catch (ArgumentException) { return Result<LeagueDto>.Failure(ProgressionErrors.InvalidLeague); }
        catch (DbUpdateConcurrencyException) { return Result<LeagueDto>.Failure(ProgressionErrors.StructuralLocked); }
    }

    public async Task<Result> ArchiveLeagueAsync(Guid id, CancellationToken ct)
    {
        if (await HasActiveSeasonAsync(ct)) return Result.Failure(ProgressionErrors.StructuralLocked);
        var entity = await db.Leagues.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return Result.Failure(ProgressionErrors.NotFound);
        entity.Archive(clock.UtcNow);
        var remaining = await db.Leagues.Where(x => x.Status == LeagueDefinitionStatus.Active && x.Id != id).ToListAsync(ct);
        if (!ValidDraftRanges(remaining)) return Result.Failure(ProgressionErrors.InvalidThresholds);
        await db.SaveChangesAsync(ct); return Result.Success();
    }

    public async Task<Result<SeasonDto>> StartSeasonAsync(CancellationToken ct)
    {
        if (await HasActiveSeasonAsync(ct)) return Result<SeasonDto>.Failure(ProgressionErrors.SeasonActive);
        var leagues = await ActiveLeaguesAsync(ct);
        if (leagues.Count < 2 || !ValidRanges(leagues)) return Result<SeasonDto>.Failure(ProgressionErrors.InvalidThresholds);
        var number = await db.Seasons.Select(x => (int?)x.Number).MaxAsync(ct) ?? 0;
        var configurationVersion = leagues.Max(x => x.ConfigurationVersion);
        var season = new LeagueSeason(Guid.NewGuid(), number + 1, clock.UtcNow, configurationVersion, true, clock.UtcNow);
        db.Seasons.Add(season);
        await SeedFromLifetimeAsync(season, leagues, ct);
        await db.SaveChangesAsync(ct);
        return Result<SeasonDto>.Success(ToDto(season));
    }

    public async Task<Result<SeasonDto>> SetAutoRenewalAsync(bool enabled, CancellationToken ct)
    {
        var season = await db.Seasons.SingleOrDefaultAsync(x => x.Status == LeagueSeasonStatus.Active, ct);
        if (season is null) return Result<SeasonDto>.Failure(ProgressionErrors.SeasonUnavailable);
        season.SetAutoStartNext(enabled); await db.SaveChangesAsync(ct); return Result<SeasonDto>.Success(ToDto(season));
    }

    public async Task<SeasonDto?> GetCurrentSeasonAsync(CancellationToken ct)
    {
        var value = await db.Seasons.AsNoTracking().SingleOrDefaultAsync(x => x.Status == LeagueSeasonStatus.Active, ct);
        return value is null ? null : ToDto(value);
    }

    public async Task<PagedProgressionDto<SeasonDto>> GetSeasonHistoryAsync(int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = Page(page, pageSize, 10);
        var query = db.Seasons.AsNoTracking().Where(x => x.Status == LeagueSeasonStatus.Closed).OrderByDescending(x => x.Number);
        var count = await query.CountAsync(ct); var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(items.Select(ToDto).ToList(), page, pageSize, count);
    }

    public async Task<Result<ProgressionOverviewDto>> GetOverviewAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var userId = UserId(principal); if (userId is null) return Result<ProgressionOverviewDto>.Failure(ProgressionErrors.StudentUnavailable);
        var directory = await students.GetByIdentityUserIdsAsync([userId.Value], ct);
        if (!directory.TryGetValue(userId.Value, out var student) || !student.IsActive)
            return Result<ProgressionOverviewDto>.Failure(ProgressionErrors.StudentUnavailable);
        var rank = await xp.GetRankAsync(userId.Value, ct);
        var season = await db.Seasons.AsNoTracking().SingleOrDefaultAsync(x => x.Status == LeagueSeasonStatus.Active, ct);
        LeagueMembership? membership = null; LeagueDefinition? league = null; int? seasonRank = null; var participants = 0; var movement = 0; var zone = "unranked";
        if (season is not null)
        {
            membership = await db.Memberships.AsNoTracking().SingleOrDefaultAsync(x => x.SeasonId == season.Id && x.UserId == userId, ct);
            if (membership is not null)
            {
                league = await db.Leagues.AsNoTracking().SingleAsync(x => x.Id == membership.LeagueId, ct);
                var ranked = await RankedActiveMembershipsAsync(season.Id, league.Id, ct);
                participants = ranked.Count; movement = MovementCount(participants);
                seasonRank = ranked.FindIndex(x => x.Id == membership.Id) + 1;
                zone = Zone(seasonRank.Value, participants, movement, league.DisplayOrder,
                    (await ActiveLeaguesAsync(ct)).Count);
            }
        }
        var previous = await PreviousAsync(userId.Value, ct);
        return Result<ProgressionOverviewDto>.Success(new(ToXp(rank.TotalXpUnits), rank.GlobalRank, rank.RankedStudentsCount,
            league is null ? null : ToDto(league, true), ToXp(membership?.SeasonScoreUnits ?? 0), seasonRank,
            participants, zone, movement, clock.UtcNow, season?.StartsAtUtc, season?.EndsAtUtc, previous));
    }

    public async Task<Result<LeaderboardDto>> GetStudentLeaderboardAsync(ClaimsPrincipal principal, Guid? leagueId, int page,
        int pageSize, CancellationToken ct)
    {
        var userId = UserId(principal); if (userId is null) return Result<LeaderboardDto>.Failure(ProgressionErrors.StudentUnavailable);
        var directory = await students.GetByIdentityUserIdsAsync([userId.Value], ct);
        if (!directory.TryGetValue(userId.Value, out var student) || !student.IsActive)
            return Result<LeaderboardDto>.Failure(ProgressionErrors.StudentUnavailable);
        var season = await db.Seasons.AsNoTracking().SingleOrDefaultAsync(x => x.Status == LeagueSeasonStatus.Active, ct);
        if (season is null) return Result<LeaderboardDto>.Failure(ProgressionErrors.SeasonUnavailable);
        var targetLeagueId = leagueId;
        if (targetLeagueId == null)
        {
            var membership = await db.Memberships.AsNoTracking().SingleOrDefaultAsync(x => x.SeasonId == season.Id && x.UserId == userId, ct);
            if (membership is null) return Result<LeaderboardDto>.Failure(ProgressionErrors.SeasonUnavailable);
            targetLeagueId = membership.LeagueId;
        }
        return await BuildLeaderboardAsync(season, targetLeagueId.Value, userId, page, pageSize, ct);
    }

    public async Task<Result<LeaderboardDto>> GetAdminLeaderboardAsync(Guid leagueId, int page, int pageSize,
        CancellationToken ct)
    {
        var season = await db.Seasons.AsNoTracking().SingleOrDefaultAsync(x => x.Status == LeagueSeasonStatus.Active, ct);
        if (season is null) return Result<LeaderboardDto>.Failure(ProgressionErrors.SeasonUnavailable);
        return await BuildLeaderboardAsync(season, leagueId, null, page, pageSize, ct);
    }

    public async Task<PagedProgressionDto<PreviousSeasonDto>> GetStudentHistoryAsync(ClaimsPrincipal principal,
        int page, int pageSize, CancellationToken ct)
    {
        var userId = UserId(principal); (page, pageSize) = Page(page, pageSize, 10);
        if (userId is null) return new([], page, pageSize, 0);
        var query = db.MovementResults.AsNoTracking().Where(x => x.UserId == userId);
        var count = await query.CountAsync(ct);
        var rows = await query.Join(db.Seasons, x => x.SeasonId, x => x.Id, (x, season) => new { x, season.Number })
            .OrderByDescending(x => x.Number).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(rows.Select(x => new PreviousSeasonDto(x.Number, x.x.FinalRank, x.x.Outcome.ToString(), x.x.FromLeagueId)).ToList(), page, pageSize, count);
    }

    public async Task HandleAsync(XpGrantedIntegrationEvent message, CancellationToken ct)
    {
        if (!CountsForLeague(message)) return;
        if (db.Database.IsNpgsql())
        {
            await HandlePostgresEventAsync(message, ct);
            return;
        }
        if (await db.ScoreEvents.AnyAsync(x => x.LedgerEntryId == message.LedgerEntryId, ct)) return;
        var season = await db.Seasons.SingleOrDefaultAsync(x => x.Status == LeagueSeasonStatus.Active
            && x.StartsAtUtc <= message.CreatedAtUtc && x.EndsAtUtc > message.CreatedAtUtc, ct);
        if (season is null) return;
        var member = await db.Memberships.SingleOrDefaultAsync(x => x.SeasonId == season.Id && x.UserId == message.UserId, ct);
        if (member is null)
        {
            var refs = await students.GetByIdentityUserIdsAsync([message.UserId], ct);
            if (!refs.TryGetValue(message.UserId, out var student) || !student.IsActive) return;
            var leagues = await ActiveLeaguesAsync(ct);
            var league = FindLeague(leagues, message.NewBalanceUnits); if (league is null) return;
            member = new(Guid.NewGuid(), season.Id, league.Id, message.UserId, message.NewBalanceUnits, message.CreatedAtUtc);
            db.Memberships.Add(member);
        }
        member.AddScore(message.AmountUnits, message.CreatedAtUtc);
        db.ScoreEvents.Add(new(message.LedgerEntryId, member.Id, message.AmountUnits, message.CreatedAtUtc));
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { db.ChangeTracker.Clear(); }
    }

    private async Task HandlePostgresEventAsync(XpGrantedIntegrationEvent message, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        var season = await db.Seasons.AsNoTracking().SingleOrDefaultAsync(x => x.Status == LeagueSeasonStatus.Active
            && x.StartsAtUtc <= message.CreatedAtUtc && x.EndsAtUtc > message.CreatedAtUtc, ct);
        if (season is null) return;

        var membership = await db.Memberships.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SeasonId == season.Id && x.UserId == message.UserId, ct);
        if (membership is null)
        {
            var refs = await students.GetByIdentityUserIdsAsync([message.UserId], ct);
            if (!refs.TryGetValue(message.UserId, out var student) || !student.IsActive) return;
            var league = FindLeague(await ActiveLeaguesAsync(ct), message.NewBalanceUnits);
            if (league is null) return;
            var membershipId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO progression.league_memberships
                    (id, season_id, league_id, user_id, initial_lifetime_xp_units, season_score_units,
                     joined_at_utc, last_score_at_utc, final_rank, outcome)
                VALUES ({membershipId}, {season.Id}, {league.Id}, {message.UserId}, {message.NewBalanceUnits},
                        0, {message.CreatedAtUtc}, NULL, NULL, NULL)
                ON CONFLICT (season_id, user_id) DO NOTHING;
                """, ct);
            membership = await db.Memberships.AsNoTracking()
                .SingleAsync(x => x.SeasonId == season.Id && x.UserId == message.UserId, ct);
        }

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            WITH accepted AS (
                INSERT INTO progression.league_score_events
                    (ledger_entry_id, membership_id, amount_units, occurred_at_utc)
                VALUES ({message.LedgerEntryId}, {membership.Id}, {message.AmountUnits}, {message.CreatedAtUtc})
                ON CONFLICT (ledger_entry_id) DO NOTHING
                RETURNING membership_id
            )
            UPDATE progression.league_memberships AS membership
            SET season_score_units = membership.season_score_units + {message.AmountUnits},
                last_score_at_utc = GREATEST(COALESCE(membership.last_score_at_utc, {message.CreatedAtUtc}), {message.CreatedAtUtc})
            FROM accepted
            WHERE membership.id = accepted.membership_id;
            """, ct);
        await transaction.CommitAsync(ct);
    }

    public async Task ProcessExpiredSeasonAsync(CancellationToken ct)
    {
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct)
            : null;
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(70422101);", ct);
        var season = await db.Seasons.SingleOrDefaultAsync(x => x.Status == LeagueSeasonStatus.Active && x.EndsAtUtc <= clock.UtcNow, ct);
        if (season is null) return;
        var aggregates = (await xp.GetSeasonAggregatesAsync(season.StartsAtUtc, season.EndsAtUtc, ct)).ToDictionary(x => x.UserId);
        var members = await db.Memberships.Where(x => x.SeasonId == season.Id).ToListAsync(ct);
        var leagues = await ActiveLeaguesAsync(ct);
        var balances = (await xp.GetPositiveBalancesAsync(ct)).ToDictionary(x => x.UserId);
        var allUserIds = members.Select(x => x.UserId).Concat(aggregates.Keys).Distinct().ToArray();
        var activeRefs = await students.GetByIdentityUserIdsAsync(allUserIds, ct);
        foreach (var aggregate in aggregates.Values.Where(x => members.All(member => member.UserId != x.UserId)
                     && activeRefs.GetValueOrDefault(x.UserId)?.IsActive == true))
        {
            var balance = balances.GetValueOrDefault(aggregate.UserId);
            var league = balance is null ? null : FindLeague(leagues, balance.TotalXpUnits);
            if (league is null) continue;
            var member = new LeagueMembership(Guid.NewGuid(), season.Id, league.Id, aggregate.UserId,
                balance!.TotalXpUnits, season.StartsAtUtc);
            db.Memberships.Add(member);
            members.Add(member);
        }
        foreach (var member in members)
            member.Reconcile(aggregates.GetValueOrDefault(member.UserId)?.TotalXpUnits ?? 0,
                aggregates.GetValueOrDefault(member.UserId)?.LastEarnedAtUtc);
        foreach (var league in leagues)
        {
            var ranked = members.Where(x => x.LeagueId == league.Id && activeRefs.GetValueOrDefault(x.UserId)?.IsActive == true)
                .OrderByDescending(x => x.SeasonScoreUnits).ThenBy(x => x.LastScoreAtUtc ?? x.JoinedAtUtc).ThenBy(x => x.UserId).ToList();
            var count = MovementCount(ranked.Count);
            for (var index = 0; index < ranked.Count; index++)
            {
                var member = ranked[index]; var destination = league; var outcome = LeagueMovementOutcome.Stayed;
                if (index < count)
                {
                    if (league.DisplayOrder == leagues.Count) outcome = LeagueMovementOutcome.TopTier;
                    else { destination = leagues.Single(x => x.DisplayOrder == league.DisplayOrder + 1); outcome = LeagueMovementOutcome.Promoted; }
                }
                else if (index >= ranked.Count - count)
                {
                    if (league.DisplayOrder == 1) outcome = LeagueMovementOutcome.BottomTier;
                    else { destination = leagues.Single(x => x.DisplayOrder == league.DisplayOrder - 1); outcome = LeagueMovementOutcome.Relegated; }
                }
                member.Finalize(index + 1, outcome);
                db.MovementResults.Add(new(Guid.NewGuid(), season.Id, member.UserId, league.Id, destination.Id, index + 1, outcome, clock.UtcNow));
            }
        }
        season.Close(clock.UtcNow);
        if (season.AutoStartNext)
        {
            var next = new LeagueSeason(Guid.NewGuid(), season.Number + 1, season.EndsAtUtc,
                season.ConfigurationVersion, true, clock.UtcNow); db.Seasons.Add(next);
            foreach (var movement in db.MovementResults.Local.Where(x => x.SeasonId == season.Id))
                db.Memberships.Add(new(Guid.NewGuid(), next.Id, movement.ToLeagueId, movement.UserId,
                    balances.GetValueOrDefault(movement.UserId)?.TotalXpUnits ?? 0, next.StartsAtUtc));
        }
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
    }

    private async Task<Result<LeaderboardDto>> BuildLeaderboardAsync(LeagueSeason season, Guid leagueId,
        Guid? currentUserId, int page, int pageSize, CancellationToken ct)
    {
        var league = await db.Leagues.AsNoTracking().SingleOrDefaultAsync(x => x.Id == leagueId, ct);
        if (league is null) return Result<LeaderboardDto>.Failure(ProgressionErrors.NotFound);
        var ranked = await RankedActiveMembershipsAsync(season.Id, leagueId, ct); var movement = MovementCount(ranked.Count);
        (page, pageSize) = Page(page, pageSize, 50); var slice = ranked.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var profiles = await students.GetByIdentityUserIdsAsync(slice.Select(x => x.UserId).Append(currentUserId ?? Guid.Empty).Where(x => x != Guid.Empty).Distinct().ToArray(), ct);
        var leagueCount = (await ActiveLeaguesAsync(ct)).Count;
        LeaderboardItemDto Map(LeagueMembership x)
        {
            var rank = ranked.FindIndex(item => item.Id == x.Id) + 1; var profile = profiles.GetValueOrDefault(x.UserId);
            return new(rank, x.UserId, profile?.DisplayName ?? "ADEEB Student", profile?.AvatarUrl,
                ToXp(x.SeasonScoreUnits), x.UserId == currentUserId, Zone(rank, ranked.Count, movement, league.DisplayOrder, leagueCount));
        }
        var current = currentUserId.HasValue ? ranked.SingleOrDefault(x => x.UserId == currentUserId) : null;
        return Result<LeaderboardDto>.Success(new(ToDto(league, true), ToDto(season), slice.Select(Map).ToList(),
            page, pageSize, ranked.Count, movement, current is null ? null : Map(current)));
    }

    private async Task<List<LeagueMembership>> RankedActiveMembershipsAsync(Guid seasonId, Guid leagueId, CancellationToken ct)
    {
        var values = await db.Memberships.AsNoTracking().Where(x => x.SeasonId == seasonId && x.LeagueId == leagueId).ToListAsync(ct);
        var refs = await students.GetByIdentityUserIdsAsync(values.Select(x => x.UserId).ToArray(), ct);
        return values.Where(x => refs.GetValueOrDefault(x.UserId)?.IsActive == true)
            .OrderByDescending(x => x.SeasonScoreUnits).ThenBy(x => x.LastScoreAtUtc ?? x.JoinedAtUtc).ThenBy(x => x.UserId).ToList();
    }

    private async Task SeedFromLifetimeAsync(LeagueSeason season, List<LeagueDefinition> leagues, CancellationToken ct)
    {
        var balances = await xp.GetPositiveBalancesAsync(ct); var refs = await students.GetByIdentityUserIdsAsync(balances.Select(x => x.UserId).ToArray(), ct);
        foreach (var balance in balances.Where(x => refs.GetValueOrDefault(x.UserId)?.IsActive == true))
        { var league = FindLeague(leagues, balance.TotalXpUnits); if (league is not null) db.Memberships.Add(new(Guid.NewGuid(), season.Id, league.Id, balance.UserId, balance.TotalXpUnits, season.StartsAtUtc)); }
    }
    private async Task<PreviousSeasonDto?> PreviousAsync(Guid userId, CancellationToken ct) =>
        await db.MovementResults.AsNoTracking().Where(x => x.UserId == userId)
            .Join(db.Seasons, x => x.SeasonId, x => x.Id, (x, s) => new { x, s.Number }).OrderByDescending(x => x.Number)
            .Select(x => new PreviousSeasonDto(x.Number, x.x.FinalRank, x.x.Outcome.ToString(), x.x.FromLeagueId)).FirstOrDefaultAsync(ct);
    private async Task<bool> HasActiveSeasonAsync(CancellationToken ct) => await db.Seasons.AnyAsync(x => x.Status == LeagueSeasonStatus.Active, ct);
    private async Task<List<LeagueDefinition>> ActiveLeaguesAsync(CancellationToken ct) => await db.Leagues.Where(x => x.Status == LeagueDefinitionStatus.Active).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
    private static LeagueDefinition? FindLeague(IEnumerable<LeagueDefinition> values, long xpUnits) => values.FirstOrDefault(x => xpUnits >= x.MinLifetimeXpUnits && (!x.MaxLifetimeXpUnits.HasValue || xpUnits < x.MaxLifetimeXpUnits));
    private static bool ValidRanges(IEnumerable<LeagueDefinition> source)
    {
        var values = source.Where(x => x.Status == LeagueDefinitionStatus.Active).OrderBy(x => x.DisplayOrder).ToList();
        if (values.Count == 0 || values[0].DisplayOrder != 1 || values[0].MinLifetimeXpUnits != 0 || values[^1].MaxLifetimeXpUnits is not null) return false;
        for (var i = 0; i < values.Count; i++)
        { if (values[i].DisplayOrder != i + 1 || i > 0 && values[i - 1].MaxLifetimeXpUnits != values[i].MinLifetimeXpUnits) return false; }
        return true;
    }
    private static bool ValidDraftRanges(IEnumerable<LeagueDefinition> source)
    {
        var values = source.Where(x => x.Status == LeagueDefinitionStatus.Active).OrderBy(x => x.DisplayOrder).ToList();
        if (values.Count == 0) return true;
        if (values[0].DisplayOrder != 1 || values[0].MinLifetimeXpUnits != 0) return false;
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i].DisplayOrder != i + 1) return false;
            if (i > 0 && values[i - 1].MaxLifetimeXpUnits != values[i].MinLifetimeXpUnits) return false;
        }
        return true;
    }
    private LeagueDefinition? BuildCandidate(Guid? id, LeagueFormRequest r, string? avatar, int version)
    { try { return new(id ?? Guid.NewGuid(), r.NameTg ?? string.Empty, r.NameRu ?? string.Empty, avatar, ToUnits(r.MinXp), ToNullableUnits(r.MaxXp), r.DisplayOrder ?? 0, version, clock.UtcNow); } catch (ArgumentException) { return null; } }
    private static bool CountsForLeague(XpGrantedIntegrationEvent x) => x.EntryType == XpEntryType.Credit && x.SourceType != XpSourceType.AdminAdjustment && x.AmountUnits > 0;
    public static int MovementCount(int participants) => Math.Min(10, participants / 3);
    private static string Zone(int rank, int total, int movement, int order, int leagueCount) => movement == 0 ? "stable"
        : rank <= movement ? order == leagueCount ? "top" : "promotion"
        : rank > total - movement ? order == 1 ? "bottom" : "relegation" : "stable";
    private static (int Page, int Size) Page(int page, int size, int fallback) => (Math.Max(1, page), Math.Clamp(size <= 0 ? fallback : size, 1, 50));
    private static long ToUnits(decimal? value) => value.HasValue && value >= 0 && value * TestXpUnitScale == decimal.Truncate(value.Value * TestXpUnitScale) ? checked((long)(value.Value * TestXpUnitScale)) : -1;
    private static long? ToNullableUnits(decimal? value) => value.HasValue ? ToUnits(value) : null;
    private const int TestXpUnitScale = 2;
    private static decimal ToXp(long units) => units / (decimal)TestXpUnitScale;
    private static Guid? UserId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var value) ? value : null;
    private static bool Russian => CultureInfo.CurrentUICulture.Name.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
    private static LeagueDto ToDto(LeagueDefinition x, bool locked) => new(x.Id, Russian ? x.NameRu : x.NameTg,
        x.NameTg, x.NameRu, x.AvatarUrl, ToXp(x.MinLifetimeXpUnits), x.MaxLifetimeXpUnits.HasValue ? ToXp(x.MaxLifetimeXpUnits.Value) : null,
        x.DisplayOrder, (int)x.Status, x.ConfigurationVersion, locked);
    private SeasonDto ToDto(LeagueSeason x) => new(x.Id, x.Number, (int)x.Status, x.StartsAtUtc, x.EndsAtUtc, clock.UtcNow, x.AutoStartNext, x.ConfigurationVersion);
}
