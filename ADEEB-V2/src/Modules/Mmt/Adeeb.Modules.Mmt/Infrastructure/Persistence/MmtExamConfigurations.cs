using Adeeb.Modules.Mmt.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence;

internal static class MmtExamDatabaseNames
{
    public const string VersionYearOfficial = "ux_mmt_exam_versions_year_official";
    public const string BlueprintCluster = "ux_mmt_exam_blueprints_version_cluster";
    public const string SubtestCode = "ux_mmt_exam_subtests_blueprint_code";
    public const string SubtestOrder = "ux_mmt_exam_subtests_blueprint_order";
    public const string RangeCode = "ux_mmt_exam_ranges_version_cluster_code";
    public const string RangeSpecialty = "ux_mmt_exam_range_specialty";
    public const string ScaleIdentity = "ux_mmt_exam_scale_identity";
    public const string ThresholdIdentity = "ux_mmt_exam_threshold_identity";
}

internal sealed class MmtExamVersionConfiguration : IEntityTypeConfiguration<MmtExamVersion>
{
    public void Configure(EntityTypeBuilder<MmtExamVersion> b)
    {
        b.ToTable("exam_versions", t => t.HasCheckConstraint("ck_mmt_exam_version_year", "admission_year BETWEEN 2000 AND 2100"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.AdmissionYear).HasColumnName("admission_year");
        b.Property(x => x.NameTg).HasColumnName("name_tg").HasMaxLength(160).IsRequired();
        b.Property(x => x.NameRu).HasColumnName("name_ru").HasMaxLength(160).IsRequired();
        b.Property(x => x.IsOfficial).HasColumnName("is_official");
        b.Property(x => x.SourceUrl).HasColumnName("source_url").HasMaxLength(500);
        b.Property(x => x.SourceChecksum).HasColumnName("source_checksum").HasMaxLength(128);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.PublishedAtUtc).HasColumnName("published_at_utc");
        b.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
        b.HasIndex(x => new { x.AdmissionYear, x.IsOfficial }).IsUnique().HasDatabaseName(MmtExamDatabaseNames.VersionYearOfficial);
        b.Navigation(x => x.Blueprints).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.SpecialtyRanges).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.ScaleEntries).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Thresholds).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MmtClusterExamBlueprintConfiguration : IEntityTypeConfiguration<MmtClusterExamBlueprint>
{
    public void Configure(EntityTypeBuilder<MmtClusterExamBlueprint> b)
    {
        b.ToTable("exam_blueprints", t => t.HasCheckConstraint("ck_mmt_exam_blueprint_duration", "duration_minutes BETWEEN 30 AND 360"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ExamVersionId).HasColumnName("exam_version_id");
        b.Property(x => x.MmtClusterId).HasColumnName("cluster_id");
        b.Property(x => x.DurationMinutes).HasColumnName("duration_minutes");
        b.Ignore(x => x.QuestionCount);
        b.HasIndex(x => new { x.ExamVersionId, x.MmtClusterId }).IsUnique().HasDatabaseName(MmtExamDatabaseNames.BlueprintCluster);
        b.HasOne(x => x.ExamVersion).WithMany(x => x.Blueprints).HasForeignKey(x => x.ExamVersionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Cluster).WithMany().HasForeignKey(x => x.MmtClusterId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Subtests).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MmtSubtestBlueprintConfiguration : IEntityTypeConfiguration<MmtSubtestBlueprint>
{
    public void Configure(EntityTypeBuilder<MmtSubtestBlueprint> b)
    {
        b.ToTable("exam_subtests", t =>
        {
            t.HasCheckConstraint("ck_mmt_exam_subtest_order", "display_order BETWEEN 1 AND 4");
            t.HasCheckConstraint("ck_mmt_exam_subtest_counts", "single_choice_count >= 0 AND matching_count >= 0 AND short_answer_count >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.MmtClusterExamBlueprintId).HasColumnName("exam_blueprint_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(2);
        b.Property(x => x.DisplayOrder).HasColumnName("display_order");
        b.Property(x => x.SubjectId).HasColumnName("subject_id");
        b.Property(x => x.SingleChoiceCount).HasColumnName("single_choice_count");
        b.Property(x => x.MatchingCount).HasColumnName("matching_count");
        b.Property(x => x.ShortAnswerCount).HasColumnName("short_answer_count");
        b.Ignore(x => x.QuestionCount); b.Ignore(x => x.MaxRawScore);
        b.HasIndex(x => new { x.MmtClusterExamBlueprintId, x.Code }).IsUnique().HasDatabaseName(MmtExamDatabaseNames.SubtestCode);
        b.HasIndex(x => new { x.MmtClusterExamBlueprintId, x.DisplayOrder }).IsUnique().HasDatabaseName(MmtExamDatabaseNames.SubtestOrder);
        b.HasOne(x => x.ClusterBlueprint).WithMany(x => x.Subtests).HasForeignKey(x => x.MmtClusterExamBlueprintId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MmtSpecialtyRangeConfiguration : IEntityTypeConfiguration<MmtSpecialtyRange>
{
    public void Configure(EntityTypeBuilder<MmtSpecialtyRange> b)
    {
        b.ToTable("exam_specialty_ranges"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.ExamVersionId).HasColumnName("exam_version_id");
        b.Property(x => x.MmtClusterId).HasColumnName("cluster_id"); b.Property(x => x.Code).HasColumnName("code").HasMaxLength(40);
        b.Property(x => x.A2MaxScore).HasColumnName("a2_max_score").HasColumnType("numeric(9,4)");
        b.Property(x => x.A3MaxScore).HasColumnName("a3_max_score").HasColumnType("numeric(9,4)");
        b.Property(x => x.A4MaxScore).HasColumnName("a4_max_score").HasColumnType("numeric(9,4)");
        b.HasIndex(x => new { x.ExamVersionId, x.MmtClusterId, x.Code }).IsUnique().HasDatabaseName(MmtExamDatabaseNames.RangeCode);
        b.HasOne(x => x.ExamVersion).WithMany(x => x.SpecialtyRanges).HasForeignKey(x => x.ExamVersionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Cluster).WithMany().HasForeignKey(x => x.MmtClusterId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Specialties).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MmtSpecialtyRangeSpecialtyConfiguration : IEntityTypeConfiguration<MmtSpecialtyRangeSpecialty>
{
    public void Configure(EntityTypeBuilder<MmtSpecialtyRangeSpecialty> b)
    {
        b.ToTable("exam_specialty_range_specialties"); b.HasKey(x => new { x.MmtSpecialtyRangeId, x.SpecialtyId });
        b.Property(x => x.MmtSpecialtyRangeId).HasColumnName("specialty_range_id"); b.Property(x => x.SpecialtyId).HasColumnName("specialty_id");
        b.HasIndex(x => x.SpecialtyId).HasDatabaseName(MmtExamDatabaseNames.RangeSpecialty);
        b.HasOne(x => x.Range).WithMany(x => x.Specialties).HasForeignKey(x => x.MmtSpecialtyRangeId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Specialty).WithMany().HasForeignKey(x => x.SpecialtyId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MmtScoreScaleEntryConfiguration : IEntityTypeConfiguration<MmtScoreScaleEntry>
{
    public void Configure(EntityTypeBuilder<MmtScoreScaleEntry> b)
    {
        b.ToTable("exam_score_scale", t => t.HasCheckConstraint("ck_mmt_exam_scale_raw", "raw_score BETWEEN 0 AND 40"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ExamVersionId).HasColumnName("exam_version_id"); b.Property(x => x.MmtClusterId).HasColumnName("cluster_id");
        b.Property(x => x.SubtestCode).HasColumnName("subtest_code").HasMaxLength(2); b.Property(x => x.SpecialtyRangeId).HasColumnName("specialty_range_id");
        b.Property(x => x.RawScore).HasColumnName("raw_score"); b.Property(x => x.ScaledScore).HasColumnName("scaled_score").HasColumnType("numeric(9,4)");
        b.Property(x => x.MaxScaledScore).HasColumnName("max_scaled_score").HasColumnType("numeric(9,4)");
        b.HasIndex(x => new { x.ExamVersionId, x.MmtClusterId, x.SubtestCode, x.SpecialtyRangeId, x.RawScore }).IsUnique().HasDatabaseName(MmtExamDatabaseNames.ScaleIdentity);
        b.HasOne(x => x.ExamVersion).WithMany(x => x.ScaleEntries).HasForeignKey(x => x.ExamVersionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.SpecialtyRange).WithMany().HasForeignKey(x => x.SpecialtyRangeId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MmtPassThresholdConfiguration : IEntityTypeConfiguration<MmtPassThreshold>
{
    public void Configure(EntityTypeBuilder<MmtPassThreshold> b)
    {
        b.ToTable("exam_pass_thresholds", t => t.HasCheckConstraint("ck_mmt_exam_threshold_raw", "minimum_raw_score BETWEEN 0 AND 40"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ExamVersionId).HasColumnName("exam_version_id"); b.Property(x => x.MmtClusterId).HasColumnName("cluster_id");
        b.Property(x => x.SubtestCode).HasColumnName("subtest_code").HasMaxLength(2); b.Property(x => x.MinimumRawScore).HasColumnName("minimum_raw_score");
        b.HasIndex(x => new { x.ExamVersionId, x.MmtClusterId, x.SubtestCode }).IsUnique().HasDatabaseName(MmtExamDatabaseNames.ThresholdIdentity);
        b.HasOne(x => x.ExamVersion).WithMany(x => x.Thresholds).HasForeignKey(x => x.ExamVersionId).OnDelete(DeleteBehavior.Cascade);
    }
}
