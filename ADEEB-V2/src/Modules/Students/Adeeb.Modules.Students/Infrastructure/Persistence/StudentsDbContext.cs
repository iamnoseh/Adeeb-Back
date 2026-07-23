using Adeeb.Modules.Students.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adeeb.Modules.Students.Infrastructure.Persistence;

public sealed class StudentsDbContext(DbContextOptions<StudentsDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentDailyActivity> DailyActivities => Set<StudentDailyActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("students");
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
        modelBuilder.ApplyConfiguration(new StudentProfileConfiguration());
        modelBuilder.ApplyConfiguration(new StudentDailyActivityConfiguration());
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
