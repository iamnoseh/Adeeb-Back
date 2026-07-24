using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Students.Application;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Students.Domain.Education;
using Adeeb.Modules.Students.Domain.Students;
using Adeeb.Modules.Students.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adeeb.Students.Tests;

public sealed class EducationHardeningTests
{
    [Fact]
    public void Studying_profile_sets_and_preserves_first_completion_timestamp()
    {
        var first = new DateTimeOffset(2026, 7, 24, 6, 0, 0, TimeSpan.Zero);
        var second = first.AddHours(2);
        var profile = new StudentEducationProfile(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 7, 2026, 2027, 2030,
            EducationStatus.Studying, null, first);

        profile.SetStudying(Guid.NewGuid(), Guid.NewGuid(), 8, 2026, 2027, 2029, null, second);

        Assert.Equal(first, profile.ProfileCompletedAtUtc);
        Assert.Null(profile.GraduatedAtUtc);
    }

    [Fact]
    public void Graduation_sets_graduation_timestamp_without_changing_grade_to_twelve()
    {
        var first = new DateTimeOffset(2026, 7, 24, 6, 0, 0, TimeSpan.Zero);
        var graduated = first.AddDays(1);
        var profile = new StudentEducationProfile(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 11, 2026, 2027, 2027,
            EducationStatus.Studying, null, first);

        profile.Graduate(graduated);

        Assert.Equal(EducationStatus.Graduated, profile.Status);
        Assert.Equal((short)11, profile.CurrentGrade);
        Assert.Equal(graduated, profile.GraduatedAtUtc);
    }

    [Fact]
    public async Task Suggestion_approval_closes_previous_current_enrollment_before_creating_new_one()
    {
        await using var db = CreateDb();
        var clock = new FixedClock();
        var service = new StudentEducationService(db, new AcademicYearService(clock), clock, NullLogger<StudentEducationService>.Instance);
        var student = new Student(Guid.NewGuid(), Guid.NewGuid(), clock.UtcNow);
        var region = CreateRegion("TJ", "Tajikistan", clock.UtcNow);
        var oldSchool = CreateVerifiedSchool(region.Id, "Old school", 1, clock.UtcNow);
        var newSchool = CreateVerifiedSchool(region.Id, "New school", 2, clock.UtcNow);
        var suggestion = new SchoolSuggestion(Guid.NewGuid(), student.Id, "New school", 2, region.Id, Key("New school"), null, clock.UtcNow);
        var profile = new StudentEducationProfile(student.Id, region.Id, null, suggestion.Id, 9, 2026, 2027, 2029,
            EducationStatus.PendingSchoolReview, null, clock.UtcNow);
        db.AddRange(student, region, oldSchool, newSchool, suggestion, profile);
        db.SchoolEnrollments.Add(new StudentSchoolEnrollment(Guid.NewGuid(), student.Id, oldSchool.Id, region.Id, 9, 2026, 2027,
            EnrollmentSource.StudentProfile, null, clock.UtcNow));
        await db.SaveChangesAsync();

        var result = await service.ReviewSuggestionAsync(suggestion.Id, new ReviewSchoolSuggestionRequest(newSchool.Id, null, false, null, 0),
            TestPrincipal.ForUser(Guid.NewGuid()), russian: false, correlationId: "trace-test", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var enrollments = await db.SchoolEnrollments.Where(x => x.StudentId == student.Id).ToListAsync();
        Assert.Single(enrollments, x => x.IsCurrent);
        Assert.Equal(newSchool.Id, enrollments.Single(x => x.IsCurrent).SchoolId);
        Assert.False(enrollments.Single(x => x.SchoolId == oldSchool.Id).IsCurrent);
        Assert.Equal("school_suggestion_approved", enrollments.Single(x => x.SchoolId == oldSchool.Id).ChangeReason);
        Assert.Equal("trace-test", await db.EducationAuditLogs.Where(x => x.Action == "student.school_suggestion.approved").Select(x => x.CorrelationId).SingleAsync());
    }

    [Fact]
    public async Task Rollover_preview_reports_promote_graduate_skip_and_conflict()
    {
        await using var db = CreateDb();
        var clock = new FixedClock();
        var service = new AcademicYearRolloverService(db, clock);
        var region = CreateRegion("TJ", "Tajikistan", clock.UtcNow);
        var verifiedSchool = CreateVerifiedSchool(region.Id, "Verified", 1, clock.UtcNow);
        var archivedSchool = CreateVerifiedSchool(region.Id, "Archived", 2, clock.UtcNow);
        archivedSchool.Archive(null, clock.UtcNow);
        db.AddRange(region, verifiedSchool, archivedSchool);
        AddStudentWithProfile(db, region.Id, verifiedSchool.Id, 10, EducationStatus.Studying, 2026, 2027);
        AddStudentWithProfile(db, region.Id, verifiedSchool.Id, 11, EducationStatus.Studying, 2026, 2027);
        AddStudentWithProfile(db, region.Id, null, 8, EducationStatus.PendingSchoolReview, 2026, 2027);
        AddStudentWithProfile(db, region.Id, archivedSchool.Id, 7, EducationStatus.Studying, 2026, 2027);
        AddStudentWithProfile(db, region.Id, verifiedSchool.Id, 6, EducationStatus.Studying, 2025, 2026);
        await db.SaveChangesAsync();

        var result = await service.CreatePreviewAsync(new CreateAcademicYearRolloverPreviewRequest(2026, "rollover-2026"),
            TestPrincipal.ForUser(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.PromotedCount);
        Assert.Equal(1, result.Value.GraduatedCount);
        Assert.Equal(1, result.Value.SkippedCount);
        Assert.Equal(2, result.Value.ConflictCount);
        Assert.Contains(result.Value.Items, x => x.Action == AcademicYearRolloverItemAction.Conflict.ToString() && x.Reason == "school_not_verified");
        Assert.Contains(result.Value.Items, x => x.Action == AcademicYearRolloverItemAction.Conflict.ToString() && x.Reason == "academic_year_mismatch");
    }

    [Fact]
    public async Task Rollover_execute_graduates_grade_eleven_without_legacy_grade_twelve()
    {
        await using var db = CreateDb();
        var clock = new FixedClock();
        var service = new AcademicYearRolloverService(db, clock);
        var region = CreateRegion("TJ", "Tajikistan", clock.UtcNow);
        var school = CreateVerifiedSchool(region.Id, "Verified", 1, clock.UtcNow);
        db.AddRange(region, school);
        var student = AddStudentWithProfile(db, region.Id, school.Id, 11, EducationStatus.Studying, 2026, 2027);
        db.SchoolEnrollments.Add(new StudentSchoolEnrollment(Guid.NewGuid(), student.Id, school.Id, region.Id, 11, 2026, 2027,
            EnrollmentSource.StudentProfile, null, clock.UtcNow));
        await db.SaveChangesAsync();
        var preview = await service.CreatePreviewAsync(new CreateAcademicYearRolloverPreviewRequest(2026, "execute-2026"),
            TestPrincipal.ForUser(Guid.NewGuid()), CancellationToken.None);
        var approved = await service.ApproveAsync(preview.Value!.Id, new ExecuteAcademicYearRolloverRequest(preview.Value.Version),
            TestPrincipal.ForUser(Guid.NewGuid()), CancellationToken.None);

        var executed = await service.ExecuteAsync(preview.Value.Id, new ExecuteAcademicYearRolloverRequest(approved.Value!.Version),
            TestPrincipal.ForUser(Guid.NewGuid()), CancellationToken.None);

        Assert.True(executed.IsSuccess);
        var profile = await db.EducationProfiles.SingleAsync(x => x.StudentId == student.Id);
        Assert.Equal(EducationStatus.Graduated, profile.Status);
        Assert.Equal((short)11, profile.CurrentGrade);
        Assert.NotNull(profile.GraduatedAtUtc);
        Assert.Equal((short)11, (await db.Students.Include(x => x.Profile).SingleAsync(x => x.Id == student.Id)).Profile.Grade);
        Assert.False((await db.SchoolEnrollments.SingleAsync(x => x.StudentId == student.Id)).IsCurrent);
    }

    private static StudentsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<StudentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StudentsDbContext(options);
    }

    private static Region CreateRegion(string nameTg, string nameRu, DateTimeOffset now)
    {
        var id = Guid.NewGuid();
        var region = new Region(id, null, RegionType.Country, nameTg, nameRu, Key(nameTg), Key(nameRu), 0, [id], 0, now);
        region.SetPaths(nameTg, nameRu, [id], 0, now);
        return region;
    }

    private static School CreateVerifiedSchool(Guid regionId, string nameRu, int number, DateTimeOffset now)
    {
        var school = new School(Guid.NewGuid(), regionId, null, nameRu, null, number, SchoolType.GeneralSchool,
            Key(nameRu), $"{Key(nameRu)} {number}", null, null, now);
        school.Verify(null, now);
        return school;
    }

    private static Student AddStudentWithProfile(StudentsDbContext db, Guid regionId, Guid? schoolId, short grade, EducationStatus status, int start, int end)
    {
        var now = new DateTimeOffset(2026, 7, 24, 6, 0, 0, TimeSpan.Zero);
        var student = new Student(Guid.NewGuid(), Guid.NewGuid(), now);
        var profile = new StudentEducationProfile(student.Id, regionId, schoolId, status == EducationStatus.PendingSchoolReview ? Guid.NewGuid() : null,
            grade, start, end, end + (11 - grade), status, null, now);
        db.AddRange(student, profile);
        return student;
    }

    private static string Key(string value) => value.Trim().ToLowerInvariant();

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 7, 24, 6, 0, 0, TimeSpan.Zero);
        public DateTimeOffset DushanbeNow => UtcNow.ToOffset(TimeSpan.FromHours(5));
        public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(5));
    }
}
