using Adeeb.Modules.Mmt.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence;

public sealed class MmtDbContext(DbContextOptions<MmtDbContext> options) : DbContext(options)
{
    public DbSet<MmtCluster> Clusters => Set<MmtCluster>();
    public DbSet<MmtClusterSubject> ClusterSubjects => Set<MmtClusterSubject>();
    public DbSet<University> Universities => Set<University>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<AdmissionProgram> AdmissionPrograms => Set<AdmissionProgram>();
    public DbSet<PassingScoreHistory> PassingScores => Set<PassingScoreHistory>();
    public DbSet<StudentMmtProfile> StudentProfiles => Set<StudentMmtProfile>();
    public DbSet<StudentAdmissionChoice> StudentAdmissionChoices => Set<StudentAdmissionChoice>();
    public DbSet<MmtExamEvaluation> ExamEvaluations => Set<MmtExamEvaluation>();
    public DbSet<MmtAdmissionChoiceSnapshot> AdmissionChoiceSnapshots => Set<MmtAdmissionChoiceSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("mmt");
        modelBuilder.ApplyConfiguration(new MmtClusterConfiguration());
        modelBuilder.ApplyConfiguration(new MmtClusterSubjectConfiguration());
        modelBuilder.ApplyConfiguration(new UniversityConfiguration());
        modelBuilder.ApplyConfiguration(new SpecialtyConfiguration());
        modelBuilder.ApplyConfiguration(new AdmissionProgramConfiguration());
        modelBuilder.ApplyConfiguration(new PassingScoreConfiguration());
        modelBuilder.ApplyConfiguration(new StudentMmtProfileConfiguration());
        modelBuilder.ApplyConfiguration(new StudentAdmissionChoiceConfiguration());
        modelBuilder.ApplyConfiguration(new MmtExamEvaluationConfiguration());
        modelBuilder.ApplyConfiguration(new MmtAdmissionChoiceSnapshotConfiguration());
    }
}

internal sealed class MmtClusterConfiguration : IEntityTypeConfiguration<MmtCluster>
{
    public void Configure(EntityTypeBuilder<MmtCluster> b)
    {
        b.ToTable("clusters");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        b.Property(x => x.NameRu).HasColumnName("name_ru").HasMaxLength(160).IsRequired();
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
        b.Property(x => x.DescriptionRu).HasColumnName("description_ru").HasMaxLength(2000);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_mmt_clusters_code");
        b.HasIndex(x => new { x.IsActive, x.Name }).HasDatabaseName("ix_mmt_clusters_active_name");
        b.HasMany(x => x.Subjects).WithOne(x => x.MmtCluster).HasForeignKey(x => x.MmtClusterId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MmtClusterSubjectConfiguration : IEntityTypeConfiguration<MmtClusterSubject>
{
    public void Configure(EntityTypeBuilder<MmtClusterSubject> b)
    {
        b.ToTable("cluster_subjects");
        b.HasKey(x => new { x.MmtClusterId, x.SubjectId });
        b.Property(x => x.MmtClusterId).HasColumnName("cluster_id");
        b.Property(x => x.SubjectId).HasColumnName("subject_id");
        b.HasIndex(x => x.SubjectId).HasDatabaseName("ix_mmt_cluster_subjects_subject_id");
    }
}

internal sealed class UniversityConfiguration : IEntityTypeConfiguration<University>
{
    public void Configure(EntityTypeBuilder<University> b)
    {
        b.ToTable("universities");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(300).IsRequired();
        b.Property(x => x.FullNameRu).HasColumnName("full_name_ru").HasMaxLength(300).IsRequired();
        b.Property(x => x.NormalizedFullName).HasColumnName("normalized_full_name").HasMaxLength(300).IsRequired();
        b.Property(x => x.ShortName).HasColumnName("short_name").HasMaxLength(120);
        b.Property(x => x.ShortNameRu).HasColumnName("short_name_ru").HasMaxLength(120);
        b.Property(x => x.City).HasColumnName("city").HasMaxLength(120).IsRequired();
        b.Property(x => x.CityRu).HasColumnName("city_ru").HasMaxLength(120).IsRequired();
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.LogoUrl).HasColumnName("logo_url").HasMaxLength(512);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => x.NormalizedFullName).IsUnique().HasDatabaseName("ux_mmt_universities_normalized_name");
        b.HasIndex(x => new { x.IsActive, x.FullName }).HasDatabaseName("ix_mmt_universities_active_name");
    }
}

internal sealed class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> b)
    {
        b.ToTable("specialties");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(60).IsRequired();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(240).IsRequired();
        b.Property(x => x.NameRu).HasColumnName("name_ru").HasMaxLength(240).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
        b.Property(x => x.DescriptionRu).HasColumnName("description_ru").HasMaxLength(2000);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_mmt_specialties_code");
        b.HasIndex(x => new { x.IsActive, x.Name }).HasDatabaseName("ix_mmt_specialties_active_name");
    }
}

internal sealed class AdmissionProgramConfiguration : IEntityTypeConfiguration<AdmissionProgram>
{
    public void Configure(EntityTypeBuilder<AdmissionProgram> b)
    {
        b.ToTable("admission_programs", t =>
        {
            t.HasCheckConstraint("ck_mmt_program_year", "admission_year >= 2000 AND admission_year <= 2100");
            t.HasCheckConstraint("ck_mmt_program_seats", "seats_count IS NULL OR seats_count >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UniversityId).HasColumnName("university_id");
        b.Property(x => x.SpecialtyId).HasColumnName("specialty_id");
        b.Property(x => x.MmtClusterId).HasColumnName("cluster_id");
        b.Property(x => x.AdmissionType).HasColumnName("admission_type").HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.StudyForm).HasColumnName("study_form").HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.StudyLanguage).HasColumnName("study_language").HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.AdmissionYear).HasColumnName("admission_year");
        b.Property(x => x.SeatsCount).HasColumnName("seats_count");
        b.Property(x => x.IsPublished).HasColumnName("is_published");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => new { x.UniversityId, x.SpecialtyId, x.MmtClusterId, x.AdmissionType, x.StudyForm, x.StudyLanguage, x.AdmissionYear })
            .IsUnique().HasDatabaseName("ux_mmt_admission_program_identity");
        b.HasIndex(x => new { x.MmtClusterId, x.AdmissionYear, x.IsPublished, x.IsActive }).HasDatabaseName("ix_mmt_program_student_lookup");
        b.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Specialty).WithMany().HasForeignKey(x => x.SpecialtyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MmtCluster).WithMany().HasForeignKey(x => x.MmtClusterId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.PassingScores).WithOne(x => x.AdmissionProgram).HasForeignKey(x => x.AdmissionProgramId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.PassingScores).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PassingScoreConfiguration : IEntityTypeConfiguration<PassingScoreHistory>
{
    public void Configure(EntityTypeBuilder<PassingScoreHistory> b)
    {
        b.ToTable("passing_score_history", t =>
        {
            t.HasCheckConstraint("ck_mmt_score_year", "year >= 2000 AND year <= 2100");
            t.HasCheckConstraint("ck_mmt_score_value", "passing_score > 0 AND passing_score <= 1000 AND scale(passing_score) <= 2");
            t.HasCheckConstraint("ck_mmt_score_seats", "seats_count IS NULL OR seats_count >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.AdmissionProgramId).HasColumnName("admission_program_id");
        b.Property(x => x.Year).HasColumnName("year");
        b.Property(x => x.PassingScore).HasColumnName("passing_score").HasColumnType("numeric");
        b.Property(x => x.SeatsCount).HasColumnName("seats_count");
        b.Property(x => x.Source).HasColumnName("source").HasMaxLength(500);
        b.Property(x => x.Note).HasColumnName("note").HasMaxLength(2000);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => new { x.AdmissionProgramId, x.Year }).IsUnique().HasDatabaseName("ux_mmt_score_program_year");
        b.HasIndex(x => new { x.AdmissionProgramId, x.Year }).IsDescending(false, true).HasDatabaseName("ix_mmt_score_latest");
    }
}
