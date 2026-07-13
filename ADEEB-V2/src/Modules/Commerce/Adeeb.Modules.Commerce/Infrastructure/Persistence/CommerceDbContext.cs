using Adeeb.Modules.Commerce.Domain.Entitlements;
using Adeeb.Modules.Commerce.Domain.Payments;
using Adeeb.Modules.Commerce.Domain.Tariffs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adeeb.Modules.Commerce.Infrastructure.Persistence;

public sealed class CommerceDbContext(DbContextOptions<CommerceDbContext> options) : DbContext(options)
{
    public DbSet<StudentEntitlement> StudentEntitlements => Set<StudentEntitlement>();
    public DbSet<CommerceTariff> Tariffs => Set<CommerceTariff>();
    public DbSet<PaymentReceipt> PaymentReceipts => Set<PaymentReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("commerce");
        modelBuilder.ApplyConfiguration(new StudentEntitlementConfiguration());
        modelBuilder.ApplyConfiguration(new CommerceTariffConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentReceiptConfiguration());
    }
}

internal sealed class CommerceTariffConfiguration : IEntityTypeConfiguration<CommerceTariff>
{
    public void Configure(EntityTypeBuilder<CommerceTariff> builder)
    {
        builder.ToTable("tariffs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(CommerceTariff.NameMaxLength).IsRequired();
        builder.Property(x => x.Price).HasColumnName("price").HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(CommerceTariff.CurrencyMaxLength).IsRequired();
        builder.Property(x => x.DurationDays).HasColumnName("duration_days").IsRequired();
        builder.Property(x => x.QrImageUrl).HasColumnName("qr_image_url").HasMaxLength(CommerceTariff.QrImageUrlMaxLength).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_commerce_tariffs_status");
    }
}

internal sealed class PaymentReceiptConfiguration : IEntityTypeConfiguration<PaymentReceipt>
{
    public void Configure(EntityTypeBuilder<PaymentReceipt> builder)
    {
        builder.ToTable("payment_receipts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.TariffId).HasColumnName("tariff_id").IsRequired();
        builder.Property(x => x.TariffNameSnapshot).HasColumnName("tariff_name_snapshot").HasMaxLength(CommerceTariff.NameMaxLength).IsRequired();
        builder.Property(x => x.PriceSnapshot).HasColumnName("price_snapshot").HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.CurrencySnapshot).HasColumnName("currency_snapshot").HasMaxLength(CommerceTariff.CurrencyMaxLength).IsRequired();
        builder.Property(x => x.DurationDaysSnapshot).HasColumnName("duration_days_snapshot").IsRequired();
        builder.Property(x => x.ReceiptImageObjectKey).HasColumnName("receipt_image_object_key").HasMaxLength(PaymentReceipt.ReceiptImageObjectKeyMaxLength).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(PaymentReceipt.IdempotencyKeyMaxLength).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(x => x.AdminNote).HasColumnName("admin_note").HasMaxLength(PaymentReceipt.AdminNoteMaxLength);
        builder.Property(x => x.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
        builder.Property(x => x.ReviewedAtUtc).HasColumnName("reviewed_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => new { x.StudentId, x.Status }).HasDatabaseName("ix_commerce_payment_receipts_student_status");
        builder.HasIndex(x => new { x.TariffId, x.Status }).HasDatabaseName("ix_commerce_payment_receipts_tariff_status");
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName(CommerceDatabaseConstraints.PaymentReceiptIdempotencyKeyUnique);
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
        builder.Property(x => x.SourcePaymentReceiptId).HasColumnName("source_payment_receipt_id");
        builder.Property(x => x.RevokeReason).HasColumnName("revoke_reason").HasMaxLength(256);
        builder.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName(CommerceDatabaseConstraints.StudentEntitlementIdempotencyKeyUnique);
        builder.HasIndex(x => new { x.StudentId, x.Kind, x.Status })
            .HasDatabaseName(CommerceDatabaseConstraints.StudentEntitlementStudentKindStatus);
        builder.HasIndex(x => x.SourcePaymentReceiptId)
            .IsUnique()
            .HasFilter("source_payment_receipt_id IS NOT NULL")
            .HasDatabaseName(CommerceDatabaseConstraints.StudentEntitlementSourcePaymentReceiptUnique);
    }
}
