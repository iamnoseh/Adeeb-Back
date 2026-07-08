using Adeeb.Modules.QuestionBank.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence;

public sealed class QuestionBankDbContext(DbContextOptions<QuestionBankDbContext> options) : DbContext(options)
{
    public DbSet<Question> Questions => Set<Question>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("question_bank");
        modelBuilder.ApplyConfiguration(new QuestionConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionTranslationMap());
        modelBuilder.ApplyConfiguration(new AnswerOptionMap());
        modelBuilder.ApplyConfiguration(new AnswerOptionTranslationMap());
    }
}

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SubjectId).HasColumnName("subject_id");
        builder.Property(x => x.TopicId).HasColumnName("topic_id");
        builder.Property(x => x.Topic).HasColumnName("topic").HasMaxLength(200);
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Difficulty).HasColumnName("difficulty").HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ImageUrl).HasColumnName("image_url").HasMaxLength(512);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(x => x.ArchivedAtUtc).HasColumnName("archived_at_utc");
        builder.Property(x => x.ArchiveReason).HasColumnName("archive_reason").HasMaxLength(160);
        builder.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
        builder.HasIndex(x => new { x.SubjectId, x.Status });
        builder.HasIndex(x => new { x.TopicId, x.Status });
        builder.HasIndex(x => new { x.Type, x.Difficulty, x.Status });
        builder.Navigation(x => x.Translations).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.AnswerOptions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Translations).WithOne().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.AnswerOptions).WithOne().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class QuestionTranslationMap : IEntityTypeConfiguration<QuestionTranslation>
{
    public void Configure(EntityTypeBuilder<QuestionTranslation> builder)
    {
        builder.ToTable("question_translations");
        builder.HasKey(x => new { x.QuestionId, x.Language });
        builder.Property(x => x.QuestionId).HasColumnName("question_id");
        builder.Property(x => x.Language).HasColumnName("language").HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Content).HasColumnName("content").HasMaxLength(4000);
        builder.Property(x => x.Explanation).HasColumnName("explanation").HasMaxLength(4000);
    }
}

internal sealed class AnswerOptionMap : IEntityTypeConfiguration<AnswerOption>
{
    public void Configure(EntityTypeBuilder<AnswerOption> builder)
    {
        builder.ToTable("answer_options");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.QuestionId).HasColumnName("question_id");
        builder.Property(x => x.DisplayOrder).HasColumnName("display_order");
        builder.Property(x => x.IsCorrect).HasColumnName("is_correct");
        builder.HasIndex(x => new { x.QuestionId, x.DisplayOrder });
        builder.Navigation(x => x.Translations).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Translations).WithOne().HasForeignKey(x => x.AnswerOptionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AnswerOptionTranslationMap : IEntityTypeConfiguration<AnswerOptionTranslation>
{
    public void Configure(EntityTypeBuilder<AnswerOptionTranslation> builder)
    {
        builder.ToTable("answer_option_translations");
        builder.HasKey(x => new { x.AnswerOptionId, x.Language });
        builder.Property(x => x.AnswerOptionId).HasColumnName("answer_option_id");
        builder.Property(x => x.Language).HasColumnName("language").HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Text).HasColumnName("text").HasMaxLength(1000);
        builder.Property(x => x.MatchPairText).HasColumnName("match_pair_text").HasMaxLength(1000);
    }
}
