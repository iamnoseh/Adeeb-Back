using Adeeb.Modules.Identity.Domain.Sessions;
using Adeeb.Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adeeb.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new AuthSessionConfiguration());
    }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.NormalizedEmail).HasMaxLength(320).IsRequired();
        builder.HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.PreferredLanguage).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
    }
}

internal sealed class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        builder.ToTable("auth_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DeviceId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DeviceName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Platform).HasMaxLength(40).IsRequired();
        builder.Property(x => x.AppVersion).HasMaxLength(40);
        builder.Property(x => x.RefreshTokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RevokeReason).HasMaxLength(80);
        builder.Property(x => x.CreatedByIp).HasMaxLength(64);
        builder.Property(x => x.LastUsedIp).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.HasIndex(x => x.RefreshTokenHash);
        builder.HasIndex(x => x.FamilyId);
        builder.HasIndex(x => new { x.UserId, x.RevokedAtUtc });
        builder.HasIndex(x => new { x.ExpiresAtUtc, x.RevokedAtUtc });
    }
}
