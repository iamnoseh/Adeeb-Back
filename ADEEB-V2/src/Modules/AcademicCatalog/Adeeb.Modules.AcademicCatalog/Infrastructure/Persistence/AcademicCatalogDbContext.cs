using Adeeb.Modules.AcademicCatalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adeeb.Modules.AcademicCatalog.Infrastructure.Persistence;

public sealed class AcademicCatalogDbContext(DbContextOptions<AcademicCatalogDbContext> options) : DbContext(options)
{
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Topic> Topics => Set<Topic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("academic");
        modelBuilder.ApplyConfiguration(new SubjectConfiguration());
        modelBuilder.ApplyConfiguration(new TopicConfiguration());
        modelBuilder.ApplyConfiguration(new SubjectTranslationMap());
        modelBuilder.ApplyConfiguration(new TopicTranslationMap());
    }
}

internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("subjects");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.IconUrl).HasColumnName("icon_url").HasMaxLength(512);
        builder.Property(x => x.DisplayOrder).HasColumnName("display_order");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => new { x.Status, x.DisplayOrder });
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(x => x.ArchivedAtUtc).HasColumnName("archived_at_utc");
        builder.Property(x => x.ArchiveReason).HasColumnName("archive_reason").HasMaxLength(160);
        builder.HasMany(x => x.Topics).WithOne(x => x.Subject).HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Translations).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Topics).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Translations).WithOne().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("topics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SubjectId).HasColumnName("subject_id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.HasIndex(x => new { x.SubjectId, x.Code }).IsUnique();
        builder.Property(x => x.DisplayOrder).HasColumnName("display_order");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => new { x.SubjectId, x.Status, x.DisplayOrder });
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(x => x.ArchivedAtUtc).HasColumnName("archived_at_utc");
        builder.Property(x => x.ArchiveReason).HasColumnName("archive_reason").HasMaxLength(160);
        builder.Navigation(x => x.Translations).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Translations).WithOne().HasForeignKey(x => x.TopicId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SubjectTranslationMap : IEntityTypeConfiguration<SubjectTranslation>
{
    public void Configure(EntityTypeBuilder<SubjectTranslation> builder)
    {
        builder.ToTable("subject_translations");
        builder.HasKey(x => new { x.SubjectId, x.Language });
        builder.Property(x => x.SubjectId).HasColumnName("subject_id");
        builder.Property(x => x.Language).HasColumnName("language").HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
    }
}

internal sealed class TopicTranslationMap : IEntityTypeConfiguration<TopicTranslation>
{
    public void Configure(EntityTypeBuilder<TopicTranslation> builder)
    {
        builder.ToTable("topic_translations");
        builder.HasKey(x => new { x.TopicId, x.Language });
        builder.Property(x => x.TopicId).HasColumnName("topic_id");
        builder.Property(x => x.Language).HasColumnName("language").HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
    }
}
