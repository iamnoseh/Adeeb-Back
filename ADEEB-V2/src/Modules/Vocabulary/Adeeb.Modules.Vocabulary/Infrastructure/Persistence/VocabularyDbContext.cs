using Adeeb.Modules.Vocabulary.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adeeb.Modules.Vocabulary.Infrastructure.Persistence;

public sealed class VocabularyDbContext(DbContextOptions<VocabularyDbContext> options) : DbContext(options)
{
    public DbSet<LearningLanguage> Languages => Set<LearningLanguage>();
    public DbSet<VocabularyTopic> Topics => Set<VocabularyTopic>();
    public DbSet<VocabularyWord> Words => Set<VocabularyWord>();
    public DbSet<VocabularyRelation> Relations => Set<VocabularyRelation>();
    public DbSet<VocabularyQuestion> Questions => Set<VocabularyQuestion>();
    public DbSet<VocabularyDailyWord> DailyWords => Set<VocabularyDailyWord>();
    public DbSet<StudentVocabularyCourse> Courses => Set<StudentVocabularyCourse>();
    public DbSet<StudentWordProgress> WordProgress => Set<StudentWordProgress>();
    public DbSet<VocabularySession> Sessions => Set<VocabularySession>();
    public DbSet<VocabularySessionQuestion> SessionQuestions => Set<VocabularySessionQuestion>();
    public DbSet<VocabularySessionAnswer> SessionAnswers => Set<VocabularySessionAnswer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("vocabulary");
        builder.ApplyConfigurationsFromAssembly(typeof(VocabularyDbContext).Assembly);
    }
}

internal sealed class LearningLanguageMap : IEntityTypeConfiguration<LearningLanguage>
{
    public void Configure(EntityTypeBuilder<LearningLanguage> b)
    {
        b.ToTable("languages"); b.HasKey(x => x.Id); Columns.Id(b);
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(16).IsRequired();
        b.Property(x => x.NameTg).HasColumnName("name_tg").HasMaxLength(120).IsRequired();
        b.Property(x => x.NameRu).HasColumnName("name_ru").HasMaxLength(120).IsRequired();
        b.Property(x => x.DisplayOrder).HasColumnName("display_order"); b.Property(x => x.IsActive).HasColumnName("is_active");
        Columns.Audit(b); b.HasIndex(x => x.Code).IsUnique().HasDatabaseName(VocabularyDatabaseConstraints.LanguageCode);
    }
}

internal sealed class VocabularyTopicMap : IEntityTypeConfiguration<VocabularyTopic>
{
    public void Configure(EntityTypeBuilder<VocabularyTopic> b)
    {
        b.ToTable("topics"); b.HasKey(x => x.Id); Columns.Id(b);
        b.Property(x => x.LanguageId).HasColumnName("language_id"); Columns.Enum(b.Property(x => x.Level), "level");
        b.Property(x => x.NameTg).HasColumnName("name_tg").HasMaxLength(160); b.Property(x => x.NameRu).HasColumnName("name_ru").HasMaxLength(160);
        b.Property(x => x.DescriptionTg).HasColumnName("description_tg").HasMaxLength(1000); b.Property(x => x.DescriptionRu).HasColumnName("description_ru").HasMaxLength(1000);
        Columns.Enum(b.Property(x => x.Status), "status"); Columns.Audit(b);
        b.HasIndex(x => new { x.LanguageId, x.Level, x.Status });
        b.HasOne<LearningLanguage>().WithMany().HasForeignKey(x => x.LanguageId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VocabularyWordMap : IEntityTypeConfiguration<VocabularyWord>
{
    public void Configure(EntityTypeBuilder<VocabularyWord> b)
    {
        b.ToTable("words"); b.HasKey(x => x.Id); Columns.Id(b);
        b.Property(x => x.LanguageId).HasColumnName("language_id"); b.Property(x => x.TopicId).HasColumnName("topic_id"); Columns.Enum(b.Property(x => x.Level), "level");
        b.Property(x => x.TargetText).HasColumnName("target_text").HasMaxLength(240); b.Property(x => x.NormalizedText).HasColumnName("normalized_text").HasMaxLength(240);
        b.Property(x => x.TranslationTg).HasColumnName("translation_tg").HasMaxLength(500); b.Property(x => x.TranslationRu).HasColumnName("translation_ru").HasMaxLength(500);
        b.Property(x => x.ExplanationTg).HasColumnName("explanation_tg").HasMaxLength(2000); b.Property(x => x.ExplanationRu).HasColumnName("explanation_ru").HasMaxLength(2000);
        b.Property(x => x.ExampleTarget).HasColumnName("example_target").HasMaxLength(1000); b.Property(x => x.ExampleTg).HasColumnName("example_tg").HasMaxLength(1000); b.Property(x => x.ExampleRu).HasColumnName("example_ru").HasMaxLength(1000);
        Columns.Enum(b.Property(x => x.Status), "status"); Columns.Audit(b);
        b.HasIndex(x => new { x.LanguageId, x.NormalizedText }).IsUnique().HasDatabaseName(VocabularyDatabaseConstraints.WordIdentity);
        b.HasIndex(x => new { x.TopicId, x.Level, x.Status });
        b.HasOne<LearningLanguage>().WithMany().HasForeignKey(x => x.LanguageId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<VocabularyTopic>().WithMany().HasForeignKey(x => x.TopicId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VocabularyRelationMap : IEntityTypeConfiguration<VocabularyRelation>
{
    public void Configure(EntityTypeBuilder<VocabularyRelation> b)
    {
        b.ToTable("relations"); b.HasKey(x => x.Id); Columns.Id(b); b.Property(x => x.WordId).HasColumnName("word_id"); b.Property(x => x.RelatedWordId).HasColumnName("related_word_id");
        Columns.Enum(b.Property(x => x.Type), "type"); b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasIndex(x => new { x.WordId, x.RelatedWordId, x.Type }).IsUnique().HasDatabaseName(VocabularyDatabaseConstraints.RelationIdentity);
        b.HasOne<VocabularyWord>().WithMany().HasForeignKey(x => x.WordId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<VocabularyWord>().WithMany().HasForeignKey(x => x.RelatedWordId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VocabularyQuestionMap : IEntityTypeConfiguration<VocabularyQuestion>
{
    public void Configure(EntityTypeBuilder<VocabularyQuestion> b)
    {
        b.ToTable("questions"); b.HasKey(x => x.Id); Columns.Id(b); b.Property(x => x.WordId).HasColumnName("word_id"); Columns.Enum(b.Property(x => x.Type), "type");
        b.Property(x => x.PromptTarget).HasColumnName("prompt_target").HasMaxLength(2000); b.Property(x => x.PromptTg).HasColumnName("prompt_tg").HasMaxLength(2000); b.Property(x => x.PromptRu).HasColumnName("prompt_ru").HasMaxLength(2000);
        b.Property(x => x.CorrectTokenIndex).HasColumnName("correct_token_index"); Columns.Enum(b.Property(x => x.Status), "status"); b.Property(x => x.ReviewedBy).HasColumnName("reviewed_by"); b.Property(x => x.ReviewedAtUtc).HasColumnName("reviewed_at_utc"); Columns.Audit(b);
        b.HasIndex(x => new { x.WordId, x.Type, x.Status }); b.Navigation(x => x.Options).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasMany(x => x.Options).WithOne().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<VocabularyWord>().WithMany().HasForeignKey(x => x.WordId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VocabularyQuestionOptionMap : IEntityTypeConfiguration<VocabularyQuestionOption>
{
    public void Configure(EntityTypeBuilder<VocabularyQuestionOption> b)
    {
        b.ToTable("question_options"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.QuestionId).HasColumnName("question_id"); b.Property(x => x.WordId).HasColumnName("word_id");
        b.Property(x => x.ValueTarget).HasColumnName("value_target").HasMaxLength(1000); b.Property(x => x.ValueTg).HasColumnName("value_tg").HasMaxLength(1000); b.Property(x => x.ValueRu).HasColumnName("value_ru").HasMaxLength(1000);
        b.Property(x => x.DisplayOrder).HasColumnName("display_order"); b.Property(x => x.IsCorrect).HasColumnName("is_correct"); b.Property(x => x.CorrectOrder).HasColumnName("correct_order"); b.HasIndex(x => new { x.QuestionId, x.DisplayOrder }).IsUnique();
        b.HasOne<VocabularyWord>().WithMany().HasForeignKey(x => x.WordId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VocabularyDailyWordMap : IEntityTypeConfiguration<VocabularyDailyWord>
{
    public void Configure(EntityTypeBuilder<VocabularyDailyWord> b)
    { b.ToTable("daily_words"); b.HasKey(x => new { x.LanguageId, x.LocalDate }).HasName(VocabularyDatabaseConstraints.DailyWordIdentity); b.Property(x => x.LanguageId).HasColumnName("language_id"); b.Property(x => x.LocalDate).HasColumnName("local_date"); b.Property(x => x.WordId).HasColumnName("word_id"); b.Property(x => x.IsAutomatic).HasColumnName("is_automatic"); b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc"); b.HasOne<LearningLanguage>().WithMany().HasForeignKey(x => x.LanguageId).OnDelete(DeleteBehavior.Cascade); b.HasOne<VocabularyWord>().WithMany().HasForeignKey(x => x.WordId).OnDelete(DeleteBehavior.Restrict); }
}

internal sealed class StudentVocabularyCourseMap : IEntityTypeConfiguration<StudentVocabularyCourse>
{
    public void Configure(EntityTypeBuilder<StudentVocabularyCourse> b)
    { b.ToTable("student_courses"); b.HasKey(x => x.StudentId).HasName(VocabularyDatabaseConstraints.CourseIdentity); b.Property(x => x.StudentId).HasColumnName("student_id"); b.Property(x => x.LanguageId).HasColumnName("language_id"); Columns.Enum(b.Property(x => x.Level), "level"); b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc"); b.HasOne<LearningLanguage>().WithMany().HasForeignKey(x => x.LanguageId).OnDelete(DeleteBehavior.Restrict); }
}

internal sealed class StudentWordProgressMap : IEntityTypeConfiguration<StudentWordProgress>
{
    public void Configure(EntityTypeBuilder<StudentWordProgress> b)
    { b.ToTable("student_word_progress"); b.HasKey(x => new { x.StudentId, x.WordId }).HasName(VocabularyDatabaseConstraints.ProgressIdentity); b.Property(x => x.StudentId).HasColumnName("student_id"); b.Property(x => x.WordId).HasColumnName("word_id"); b.Property(x => x.MasteryLevel).HasColumnName("mastery_level"); b.Property(x => x.CorrectCount).HasColumnName("correct_count"); b.Property(x => x.WrongCount).HasColumnName("wrong_count"); b.Property(x => x.LastPracticedAtUtc).HasColumnName("last_practiced_at_utc"); b.Property(x => x.NextReviewDate).HasColumnName("next_review_date"); b.HasIndex(x => new { x.StudentId, x.NextReviewDate }); b.HasOne<VocabularyWord>().WithMany().HasForeignKey(x => x.WordId).OnDelete(DeleteBehavior.Cascade); }
}

internal sealed class VocabularySessionMap : IEntityTypeConfiguration<VocabularySession>
{
    public void Configure(EntityTypeBuilder<VocabularySession> b)
    { b.ToTable("sessions"); b.HasKey(x => x.Id); Columns.Id(b); b.Property(x => x.StudentId).HasColumnName("student_id"); b.Property(x => x.LanguageId).HasColumnName("language_id"); Columns.Enum(b.Property(x => x.Mode), "mode"); Columns.Enum(b.Property(x => x.Level), "level"); b.Property(x => x.TopicId).HasColumnName("topic_id"); b.Property(x => x.LocalDate).HasColumnName("local_date"); b.Property(x => x.QuestionCount).HasColumnName("question_count"); Columns.Enum(b.Property(x => x.Status), "status"); b.Property(x => x.CorrectCount).HasColumnName("correct_count"); b.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc"); b.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc"); b.HasIndex(x => new { x.StudentId, x.LanguageId, x.Mode }).IsUnique().HasFilter("status = 'InProgress'").HasDatabaseName(VocabularyDatabaseConstraints.InProgressSessionLookup); b.HasOne<LearningLanguage>().WithMany().HasForeignKey(x => x.LanguageId).OnDelete(DeleteBehavior.Restrict); }
}

internal sealed class VocabularySessionQuestionMap : IEntityTypeConfiguration<VocabularySessionQuestion>
{
    public void Configure(EntityTypeBuilder<VocabularySessionQuestion> b)
    { b.ToTable("session_questions"); b.HasKey(x => new { x.SessionId, x.QuestionId }); b.Property(x => x.SessionId).HasColumnName("session_id"); b.Property(x => x.QuestionId).HasColumnName("question_id"); b.Property(x => x.WordId).HasColumnName("word_id"); b.Property(x => x.Order).HasColumnName("display_order"); Columns.Enum(b.Property(x => x.Type), "type"); b.Property(x => x.Prompt).HasColumnName("prompt").HasMaxLength(2000); b.Property(x => x.CorrectTokenIndex).HasColumnName("correct_token_index"); b.Property(x => x.OptionsJson).HasColumnName("options_json").HasColumnType("jsonb"); b.Property(x => x.CorrectAnswerJson).HasColumnName("correct_answer_json").HasColumnType("jsonb"); b.HasIndex(x => new { x.SessionId, x.Order }).IsUnique(); b.HasOne<VocabularySession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade); }
}

internal sealed class VocabularySessionAnswerMap : IEntityTypeConfiguration<VocabularySessionAnswer>
{
    public void Configure(EntityTypeBuilder<VocabularySessionAnswer> b)
    { b.ToTable("session_answers"); b.HasKey(x => new { x.SessionId, x.QuestionId }).HasName(VocabularyDatabaseConstraints.SessionAnswerIdentity); b.Property(x => x.SessionId).HasColumnName("session_id"); b.Property(x => x.QuestionId).HasColumnName("question_id"); b.Property(x => x.SubmissionJson).HasColumnName("submission_json").HasColumnType("jsonb"); b.Property(x => x.IsCorrect).HasColumnName("is_correct"); b.Property(x => x.AnsweredAtUtc).HasColumnName("answered_at_utc"); b.HasOne<VocabularySessionQuestion>().WithOne().HasForeignKey<VocabularySessionAnswer>(x => new { x.SessionId, x.QuestionId }).OnDelete(DeleteBehavior.Cascade); }
}

file static class Columns
{
    public static void Id<TEntity>(EntityTypeBuilder<TEntity> b) where TEntity : class => b.Property<Guid>("Id").HasColumnName("id");
    public static void Audit<TEntity>(EntityTypeBuilder<TEntity> b) where TEntity : class { b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnName("created_at_utc"); b.Property<DateTimeOffset>("UpdatedAtUtc").HasColumnName("updated_at_utc"); }
    public static void Enum<TEnum>(PropertyBuilder<TEnum> p, string name) where TEnum : struct, Enum => p.HasColumnName(name).HasConversion<string>().HasMaxLength(32);
}
