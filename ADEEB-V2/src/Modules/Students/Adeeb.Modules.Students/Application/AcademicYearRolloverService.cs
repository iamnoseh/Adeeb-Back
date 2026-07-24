using System.Security.Claims;
using System.Text.Json;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Students.Domain.Education;
using Adeeb.Modules.Students.Domain.Students;
using Adeeb.Modules.Students.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Adeeb.Modules.Students.Application;

public sealed class AcademicYearRolloverService(StudentsDbContext db, IDateTimeProvider clock)
{
    public async Task<Result<AcademicYearRolloverResponse>> CreatePreviewAsync(CreateAcademicYearRolloverPreviewRequest request,
        ClaimsPrincipal actor, CancellationToken ct)
    {
        if (request.AcademicYearStart is < 2000 or > 2100 || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Trim().Length > 100)
            return Result<AcademicYearRolloverResponse>.Failure(EducationErrors.RolloverInvalid);
        var end = request.AcademicYearStart + 1;
        var existing = await db.AcademicYearRollovers.SingleOrDefaultAsync(x => x.AcademicYearStart == request.AcademicYearStart && x.AcademicYearEnd == end, ct);
        if (existing is not null) return Result<AcademicYearRolloverResponse>.Success(await ToResponseAsync(existing, ct));

        IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        await using (transaction)
        {
            var duplicate = await db.AcademicYearRollovers.SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey.Trim(), ct);
            if (duplicate is not null) return Result<AcademicYearRolloverResponse>.Success(await ToResponseAsync(duplicate, ct));
            var now = clock.UtcNow;
            var rollover = new AcademicYearRollover(Guid.NewGuid(), request.AcademicYearStart, end, request.IdempotencyKey.Trim(), now);
            db.AcademicYearRollovers.Add(rollover);
            var profiles = await db.EducationProfiles.AsNoTracking()
                .Join(db.Students.AsNoTracking(), profile => profile.StudentId, student => student.Id, (profile, student) => new { profile, student })
                .ToListAsync(ct);
            var schoolIds = profiles.Select(x => x.profile.SchoolId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
            var schools = await db.Schools.AsNoTracking().Where(x => schoolIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var promoted = 0;
            var graduated = 0;
            var skipped = 0;
            var conflicts = 0;
            foreach (var row in profiles)
            {
                var (action, reason) = Classify(row.profile, row.student, schools, request.AcademicYearStart, end);
                switch (action)
                {
                    case AcademicYearRolloverItemAction.Promote:
                        promoted++;
                        break;
                    case AcademicYearRolloverItemAction.Graduate:
                        graduated++;
                        break;
                    case AcademicYearRolloverItemAction.Skip:
                        skipped++;
                        break;
                    case AcademicYearRolloverItemAction.Conflict:
                        conflicts++;
                        break;
                    default:
                        conflicts++;
                        break;
                }

                db.AcademicYearRolloverItems.Add(new AcademicYearRolloverItem(Guid.NewGuid(), rollover.Id, row.profile.StudentId, row.profile.Version,
                    row.profile.CurrentGrade, action, reason, now));
            }
            rollover.SetPreviewCounts(promoted, graduated, skipped, conflicts);
            WriteAudit(UserId(actor), "student.rollover.preview_created", "academic_year_rollover", rollover.Id, null,
                new { rollover.AcademicYearStart, rollover.AcademicYearEnd, promoted, graduated, skipped, conflicts });
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Result<AcademicYearRolloverResponse>.Success(await ToResponseAsync(rollover, ct));
        }
    }

    public async Task<Result<AcademicYearRolloverResponse>> ApproveAsync(Guid id, ExecuteAcademicYearRolloverRequest request, ClaimsPrincipal actor, CancellationToken ct)
    {
        var rollover = await db.AcademicYearRollovers.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (rollover is null) return Result<AcademicYearRolloverResponse>.Failure(EducationErrors.RolloverNotFound);
        if (rollover.Version != request.ExpectedVersion || rollover.Status != AcademicYearRolloverStatus.Preview)
            return Result<AcademicYearRolloverResponse>.Failure(EducationErrors.RolloverInvalid);
        rollover.Approve(clock.UtcNow);
        WriteAudit(UserId(actor), "student.rollover.approved", "academic_year_rollover", rollover.Id, null, null);
        await db.SaveChangesAsync(ct);
        return Result<AcademicYearRolloverResponse>.Success(await ToResponseAsync(rollover, ct));
    }

    public async Task<Result<AcademicYearRolloverResponse>> ExecuteAsync(Guid id, ExecuteAcademicYearRolloverRequest request, ClaimsPrincipal actor, CancellationToken ct)
    {
        IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        await using (transaction)
        {
            var rollover = await db.AcademicYearRollovers.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (rollover is null) return Result<AcademicYearRolloverResponse>.Failure(EducationErrors.RolloverNotFound);
            if (rollover.Status == AcademicYearRolloverStatus.Executed) return Result<AcademicYearRolloverResponse>.Success(await ToResponseAsync(rollover, ct));
            if (rollover.Version != request.ExpectedVersion || rollover.Status != AcademicYearRolloverStatus.Approved)
                return Result<AcademicYearRolloverResponse>.Failure(EducationErrors.RolloverInvalid);

            var now = clock.UtcNow;
            var items = await db.AcademicYearRolloverItems.Where(x => x.RolloverId == rollover.Id).ToListAsync(ct);
            var studentIds = items.Select(x => x.StudentId).Distinct().ToArray();
            var profiles = await db.EducationProfiles.Where(x => studentIds.Contains(x.StudentId)).ToDictionaryAsync(x => x.StudentId, ct);
            var students = await db.Students.Include(x => x.Profile).Where(x => studentIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var schoolIds = profiles.Values.Where(x => x.SchoolId.HasValue).Select(x => x.SchoolId!.Value).Distinct().ToArray();
            var schools = await db.Schools.Where(x => schoolIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var regionIds = profiles.Values.Where(x => x.ResidenceRegionId.HasValue).Select(x => x.ResidenceRegionId!.Value)
                .Concat(schools.Values.Select(x => x.RegionId)).Distinct().ToArray();
            var regions = await db.Regions.Where(x => regionIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var enrollments = await db.SchoolEnrollments.Where(x => studentIds.Contains(x.StudentId) && x.IsCurrent).ToDictionaryAsync(x => x.StudentId, ct);
            var promoted = 0;
            var graduated = 0;
            var skipped = 0;
            var conflicts = 0;
            foreach (var item in items)
            {
                if (item.Action == AcademicYearRolloverItemAction.Skip) { skipped++; continue; }
                if (item.Action == AcademicYearRolloverItemAction.Conflict) { conflicts++; continue; }
                if (!profiles.TryGetValue(item.StudentId, out var profile) || profile.Version != item.ProfileVersion || profile.Status != EducationStatus.Studying ||
                    profile.AcademicYearStart != rollover.AcademicYearStart)
                { conflicts++; continue; }
                if (item.Action == AcademicYearRolloverItemAction.Graduate)
                {
                    if (profile.CurrentGrade != 11 || profile.SchoolId is not Guid graduateSchoolId || !schools.TryGetValue(graduateSchoolId, out var graduateSchool) ||
                        graduateSchool.Status != SchoolStatus.Verified)
                    { conflicts++; continue; }

                    profile.Graduate(now);
                    if (enrollments.TryGetValue(item.StudentId, out var enrollment)) enrollment.End("academic_year_graduation", now);
                    if (students.TryGetValue(item.StudentId, out var graduateStudent))
                    {
                        var graduateRegion = profile.ResidenceRegionId.HasValue && regions.TryGetValue(profile.ResidenceRegionId.Value, out var residence)
                            ? residence
                            : null;
                        graduateStudent.Profile.UpdateEducationSnapshot(graduateRegion?.NameTg, graduateRegion?.NameTg,
                            graduateSchool.NameTg ?? graduateSchool.NameRu, 11, now);
                    }

                    graduated++;
                    continue;
                }
                if (profile.CurrentGrade is null || profile.CurrentGrade.Value is < 1 or > 10 || profile.SchoolId is not Guid schoolId || !schools.TryGetValue(schoolId, out var school) ||
                    school.Status != SchoolStatus.Verified || profile.ResidenceRegionId is null)
                { conflicts++; continue; }
                var nextGrade = (short)(profile.CurrentGrade.Value + 1);
                var nextStart = rollover.AcademicYearEnd;
                var nextEnd = nextStart + 1;
                profile.Promote(nextGrade, nextStart, nextEnd, AcademicYearService.ExpectedGraduationYear(nextEnd, nextGrade), now);
                if (enrollments.TryGetValue(item.StudentId, out var currentEnrollment)) currentEnrollment.End("academic_year_promotion", now);
                db.SchoolEnrollments.Add(new StudentSchoolEnrollment(Guid.NewGuid(), item.StudentId, schoolId, school.RegionId, nextGrade,
                    nextStart, nextEnd, EnrollmentSource.AcademicRollover, null, now));
                if (students.TryGetValue(item.StudentId, out var promotedStudent))
                {
                    var residenceRegion = profile.ResidenceRegionId.HasValue && regions.TryGetValue(profile.ResidenceRegionId.Value, out var residence)
                        ? residence
                        : null;
                    promotedStudent.Profile.UpdateEducationSnapshot(residenceRegion?.NameTg, residenceRegion?.NameTg, school.NameTg ?? school.NameRu, nextGrade, now);
                }

                promoted++;
            }
            rollover.Complete(UserId(actor), promoted, graduated, skipped, conflicts, now);
            WriteAudit(UserId(actor), "student.rollover.executed", "academic_year_rollover", rollover.Id, null,
                new { promoted, graduated, skipped, conflicts });
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Result<AcademicYearRolloverResponse>.Success(await ToResponseAsync(rollover, ct));
        }
    }

    public async Task<Result<AcademicYearRolloverResponse>> GetAsync(Guid id, CancellationToken ct)
    {
        var rollover = await db.AcademicYearRollovers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return rollover is null ? Result<AcademicYearRolloverResponse>.Failure(EducationErrors.RolloverNotFound) : Result<AcademicYearRolloverResponse>.Success(await ToResponseAsync(rollover, ct));
    }

    private static (AcademicYearRolloverItemAction Action, string? Reason) Classify(
        StudentEducationProfile profile,
        Student student,
        IReadOnlyDictionary<Guid, School> schools,
        int academicYearStart,
        int academicYearEnd)
    {
        if (student.Status != StudentStatus.Active) return (AcademicYearRolloverItemAction.Skip, "student_not_active");
        if (profile.Status == EducationStatus.Incomplete) return (AcademicYearRolloverItemAction.Skip, "profile_incomplete");
        if (profile.Status == EducationStatus.PendingSchoolReview) return (AcademicYearRolloverItemAction.Skip, "pending_school_review");
        if (profile.Status == EducationStatus.Graduated) return (AcademicYearRolloverItemAction.Skip, "already_graduated");
        if (profile.Status == EducationStatus.LeftSchool) return (AcademicYearRolloverItemAction.Skip, "left_school");
        if (profile.Status != EducationStatus.Studying) return (AcademicYearRolloverItemAction.Skip, "not_studying");
        if (profile.AcademicYearStart != academicYearStart || profile.AcademicYearEnd != academicYearEnd) return (AcademicYearRolloverItemAction.Conflict, "academic_year_mismatch");
        if (profile.SchoolId is not Guid schoolId || !schools.TryGetValue(schoolId, out var school)) return (AcademicYearRolloverItemAction.Conflict, "school_missing");
        if (school.Status != SchoolStatus.Verified) return (AcademicYearRolloverItemAction.Conflict, "school_not_verified");
        if (profile.CurrentGrade is null) return (AcademicYearRolloverItemAction.Conflict, "grade_missing");
        return profile.CurrentGrade.Value switch
        {
            >= 1 and <= 10 => (AcademicYearRolloverItemAction.Promote, null),
            11 => (AcademicYearRolloverItemAction.Graduate, null),
            _ => (AcademicYearRolloverItemAction.Conflict, "grade_invalid")
        };
    }

    private void WriteAudit(Guid? actorId, string action, string resourceType, Guid resourceId, Guid? studentId, object? values)
    {
        db.EducationAuditLogs.Add(new StudentEducationAuditLog(Guid.NewGuid(), actorId, action, resourceType, resourceId.ToString(), studentId,
            null, values is null ? null : JsonSerializer.Serialize(values), null, clock.UtcNow));
    }

    private static Guid? UserId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) ? id : null;
    private async Task<AcademicYearRolloverResponse> ToResponseAsync(AcademicYearRollover rollover, CancellationToken ct)
    {
        var items = await db.AcademicYearRolloverItems.AsNoTracking().Where(x => x.RolloverId == rollover.Id)
            .OrderBy(x => x.Action).ThenBy(x => x.StudentId)
            .Select(x => new AcademicYearRolloverItemResponse(x.Id, x.StudentId, x.SourceGrade, x.Action.ToString(), x.Reason))
            .ToListAsync(ct);
        return ToResponse(rollover, items);
    }

    private static AcademicYearRolloverResponse ToResponse(AcademicYearRollover rollover) => ToResponse(rollover, []);

    private static AcademicYearRolloverResponse ToResponse(AcademicYearRollover rollover, IReadOnlyList<AcademicYearRolloverItemResponse> items) => new(rollover.Id, rollover.AcademicYearStart,
        rollover.AcademicYearEnd, rollover.Status.ToString(), rollover.PromotedCount, rollover.GraduatedCount, rollover.SkippedCount,
        rollover.ConflictCount, rollover.PreviewCreatedAtUtc, rollover.ExecutedAtUtc, rollover.Version, items);
}
