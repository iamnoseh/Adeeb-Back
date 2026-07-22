using Adeeb.Modules.QuestionBank.Domain;
using Adeeb.SharedKernel.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql;

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence;

internal static class StudentTestingDatabaseNames
{
    public const string RedListUserQuestion = "ux_question_bank_red_list_user_question";
    public const string AttemptQuestionOrder = "ux_question_bank_attempt_question_order";
    public const string AttemptQuestionIdentity = "ux_question_bank_attempt_question_identity";
    public const string AttemptAnswerQuestion = "ux_question_bank_attempt_answer_question";
    public const string AttemptDraftQuestion = "ux_question_bank_attempt_draft_question";
    public const string AttemptResult = "ux_question_bank_attempt_result";
    public const string MonthlyWindow = "ux_question_bank_monthly_window";
    public const string XpLedgerIdempotency = "ux_question_bank_xp_ledger_idempotency";
    public const string XpLedgerSource = "ux_question_bank_xp_ledger_source";
    public const string XpLedgerUserHistory = "ix_question_bank_xp_ledger_user_history";
    public const string XpLedgerUserSource = "ix_question_bank_xp_ledger_user_source";
    public const string XpSettlementAttempt = "ux_question_bank_xp_settlement_attempt";
    public const string XpSettlementLedger = "ux_question_bank_xp_settlement_ledger";
    public const string XpBalanceNonNegative = "ck_question_bank_xp_balance_non_negative";
    public const string XpLedgerAmountNonNegative = "ck_question_bank_xp_ledger_amount_non_negative";
    public const string XpSettlementNonNegative = "ck_question_bank_xp_settlement_non_negative";
    public const string XpSettlementTotal = "ck_question_bank_xp_settlement_total";
    public const string XpSettlementZeroCorrect = "ck_question_bank_xp_settlement_zero_correct";

    public static bool IsMonthlyWindowViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: MonthlyWindow
        };

}

internal static class TestAttemptFinalizationConcurrency
{
    public static async Task AcquireAttemptLockAsync(QuestionBankDbContext db, Guid attemptId, CancellationToken ct)
    {
        if (!db.Database.IsNpgsql()) return;
        var lockKey = BitConverter.ToInt64(attemptId.ToByteArray(), 0);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockKey})", ct);
    }
}

internal sealed class TestAttemptConfiguration : IEntityTypeConfiguration<TestAttempt>
{
    public void Configure(EntityTypeBuilder<TestAttempt> b)
    {
        b.ToTable("test_attempts"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Mode).HasColumnName("mode").HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.SubjectId).HasColumnName("subject_id"); b.Property(x => x.ClusterId).HasColumnName("cluster_id");
        b.Property(x => x.MonthlyWindowKey).HasColumnName("monthly_window_key").HasMaxLength(10);
        b.Property(x => x.ModeSnapshotJson).HasColumnName("mode_snapshot_json").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc"); b.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc");
        b.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc"); b.Property(x => x.SubmittedAtUtc).HasColumnName("submitted_at_utc");
        b.Property(x => x.QuestionCount).HasColumnName("question_count"); b.Property(x => x.CorrectCount).HasColumnName("correct_count");
        b.Property(x => x.WrongCount).HasColumnName("wrong_count"); b.Property(x => x.Score).HasColumnName("score").HasColumnType("numeric(10,2)");
        b.Property(x => x.Percentage).HasColumnName("percentage").HasColumnType("numeric(5,2)");
        b.HasIndex(x => new { x.UserId, x.StartedAtUtc }); b.HasIndex(x => new { x.UserId, x.Status });
        b.HasIndex(x => new { x.UserId, x.Mode, x.MonthlyWindowKey }).IsUnique().HasDatabaseName(StudentTestingDatabaseNames.MonthlyWindow)
            .HasFilter("monthly_window_key IS NOT NULL");
        b.Navigation(x => x.Questions).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Answers).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.DraftAnswers).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasMany(x => x.Questions).WithOne(x => x.TestAttempt).HasForeignKey(x => x.TestAttemptId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Answers).WithOne().HasForeignKey(x => x.TestAttemptId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.DraftAnswers).WithOne().HasForeignKey(x => x.TestAttemptId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Result).WithOne().HasForeignKey<TestAttemptResult>(x => x.TestAttemptId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.XpSettlement).WithOne().HasForeignKey<TestXpSettlement>(x => x.AttemptId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class XpLedgerEntryConfiguration : IEntityTypeConfiguration<XpLedgerEntry>
{
    public void Configure(EntityTypeBuilder<XpLedgerEntry> b)
    {
        b.ToTable("xp_ledger_entries", table => table.HasCheckConstraint(
            StudentTestingDatabaseNames.XpLedgerAmountNonNegative, "amount_units >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.SourceType).HasColumnName("source_type").HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.SourceId).HasColumnName("source_id").HasMaxLength(128);
        b.Property(x => x.EntryType).HasColumnName("entry_type").HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.AmountUnits).HasColumnName("amount_units");
        b.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(160);
        b.Property(x => x.BalanceBeforeUnits).HasColumnName("balance_before_units");
        b.Property(x => x.BalanceAfterUnits).HasColumnName("balance_after_units");
        b.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName(StudentTestingDatabaseNames.XpLedgerIdempotency);
        b.HasIndex(x => new { x.UserId, x.SourceType, x.SourceId }).IsUnique()
            .HasDatabaseName(StudentTestingDatabaseNames.XpLedgerSource);
        b.HasIndex(x => new { x.UserId, x.CreatedAtUtc }).HasDatabaseName(StudentTestingDatabaseNames.XpLedgerUserHistory);
        b.HasIndex(x => new { x.UserId, x.SourceType }).HasDatabaseName(StudentTestingDatabaseNames.XpLedgerUserSource);
    }
}

internal sealed class StudentXpBalanceConfiguration : IEntityTypeConfiguration<StudentXpBalance>
{
    public void Configure(EntityTypeBuilder<StudentXpBalance> b)
    {
        b.ToTable("student_xp_balances", table => table.HasCheckConstraint(
            StudentTestingDatabaseNames.XpBalanceNonNegative, "total_xp_units >= 0"));
        b.HasKey(x => x.UserId);
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.TotalXpUnits).HasColumnName("total_xp_units");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

internal sealed class TestXpSettlementConfiguration : IEntityTypeConfiguration<TestXpSettlement>
{
    public void Configure(EntityTypeBuilder<TestXpSettlement> b)
    {
        b.ToTable("test_xp_settlements", table =>
        {
            table.HasCheckConstraint(StudentTestingDatabaseNames.XpSettlementNonNegative,
                "easy_correct_count >= 0 AND medium_correct_count >= 0 AND hard_correct_count >= 0 AND answer_xp_units >= 0 AND completion_bonus_xp_units >= 0 AND total_xp_units >= 0");
            table.HasCheckConstraint(StudentTestingDatabaseNames.XpSettlementTotal,
                "total_xp_units = answer_xp_units + completion_bonus_xp_units");
            table.HasCheckConstraint(StudentTestingDatabaseNames.XpSettlementZeroCorrect,
                "easy_correct_count + medium_correct_count + hard_correct_count > 0 OR total_xp_units = 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.AttemptId).HasColumnName("attempt_id");
        b.Property(x => x.LedgerEntryId).HasColumnName("ledger_entry_id");
        b.Property(x => x.EasyCorrectCount).HasColumnName("easy_correct_count");
        b.Property(x => x.MediumCorrectCount).HasColumnName("medium_correct_count");
        b.Property(x => x.HardCorrectCount).HasColumnName("hard_correct_count");
        b.Property(x => x.AnswerXpUnits).HasColumnName("answer_xp_units");
        b.Property(x => x.CompletionBonusXpUnits).HasColumnName("completion_bonus_xp_units");
        b.Property(x => x.TotalXpUnits).HasColumnName("total_xp_units");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasIndex(x => x.AttemptId).IsUnique().HasDatabaseName(StudentTestingDatabaseNames.XpSettlementAttempt);
        b.HasIndex(x => x.LedgerEntryId).IsUnique().HasDatabaseName(StudentTestingDatabaseNames.XpSettlementLedger);
        b.HasOne<XpLedgerEntry>().WithOne().HasForeignKey<TestXpSettlement>(x => x.LedgerEntryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class TestAttemptQuestionConfiguration : IEntityTypeConfiguration<TestAttemptQuestion>
{
    public void Configure(EntityTypeBuilder<TestAttemptQuestion> b)
    {
        b.ToTable("test_attempt_questions"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.TestAttemptId).HasColumnName("test_attempt_id");
        b.Property(x => x.QuestionId).HasColumnName("question_id"); b.Property(x => x.DisplayOrder).HasColumnName("display_order");
        b.Property(x => x.SubjectId).HasColumnName("subject_id"); b.Property(x => x.TopicId).HasColumnName("topic_id");
        b.Property(x => x.QuestionType).HasColumnName("question_type").HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.Difficulty).HasColumnName("difficulty").HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.QuestionSnapshotJson).HasColumnName("question_snapshot_json").HasColumnType("jsonb");
        b.HasIndex(x => new { x.TestAttemptId, x.DisplayOrder }).IsUnique().HasDatabaseName(StudentTestingDatabaseNames.AttemptQuestionOrder);
        b.HasIndex(x => new { x.TestAttemptId, x.QuestionId }).IsUnique().HasDatabaseName(StudentTestingDatabaseNames.AttemptQuestionIdentity);
    }
}

internal sealed class TestAttemptAnswerConfiguration : IEntityTypeConfiguration<TestAttemptAnswer>
{
    public void Configure(EntityTypeBuilder<TestAttemptAnswer> b)
    {
        b.ToTable("test_attempt_answers"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.TestAttemptId).HasColumnName("test_attempt_id");
        b.Property(x => x.TestAttemptQuestionId).HasColumnName("test_attempt_question_id"); b.Property(x => x.QuestionId).HasColumnName("question_id");
        b.Property(x => x.AnswerSnapshotJson).HasColumnName("answer_snapshot_json").HasColumnType("jsonb");
        b.Property(x => x.IsAnswered).HasColumnName("is_answered"); b.Property(x => x.IsCorrect).HasColumnName("is_correct");
        b.Property(x => x.CorrectPairsCount).HasColumnName("correct_pairs_count"); b.Property(x => x.TotalPairsCount).HasColumnName("total_pairs_count");
        b.Property(x => x.AnsweredAtUtc).HasColumnName("answered_at_utc");
        b.HasIndex(x => x.TestAttemptQuestionId).IsUnique().HasDatabaseName(StudentTestingDatabaseNames.AttemptAnswerQuestion);
        b.HasOne<TestAttemptQuestion>().WithMany().HasForeignKey(x => x.TestAttemptQuestionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TestAttemptResultConfiguration : IEntityTypeConfiguration<TestAttemptResult>
{
    public void Configure(EntityTypeBuilder<TestAttemptResult> b)
    {
        b.ToTable("test_attempt_results"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.TestAttemptId).HasColumnName("test_attempt_id");
        b.Property(x => x.TopicBreakdownJson).HasColumnName("topic_breakdown_json").HasColumnType("jsonb");
        b.Property(x => x.ResultSnapshotJson).HasColumnName("result_snapshot_json").HasColumnType("jsonb");
        b.Property(x => x.OfficialScoreSnapshotJson).HasColumnName("official_score_snapshot_json").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasIndex(x => x.TestAttemptId).IsUnique().HasDatabaseName(StudentTestingDatabaseNames.AttemptResult);
    }
}

internal sealed class TestAttemptDraftAnswerConfiguration : IEntityTypeConfiguration<TestAttemptDraftAnswer>
{
    public void Configure(EntityTypeBuilder<TestAttemptDraftAnswer> b)
    {
        b.ToTable("test_attempt_draft_answers"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.TestAttemptId).HasColumnName("test_attempt_id");
        b.Property(x => x.TestAttemptQuestionId).HasColumnName("test_attempt_question_id");
        b.Property(x => x.QuestionId).HasColumnName("question_id");
        b.Property(x => x.AnswerSnapshotJson).HasColumnName("answer_snapshot_json").HasColumnType("jsonb");
        b.Property(x => x.IsMarkedForReview).HasColumnName("is_marked_for_review");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => x.TestAttemptQuestionId).IsUnique().HasDatabaseName(StudentTestingDatabaseNames.AttemptDraftQuestion);
        b.HasOne<TestAttemptQuestion>().WithMany().HasForeignKey(x => x.TestAttemptQuestionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class StudentRedListItemConfiguration : IEntityTypeConfiguration<StudentRedListItem>
{
    public void Configure(EntityTypeBuilder<StudentRedListItem> b)
    {
        b.ToTable("student_red_list_items"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.QuestionId).HasColumnName("question_id"); b.Property(x => x.SubjectId).HasColumnName("subject_id");
        b.Property(x => x.TopicId).HasColumnName("topic_id"); b.Property(x => x.QuestionType).HasColumnName("question_type").HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.WrongCount).HasColumnName("wrong_count"); b.Property(x => x.CorrectStreak).HasColumnName("correct_streak");
        b.Property(x => x.LastWrongAtUtc).HasColumnName("last_wrong_at_utc"); b.Property(x => x.LastPracticedAtUtc).HasColumnName("last_practiced_at_utc");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.MasteredAtUtc).HasColumnName("mastered_at_utc");
        b.HasIndex(x => new { x.UserId, x.QuestionId }).IsUnique().HasDatabaseName(StudentTestingDatabaseNames.RedListUserQuestion);
        b.HasIndex(x => new { x.UserId, x.Status, x.SubjectId }); b.HasIndex(x => new { x.UserId, x.LastPracticedAtUtc });
    }
}
