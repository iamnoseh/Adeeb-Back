using Adeeb.Modules.Progression.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adeeb.Modules.Progression.Infrastructure.Persistence;

public sealed class ProgressionDbContext(DbContextOptions<ProgressionDbContext> options) : DbContext(options)
{
    public DbSet<LeagueDefinition> Leagues => Set<LeagueDefinition>();
    public DbSet<LeagueSeason> Seasons => Set<LeagueSeason>();
    public DbSet<LeagueMembership> Memberships => Set<LeagueMembership>();
    public DbSet<LeagueScoreEvent> ScoreEvents => Set<LeagueScoreEvent>();
    public DbSet<LeagueMovementResult> MovementResults => Set<LeagueMovementResult>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("progression");
        b.ApplyConfiguration(new LeagueMap()); b.ApplyConfiguration(new SeasonMap());
        b.ApplyConfiguration(new MembershipMap()); b.ApplyConfiguration(new ScoreEventMap());
        b.ApplyConfiguration(new MovementMap());
    }
}

internal static class ProgressionDatabaseNames
{
    public const string LeagueOrder = "ux_progression_leagues_order_active";
    public const string LeagueRange = "ck_progression_leagues_range";
    public const string ActiveSeason = "ux_progression_seasons_active";
    public const string MembershipUser = "ux_progression_memberships_season_user";
    public const string Leaderboard = "ix_progression_memberships_leaderboard";
    public const string ScoreEventLedger = "pk_progression_score_events_ledger";
    public const string MovementUser = "ux_progression_movements_season_user";
}

internal sealed class LeagueMap : IEntityTypeConfiguration<LeagueDefinition>
{
    public void Configure(EntityTypeBuilder<LeagueDefinition> b)
    {
        b.ToTable("league_definitions", t => t.HasCheckConstraint(ProgressionDatabaseNames.LeagueRange,
            "min_lifetime_xp_units >= 0 AND (max_lifetime_xp_units IS NULL OR max_lifetime_xp_units > min_lifetime_xp_units)"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.NameTg).HasColumnName("name_tg").HasMaxLength(LeagueDefinition.NameMaxLength).IsRequired();
        b.Property(x => x.NameRu).HasColumnName("name_ru").HasMaxLength(LeagueDefinition.NameMaxLength).IsRequired();
        b.Property(x => x.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(LeagueDefinition.AvatarUrlMaxLength);
        b.Property(x => x.MinLifetimeXpUnits).HasColumnName("min_lifetime_xp_units");
        b.Property(x => x.MaxLifetimeXpUnits).HasColumnName("max_lifetime_xp_units");
        b.Property(x => x.DisplayOrder).HasColumnName("display_order");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<int>();
        b.Property(x => x.ConfigurationVersion).HasColumnName("configuration_version");
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc"); b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => x.DisplayOrder).IsUnique().HasFilter("status = 1").HasDatabaseName(ProgressionDatabaseNames.LeagueOrder);
    }
}
internal sealed class SeasonMap : IEntityTypeConfiguration<LeagueSeason>
{
    public void Configure(EntityTypeBuilder<LeagueSeason> b)
    {
        b.ToTable("league_seasons"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Number).HasColumnName("number"); b.HasIndex(x => x.Number).IsUnique();
        b.Property(x => x.StartsAtUtc).HasColumnName("starts_at_utc"); b.Property(x => x.EndsAtUtc).HasColumnName("ends_at_utc");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<int>(); b.Property(x => x.AutoStartNext).HasColumnName("auto_start_next");
        b.Property(x => x.ConfigurationVersion).HasColumnName("configuration_version");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc"); b.Property(x => x.ClosedAtUtc).HasColumnName("closed_at_utc");
        b.HasIndex(x => x.Status).IsUnique().HasFilter("status = 1").HasDatabaseName(ProgressionDatabaseNames.ActiveSeason);
    }
}
internal sealed class MembershipMap : IEntityTypeConfiguration<LeagueMembership>
{
    public void Configure(EntityTypeBuilder<LeagueMembership> b)
    {
        b.ToTable("league_memberships"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.SeasonId).HasColumnName("season_id"); b.Property(x => x.LeagueId).HasColumnName("league_id");
        b.Property(x => x.UserId).HasColumnName("user_id"); b.Property(x => x.InitialLifetimeXpUnits).HasColumnName("initial_lifetime_xp_units");
        b.Property(x => x.SeasonScoreUnits).HasColumnName("season_score_units"); b.Property(x => x.JoinedAtUtc).HasColumnName("joined_at_utc");
        b.Property(x => x.LastScoreAtUtc).HasColumnName("last_score_at_utc"); b.Property(x => x.FinalRank).HasColumnName("final_rank");
        b.Property(x => x.Outcome).HasColumnName("outcome").HasConversion<int?>();
        b.HasIndex(x => new { x.SeasonId, x.UserId }).IsUnique().HasDatabaseName(ProgressionDatabaseNames.MembershipUser);
        b.HasIndex(x => new { x.SeasonId, x.LeagueId, x.SeasonScoreUnits, x.LastScoreAtUtc, x.UserId }).HasDatabaseName(ProgressionDatabaseNames.Leaderboard);
        b.HasOne<LeagueSeason>().WithMany().HasForeignKey(x => x.SeasonId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LeagueDefinition>().WithMany().HasForeignKey(x => x.LeagueId).OnDelete(DeleteBehavior.Restrict);
    }
}
internal sealed class ScoreEventMap : IEntityTypeConfiguration<LeagueScoreEvent>
{
    public void Configure(EntityTypeBuilder<LeagueScoreEvent> b)
    {
        b.ToTable("league_score_events"); b.HasKey(x => x.LedgerEntryId).HasName(ProgressionDatabaseNames.ScoreEventLedger);
        b.Property(x => x.LedgerEntryId).HasColumnName("ledger_entry_id"); b.Property(x => x.MembershipId).HasColumnName("membership_id");
        b.Property(x => x.AmountUnits).HasColumnName("amount_units"); b.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
        b.HasOne<LeagueMembership>().WithMany().HasForeignKey(x => x.MembershipId).OnDelete(DeleteBehavior.Cascade);
    }
}
internal sealed class MovementMap : IEntityTypeConfiguration<LeagueMovementResult>
{
    public void Configure(EntityTypeBuilder<LeagueMovementResult> b)
    {
        b.ToTable("league_movement_results"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.SeasonId).HasColumnName("season_id"); b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.FromLeagueId).HasColumnName("from_league_id"); b.Property(x => x.ToLeagueId).HasColumnName("to_league_id");
        b.Property(x => x.FinalRank).HasColumnName("final_rank"); b.Property(x => x.Outcome).HasColumnName("outcome").HasConversion<int>();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasIndex(x => new { x.SeasonId, x.UserId }).IsUnique().HasDatabaseName(ProgressionDatabaseNames.MovementUser);
    }
}
