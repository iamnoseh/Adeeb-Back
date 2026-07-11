using Adeeb.Modules.Commerce.Domain.Entitlements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adeeb.Modules.Commerce.Infrastructure.Persistence;

public sealed class CommerceDbContext(DbContextOptions<CommerceDbContext> options) : DbContext(options)
{
    public DbSet<StudentEntitlement> StudentEntitlements => Set<StudentEntitlement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("commerce");
        modelBuilder.ApplyConfiguration(new StudentEntitlementConfiguration());
    }
}

internal sealed class StudentEntitlementConfiguration : IEntityTypeConfiguration<StudentEntitlement>
{
    public void Configure(EntityTypeBuilder<StudentEntitlement> builder)
    {
        builder.ToTable("student_entitlements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.Kind).HasColumnName("kind").HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(x => x.Source).HasColumnName("source").HasConversion<int>().IsRequired();
        builder.Property(x => x.StartsAtUtc).HasColumnName("starts_at_utc").IsRequired();
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.RevokeReason).HasColumnName("revoke_reason").HasMaxLength(256);
        builder.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName(CommerceDatabaseConstraints.StudentEntitlementIdempotencyKeyUnique);
        builder.HasIndex(x => new { x.StudentId, x.Kind, x.Status })
            .HasDatabaseName(CommerceDatabaseConstraints.StudentEntitlementStudentKindStatus);
    }
}
