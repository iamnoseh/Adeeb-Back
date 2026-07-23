using Adeeb.Modules.Students.Domain.Students;
using Adeeb.Modules.Students.Domain.Education;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adeeb.Modules.Students.Infrastructure.Persistence;

public sealed class StudentsDbContext(DbContextOptions<StudentsDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentDailyActivity> DailyActivities => Set<StudentDailyActivity>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<StudentEducationProfile> EducationProfiles => Set<StudentEducationProfile>();
    public DbSet<StudentSchoolEnrollment> SchoolEnrollments => Set<StudentSchoolEnrollment>();
    public DbSet<SchoolSuggestion> SchoolSuggestions => Set<SchoolSuggestion>();
    public DbSet<AcademicYearRollover> AcademicYearRollovers => Set<AcademicYearRollover>();
    public DbSet<AcademicYearRolloverItem> AcademicYearRolloverItems => Set<AcademicYearRolloverItem>();
    public DbSet<StudentEducationAuditLog> EducationAuditLogs => Set<StudentEducationAuditLog>();
    public DbSet<EducationImportBatch> EducationImportBatches => Set<EducationImportBatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("students");
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
        modelBuilder.ApplyConfiguration(new StudentProfileConfiguration());
        modelBuilder.ApplyConfiguration(new StudentDailyActivityConfiguration());
        modelBuilder.ApplyConfiguration(new RegionConfiguration());
        modelBuilder.ApplyConfiguration(new SchoolConfiguration());
        modelBuilder.ApplyConfiguration(new StudentEducationProfileConfiguration());
        modelBuilder.ApplyConfiguration(new StudentSchoolEnrollmentConfiguration());
        modelBuilder.ApplyConfiguration(new SchoolSuggestionConfiguration());
        modelBuilder.ApplyConfiguration(new AcademicYearRolloverConfiguration());
        modelBuilder.ApplyConfiguration(new AcademicYearRolloverItemConfiguration());
        modelBuilder.ApplyConfiguration(new StudentEducationAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new EducationImportBatchConfiguration());
    }
}

internal sealed class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.ToTable("regions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ParentId).HasColumnName("parent_id");
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<int>().IsRequired();
        builder.Property(x => x.NameTg).HasColumnName("name_tg").HasMaxLength(Region.NameMaxLength).IsRequired();
        builder.Property(x => x.NameRu).HasColumnName("name_ru").HasMaxLength(Region.NameMaxLength).IsRequired();
        builder.Property(x => x.NormalizedNameTg).HasColumnName("normalized_name_tg").HasMaxLength(Region.NameMaxLength).IsRequired();
        builder.Property(x => x.NormalizedNameRu).HasColumnName("normalized_name_ru").HasMaxLength(Region.NameMaxLength).IsRequired();
        builder.Property(x => x.FullPathTg).HasColumnName("full_path_tg").HasMaxLength(Region.FullPathMaxLength).IsRequired();
        builder.Property(x => x.FullPathRu).HasColumnName("full_path_ru").HasMaxLength(Region.FullPathMaxLength).IsRequired();
        builder.Property(x => x.PathIds).HasColumnName("path_ids").HasColumnType("uuid[]").IsRequired();
        builder.Property(x => x.Depth).HasColumnName("depth").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
        builder.HasOne<Region>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.PathIds).HasMethod("gin");
        builder.HasIndex(x => new { x.ParentId, x.Type, x.NormalizedNameRu }).IsUnique()
            .HasFilter("parent_id IS NOT NULL AND is_active = true").HasDatabaseName(StudentDatabaseConstraints.RegionSiblingUnique);
        builder.HasIndex(x => new { x.Type, x.NormalizedNameRu }).IsUnique()
            .HasFilter("parent_id IS NULL AND is_active = true").HasDatabaseName(StudentDatabaseConstraints.RootRegionTypeNameUnique);
        builder.ToTable(t => t.HasCheckConstraint("CK_regions_depth_non_negative", "depth >= 0"));
    }
}

internal sealed class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.ToTable("schools");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RegionId).HasColumnName("region_id").IsRequired();
        builder.Property(x => x.NameTg).HasColumnName("name_tg").HasMaxLength(School.NameMaxLength);
        builder.Property(x => x.NameRu).HasColumnName("name_ru").HasMaxLength(School.NameMaxLength).IsRequired();
        builder.Property(x => x.ShortName).HasColumnName("short_name").HasMaxLength(School.ShortNameMaxLength);
        builder.Property(x => x.Number).HasColumnName("number");
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(x => x.NormalizedName).HasColumnName("normalized_name").HasMaxLength(School.NormalizedNameMaxLength).IsRequired();
        builder.Property(x => x.SearchText).HasColumnName("search_text").HasMaxLength(School.SearchTextMaxLength).IsRequired();
        builder.Property(x => x.AddressText).HasColumnName("address_text").HasMaxLength(School.AddressTextMaxLength);
        builder.Property(x => x.VerifiedAtUtc).HasColumnName("verified_at_utc");
        builder.Property(x => x.VerifiedByUserId).HasColumnName("verified_by_user_id");
        builder.Property(x => x.ArchivedAtUtc).HasColumnName("archived_at_utc");
        builder.Property(x => x.ArchivedByUserId).HasColumnName("archived_by_user_id");
        builder.Property(x => x.MergedIntoSchoolId).HasColumnName("merged_into_school_id");
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
        builder.HasOne<Region>().WithMany().HasForeignKey(x => x.RegionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<School>().WithMany().HasForeignKey(x => x.MergedIntoSchoolId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.RegionId, x.Status, x.Number });
        builder.HasIndex(x => new { x.RegionId, x.Number, x.Type }).IsUnique()
            .HasFilter("number IS NOT NULL AND status IN (0, 1, 2)").HasDatabaseName(StudentDatabaseConstraints.SchoolNumberUnique);
        builder.HasIndex(x => new { x.RegionId, x.NormalizedName, x.Type }).IsUnique()
            .HasFilter("number IS NULL AND status IN (0, 1, 2)").HasDatabaseName(StudentDatabaseConstraints.SchoolNameUnique);
        builder.ToTable(t => t.HasCheckConstraint("CK_schools_number_positive", "number IS NULL OR number > 0"));
    }
}

internal sealed class StudentEducationProfileConfiguration : IEntityTypeConfiguration<StudentEducationProfile>
{
    public void Configure(EntityTypeBuilder<StudentEducationProfile> builder)
    {
        builder.ToTable("student_education_profiles");
        builder.HasKey(x => x.StudentId);
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.ResidenceRegionId).HasColumnName("residence_region_id");
        builder.Property(x => x.SchoolId).HasColumnName("school_id");
        builder.Property(x => x.PendingSchoolSuggestionId).HasColumnName("pending_school_suggestion_id");
        builder.Property(x => x.CurrentGrade).HasColumnName("current_grade");
        builder.Property(x => x.AcademicYearStart).HasColumnName("academic_year_start");
        builder.Property(x => x.AcademicYearEnd).HasColumnName("academic_year_end");
        builder.Property(x => x.ExpectedGraduationYear).HasColumnName("expected_graduation_year");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(x => x.AddressText).HasColumnName("address_text").HasMaxLength(School.AddressTextMaxLength);
        builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
        builder.HasOne<Student>().WithOne().HasForeignKey<StudentEducationProfile>(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Region>().WithMany().HasForeignKey(x => x.ResidenceRegionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<School>().WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchoolSuggestion>().WithMany().HasForeignKey(x => x.PendingSchoolSuggestionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.StudentId).IsUnique().HasDatabaseName(StudentDatabaseConstraints.EducationProfileStudentUnique);
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_student_education_profiles_grade", "current_grade IS NULL OR (current_grade >= 1 AND current_grade <= 11)");
            t.HasCheckConstraint("CK_student_education_profiles_year", "academic_year_start IS NULL OR academic_year_end = academic_year_start + 1");
        });
    }
}

internal sealed class StudentSchoolEnrollmentConfiguration : IEntityTypeConfiguration<StudentSchoolEnrollment>
{
    public void Configure(EntityTypeBuilder<StudentSchoolEnrollment> builder)
    {
        builder.ToTable("student_school_enrollments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.RegionId).HasColumnName("region_id").IsRequired();
        builder.Property(x => x.Grade).HasColumnName("grade").IsRequired();
        builder.Property(x => x.AcademicYearStart).HasColumnName("academic_year_start").IsRequired();
        builder.Property(x => x.AcademicYearEnd).HasColumnName("academic_year_end").IsRequired();
        builder.Property(x => x.IsCurrent).HasColumnName("is_current").IsRequired();
        builder.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc").IsRequired();
        builder.Property(x => x.EndedAtUtc).HasColumnName("ended_at_utc");
        builder.Property(x => x.ChangeReason).HasColumnName("change_reason").HasMaxLength(StudentSchoolEnrollment.ReasonMaxLength);
        builder.Property(x => x.Source).HasColumnName("source").HasConversion<int>().IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<School>().WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Region>().WithMany().HasForeignKey(x => x.RegionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.StudentId).IsUnique().HasFilter("is_current = true").HasDatabaseName(StudentDatabaseConstraints.CurrentEnrollmentUnique);
        builder.HasIndex(x => new { x.SchoolId, x.AcademicYearStart, x.AcademicYearEnd });
        builder.HasIndex(x => new { x.RegionId, x.AcademicYearStart, x.AcademicYearEnd });
        builder.HasIndex(x => new { x.StudentId, x.AcademicYearStart, x.AcademicYearEnd });
        builder.HasIndex(x => new { x.Grade, x.AcademicYearStart, x.AcademicYearEnd });
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_student_school_enrollments_grade", "grade >= 1 AND grade <= 11");
            t.HasCheckConstraint("CK_student_school_enrollments_year", "academic_year_end = academic_year_start + 1");
        });
    }
}

internal sealed class SchoolSuggestionConfiguration : IEntityTypeConfiguration<SchoolSuggestion>
{
    public void Configure(EntityTypeBuilder<SchoolSuggestion> builder)
    {
        builder.ToTable("school_suggestions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SubmittedByStudentId).HasColumnName("submitted_by_student_id").IsRequired();
        builder.Property(x => x.SuggestedName).HasColumnName("suggested_name").HasMaxLength(SchoolSuggestion.NameMaxLength).IsRequired();
        builder.Property(x => x.SuggestedNumber).HasColumnName("suggested_number");
        builder.Property(x => x.RegionId).HasColumnName("region_id").IsRequired();
        builder.Property(x => x.NormalizedName).HasColumnName("normalized_name").HasMaxLength(School.NormalizedNameMaxLength).IsRequired();
        builder.Property(x => x.AddressText).HasColumnName("address_text").HasMaxLength(SchoolSuggestion.AddressMaxLength);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(x => x.ApprovedSchoolId).HasColumnName("approved_school_id");
        builder.Property(x => x.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(SchoolSuggestion.RejectionReasonMaxLength);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.ReviewedAtUtc).HasColumnName("reviewed_at_utc");
        builder.Property(x => x.ReviewedByAdminId).HasColumnName("reviewed_by_admin_id");
        builder.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
        builder.HasOne<Student>().WithMany().HasForeignKey(x => x.SubmittedByStudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Region>().WithMany().HasForeignKey(x => x.RegionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<School>().WithMany().HasForeignKey(x => x.ApprovedSchoolId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.SubmittedByStudentId, x.RegionId, x.NormalizedName }).IsUnique()
            .HasFilter("status = 0").HasDatabaseName(StudentDatabaseConstraints.SuggestionPendingUnique);
    }
}

internal sealed class AcademicYearRolloverConfiguration : IEntityTypeConfiguration<AcademicYearRollover>
{
    public void Configure(EntityTypeBuilder<AcademicYearRollover> builder)
    {
        builder.ToTable("academic_year_rollovers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AcademicYearStart).HasColumnName("academic_year_start").IsRequired();
        builder.Property(x => x.AcademicYearEnd).HasColumnName("academic_year_end").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(x => x.PreviewCreatedAtUtc).HasColumnName("preview_created_at_utc").IsRequired();
        builder.Property(x => x.ApprovedAtUtc).HasColumnName("approved_at_utc");
        builder.Property(x => x.ExecutedAtUtc).HasColumnName("executed_at_utc");
        builder.Property(x => x.ExecutedByUserId).HasColumnName("executed_by_user_id");
        builder.Property(x => x.PromotedCount).HasColumnName("promoted_count").IsRequired();
        builder.Property(x => x.GraduatedCount).HasColumnName("graduated_count").IsRequired();
        builder.Property(x => x.SkippedCount).HasColumnName("skipped_count").IsRequired();
        builder.Property(x => x.ConflictCount).HasColumnName("conflict_count").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
        builder.HasIndex(x => new { x.AcademicYearStart, x.AcademicYearEnd }).IsUnique().HasDatabaseName(StudentDatabaseConstraints.RolloverAcademicYearUnique);
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName(StudentDatabaseConstraints.RolloverIdempotencyUnique);
        builder.ToTable(t => t.HasCheckConstraint("CK_academic_year_rollovers_year", "academic_year_end = academic_year_start + 1"));
    }
}

internal sealed class AcademicYearRolloverItemConfiguration : IEntityTypeConfiguration<AcademicYearRolloverItem>
{
    public void Configure(EntityTypeBuilder<AcademicYearRolloverItem> builder)
    {
        builder.ToTable("academic_year_rollover_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RolloverId).HasColumnName("rollover_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.ProfileVersion).HasColumnName("profile_version").IsRequired();
        builder.Property(x => x.SourceGrade).HasColumnName("source_grade");
        builder.Property(x => x.Action).HasColumnName("action").HasConversion<int>().IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(400);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.HasOne<AcademicYearRollover>().WithMany().HasForeignKey(x => x.RolloverId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.RolloverId, x.StudentId }).IsUnique();
    }
}

internal sealed class StudentEducationAuditLogConfiguration : IEntityTypeConfiguration<StudentEducationAuditLog>
{
    public void Configure(EntityTypeBuilder<StudentEducationAuditLog> builder)
    {
        builder.ToTable("student_education_audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(160).IsRequired();
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").HasMaxLength(80).IsRequired();
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").HasMaxLength(80).IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.OldValuesJson).HasColumnName("old_values_json");
        builder.Property(x => x.NewValuesJson).HasColumnName("new_values_json");
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.HasIndex(x => new { x.StudentId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.ResourceType, x.ResourceId, x.CreatedAtUtc });
    }
}

internal sealed class EducationImportBatchConfiguration : IEntityTypeConfiguration<EducationImportBatch>
{
    public void Configure(EntityTypeBuilder<EducationImportBatch> builder)
    {
        builder.ToTable("education_import_batches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Kind).HasColumnName("kind").HasConversion<int>().IsRequired();
        builder.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(EducationImportBatch.FileNameMaxLength).IsRequired();
        builder.Property(x => x.RequestedByUserId).HasColumnName("requested_by_user_id");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(x => x.TotalRows).HasColumnName("total_rows").IsRequired();
        builder.Property(x => x.ValidRows).HasColumnName("valid_rows").IsRequired();
        builder.Property(x => x.InvalidRows).HasColumnName("invalid_rows").IsRequired();
        builder.Property(x => x.CreatedRegions).HasColumnName("created_regions").IsRequired();
        builder.Property(x => x.CreatedSchools).HasColumnName("created_schools").IsRequired();
        builder.Property(x => x.SkippedSchools).HasColumnName("skipped_schools").IsRequired();
        builder.Property(x => x.SummaryJson).HasColumnName("summary_json");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc");
        builder.HasIndex(x => new { x.Kind, x.CreatedAtUtc });
    }
}

internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.IdentityUserId).HasColumnName("identity_user_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(x => x.OnboardingState).HasColumnName("onboarding_state").HasConversion<int>().IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.HasIndex(x => x.IdentityUserId).IsUnique().HasDatabaseName(StudentDatabaseConstraints.IdentityUserIdUnique);
        builder.HasOne(x => x.Profile)
            .WithOne()
            .HasForeignKey<StudentProfile>(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Profile).IsRequired();
    }
}

internal sealed class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("student_profiles");
        builder.HasKey(x => x.StudentId);
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(StudentProfile.DisplayNameMaxLength);
        builder.Property(x => x.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(StudentProfile.AvatarUrlMaxLength);
        builder.Property(x => x.DateOfBirth).HasColumnName("date_of_birth");
        builder.Property(x => x.Region).HasColumnName("region").HasMaxLength(StudentProfile.RegionMaxLength);
        builder.Property(x => x.City).HasColumnName("city").HasMaxLength(StudentProfile.CityMaxLength);
        builder.Property(x => x.SchoolName).HasColumnName("school_name").HasMaxLength(StudentProfile.SchoolNameMaxLength);
        builder.Property(x => x.Grade).HasColumnName("grade");
        builder.Property(x => x.Gender).HasColumnName("gender").HasMaxLength(StudentProfile.GenderMaxLength);
        builder.Property(x => x.TimeZoneId).HasColumnName("time_zone_id").HasMaxLength(StudentProfile.TimeZoneIdMaxLength).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
    }
}

internal sealed class StudentDailyActivityConfiguration : IEntityTypeConfiguration<StudentDailyActivity>
{
    public void Configure(EntityTypeBuilder<StudentDailyActivity> builder)
    {
        builder.ToTable("student_daily_activities");
        builder.HasKey(x => new { x.StudentId, x.LocalDate }).HasName(StudentDatabaseConstraints.DailyActivityPrimaryKey);
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.LocalDate).HasColumnName("local_date");
        builder.Property(x => x.TimeZoneId).HasColumnName("time_zone_id").HasMaxLength(StudentProfile.TimeZoneIdMaxLength).IsRequired();
        builder.Property(x => x.FirstSeenAtUtc).HasColumnName("first_seen_at_utc").IsRequired();
        builder.Property(x => x.LastSeenAtUtc).HasColumnName("last_seen_at_utc").IsRequired();
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
