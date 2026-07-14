using Adeeb.Modules.Mmt.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence;

internal sealed class StudentMmtProfileConfiguration : IEntityTypeConfiguration<StudentMmtProfile>
{
    public void Configure(EntityTypeBuilder<StudentMmtProfile> b)
    {
        b.ToTable("student_profiles", t =>
            t.HasCheckConstraint("ck_mmt_student_profile_year", "admission_year >= 2000 AND admission_year <= 2100"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.MmtClusterId).HasColumnName("cluster_id");
        b.Property(x => x.AdmissionYear).HasColumnName("admission_year");
        b.Property(x => x.GoalAdmissionProgramId).HasColumnName("goal_admission_program_id");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => new { x.UserId, x.AdmissionYear })
            .IsUnique()
            .HasFilter("is_active")
            .HasDatabaseName(MmtDatabaseConstraints.ActiveStudentProfile);
        b.HasIndex(x => new { x.MmtClusterId, x.AdmissionYear, x.IsActive })
            .HasDatabaseName("ix_mmt_student_profiles_admin");
        b.HasOne(x => x.MmtCluster).WithMany().HasForeignKey(x => x.MmtClusterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.GoalAdmissionProgram).WithMany().HasForeignKey(x => x.GoalAdmissionProgramId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Choices).WithOne(x => x.StudentMmtProfile).HasForeignKey(x => x.StudentMmtProfileId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Choices).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class StudentAdmissionChoiceConfiguration : IEntityTypeConfiguration<StudentAdmissionChoice>
{
    public void Configure(EntityTypeBuilder<StudentAdmissionChoice> b)
    {
        b.ToTable("student_admission_choices", t =>
            t.HasCheckConstraint("ck_mmt_choice_priority", "priority_order >= 1 AND priority_order <= 12"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.StudentMmtProfileId).HasColumnName("student_mmt_profile_id");
        b.Property(x => x.AdmissionProgramId).HasColumnName("admission_program_id");
        b.Property(x => x.PriorityOrder).HasColumnName("priority_order");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => new { x.StudentMmtProfileId, x.PriorityOrder }).IsUnique().HasDatabaseName(MmtDatabaseConstraints.ChoicePriority);
        b.HasIndex(x => new { x.StudentMmtProfileId, x.AdmissionProgramId }).IsUnique().HasDatabaseName(MmtDatabaseConstraints.ChoiceProgram);
        b.HasOne(x => x.AdmissionProgram).WithMany().HasForeignKey(x => x.AdmissionProgramId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MmtExamEvaluationConfiguration : IEntityTypeConfiguration<MmtExamEvaluation>
{
    public void Configure(EntityTypeBuilder<MmtExamEvaluation> b)
    {
        b.ToTable("exam_evaluations", t =>
        {
            t.HasCheckConstraint("ck_mmt_evaluation_year", "admission_year >= 2000 AND admission_year <= 2100");
            t.HasCheckConstraint("ck_mmt_evaluation_score", "total_score >= 0 AND total_score <= 1000 AND scale(total_score) <= 2");
            t.HasCheckConstraint("ck_mmt_evaluation_readiness", "readiness_percentage IS NULL OR (readiness_percentage >= 0 AND readiness_percentage <= 100 AND scale(readiness_percentage) <= 2)");
            t.HasCheckConstraint("ck_mmt_evaluation_goal_missing", "missing_score_for_goal IS NULL OR (missing_score_for_goal >= 0 AND missing_score_for_goal <= 1000 AND scale(missing_score_for_goal) <= 2)");
            t.HasCheckConstraint("ck_mmt_evaluation_accepted_priority", "accepted_choice_priority IS NULL OR (accepted_choice_priority >= 1 AND accepted_choice_priority <= 12)");
            t.HasCheckConstraint("ck_mmt_evaluation_accepted_pair", "(accepted_choice_priority IS NULL) = (accepted_admission_program_id IS NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.StudentMmtProfileId).HasColumnName("student_mmt_profile_id");
        b.Property(x => x.ExamSessionId).HasColumnName("exam_session_id");
        b.Property(x => x.TotalScore).HasColumnName("total_score").HasColumnType("numeric");
        b.Property(x => x.AdmissionYear).HasColumnName("admission_year");
        b.Property(x => x.ClusterId).HasColumnName("cluster_id");
        b.Property(x => x.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        b.Property(x => x.AcceptedChoicePriority).HasColumnName("accepted_choice_priority");
        b.Property(x => x.AcceptedAdmissionProgramId).HasColumnName("accepted_admission_program_id");
        b.Property(x => x.MissingScoreForGoal).HasColumnName("missing_score_for_goal").HasColumnType("numeric");
        b.Property(x => x.ReadinessPercentage).HasColumnName("readiness_percentage").HasColumnType("numeric");
        b.Property(x => x.MotivationalMessageKey).HasColumnName("motivational_message_key").HasMaxLength(80).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasIndex(x => new { x.UserId, x.EvaluatedAtUtc }).IsDescending(false, true).HasDatabaseName("ix_mmt_evaluations_user_history");
        b.HasIndex(x => new { x.AdmissionYear, x.EvaluatedAtUtc }).IsDescending(false, true).HasDatabaseName("ix_mmt_evaluations_admin_history");
        b.HasOne(x => x.StudentMmtProfile).WithMany().HasForeignKey(x => x.StudentMmtProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AcceptedAdmissionProgram).WithMany().HasForeignKey(x => x.AcceptedAdmissionProgramId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Cluster).WithMany().HasForeignKey(x => x.ClusterId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.ChoiceSnapshots).WithOne(x => x.MmtExamEvaluation).HasForeignKey(x => x.MmtExamEvaluationId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.ChoiceSnapshots).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MmtAdmissionChoiceSnapshotConfiguration : IEntityTypeConfiguration<MmtAdmissionChoiceSnapshot>
{
    public void Configure(EntityTypeBuilder<MmtAdmissionChoiceSnapshot> b)
    {
        b.ToTable("admission_choice_snapshots", t =>
        {
            t.HasCheckConstraint("ck_mmt_snapshot_priority", "priority_order >= 1 AND priority_order <= 12");
            t.HasCheckConstraint("ck_mmt_snapshot_student_score", "student_score >= 0 AND student_score <= 1000 AND scale(student_score) <= 2");
            t.HasCheckConstraint("ck_mmt_snapshot_missing", "missing_score IS NULL OR (missing_score >= 0 AND missing_score <= 1000 AND scale(missing_score) <= 2)");
            t.HasCheckConstraint("ck_mmt_snapshot_year", "admission_year >= 2000 AND admission_year <= 2100");
            t.HasCheckConstraint("ck_mmt_snapshot_passing_score", "passing_score_used IS NULL OR (passing_score_used > 0 AND passing_score_used <= 1000 AND scale(passing_score_used) <= 2)");
            t.HasCheckConstraint("ck_mmt_snapshot_threshold", "conservative_threshold_used IS NULL OR (conservative_threshold_used > 0 AND conservative_threshold_used <= 1000 AND scale(conservative_threshold_used) <= 2)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.MmtExamEvaluationId).HasColumnName("mmt_exam_evaluation_id");
        b.Property(x => x.PriorityOrder).HasColumnName("priority_order");
        b.Property(x => x.AdmissionProgramId).HasColumnName("admission_program_id");
        b.Property(x => x.UniversityNameSnapshot).HasColumnName("university_name_snapshot").HasMaxLength(300).IsRequired();
        b.Property(x => x.SpecialtyCodeSnapshot).HasColumnName("specialty_code_snapshot").HasMaxLength(60).IsRequired();
        b.Property(x => x.SpecialtyNameSnapshot).HasColumnName("specialty_name_snapshot").HasMaxLength(240).IsRequired();
        b.Property(x => x.ClusterCodeSnapshot).HasColumnName("cluster_code_snapshot").HasMaxLength(40).IsRequired();
        b.Property(x => x.AdmissionType).HasColumnName("admission_type").HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.StudyForm).HasColumnName("study_form").HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.StudyLanguage).HasColumnName("study_language").HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.AdmissionYear).HasColumnName("admission_year");
        b.Property(x => x.PassingScoreUsed).HasColumnName("passing_score_used").HasColumnType("numeric");
        b.Property(x => x.ConservativeThresholdUsed).HasColumnName("conservative_threshold_used").HasColumnType("numeric");
        b.Property(x => x.StudentScore).HasColumnName("student_score").HasColumnType("numeric");
        b.Property(x => x.IsAccepted).HasColumnName("is_accepted");
        b.Property(x => x.MissingScore).HasColumnName("missing_score").HasColumnType("numeric");
        b.HasIndex(x => new { x.MmtExamEvaluationId, x.PriorityOrder }).IsUnique().HasDatabaseName("ux_mmt_snapshot_evaluation_priority");
        b.HasOne(x => x.AdmissionProgram).WithMany().HasForeignKey(x => x.AdmissionProgramId).OnDelete(DeleteBehavior.Restrict);
    }
}
