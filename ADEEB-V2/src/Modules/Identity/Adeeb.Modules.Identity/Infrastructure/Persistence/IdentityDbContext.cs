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
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        builder.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(320).IsRequired();
        builder.HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(32);
        builder.Property(x => x.NormalizedPhoneNumber).HasColumnName("normalized_phone_number").HasMaxLength(32);
        builder.HasIndex(x => x.NormalizedPhoneNumber).IsUnique().HasFilter("normalized_phone_number IS NOT NULL");
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
        builder.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(80).IsRequired();
        builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(80).IsRequired();
        builder.Property(x => x.PreferredLanguage).HasColumnName("preferred_language").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.EmailVerified).HasColumnName("email_verified");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(x => x.LastLoginAtUtc).HasColumnName("last_login_at_utc");
    }
}

internal sealed class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        builder.ToTable("auth_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.FamilyId).HasColumnName("family_id");
        builder.Property(x => x.DeviceId).HasColumnName("device_id").HasMaxLength(128).IsRequired();
        builder.Property(x => x.DeviceName).HasColumnName("device_name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Platform).HasColumnName("platform").HasMaxLength(40).IsRequired();
        builder.Property(x => x.AppVersion).HasColumnName("app_version").HasMaxLength(40);
        builder.Property(x => x.RefreshTokenHash).HasColumnName("refresh_token_hash").HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(x => x.LastUsedAtUtc).HasColumnName("last_used_at_utc");
        builder.Property(x => x.RotatedAtUtc).HasColumnName("rotated_at_utc");
        builder.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.Property(x => x.RevokeReason).HasColumnName("revoke_reason").HasMaxLength(80);
        builder.Property(x => x.ReplacedBySessionId).HasColumnName("replaced_by_session_id");
        builder.Property(x => x.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(64);
        builder.Property(x => x.LastUsedIp).HasColumnName("last_used_ip").HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
        builder.HasIndex(x => x.RefreshTokenHash);
        builder.HasIndex(x => x.FamilyId);
        builder.HasIndex(x => new { x.UserId, x.RevokedAtUtc });
        builder.HasIndex(x => new { x.ExpiresAtUtc, x.RevokedAtUtc });
    }
}
