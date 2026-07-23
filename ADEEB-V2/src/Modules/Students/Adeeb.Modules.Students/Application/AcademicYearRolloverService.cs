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
        if (existing is not null) return Result<AcademicYearRolloverResponse>.Success(ToResponse(existing));

        IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        await using (transaction)
        {
            var duplicate = await db.AcademicYearRollovers.SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey.Trim(), ct);
            if (duplicate is not null) return Result<AcademicYearRolloverResponse>.Success(ToResponse(duplicate));
            var now = clock.UtcNow;
            var rollover = new AcademicYearRollover(Guid.NewGuid(), request.AcademicYearStart, end, request.IdempotencyKey.Trim(), now);
            db.AcademicYearRollovers.Add(rollover);
            var candidates = await db.EducationProfiles.AsNoTracking()
                .Join(db.Students.AsNoTracking(), profile => profile.StudentId, student => student.Id, (profile, student) => new { profile, student })
                .Where(x => x.student.Status == StudentStatus.Active && x.profile.Status == EducationStatus.Studying &&
                    x.profile.AcademicYearStart == request.AcademicYearStart && x.profile.AcademicYearEnd == end)
                .Select(x => x.profile).ToListAsync(ct);
            foreach (var profile in candidates)
            {
                var action = profile.CurrentGrade switch
                {
                    >= 1 and <= 10 => AcademicYearRolloverItemAction.Promote,
                    11 => AcademicYearRolloverItemAction.Graduate,
                    _ => AcademicYearRolloverItemAction.Skip
                };
                var reason = action == AcademicYearRolloverItemAction.Skip ? "incomplete_or_invalid_profile" : null;
                db.AcademicYearRolloverItems.Add(new AcademicYearRolloverItem(Guid.NewGuid(), rollover.Id, profile.StudentId, profile.Version,
                    profile.CurrentGrade, action, reason, now));
            }
            WriteAudit(UserId(actor), "student.rollover.preview_created", "academic_year_rollover", rollover.Id, null,
                new { rollover.AcademicYearStart, rollover.AcademicYearEnd, candidates = candidates.Count });
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Result<AcademicYearRolloverResponse>.Success(ToResponse(rollover));
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
        return Result<AcademicYearRolloverResponse>.Success(ToResponse(rollover));
    }

    public async Task<Result<AcademicYearRolloverResponse>> ExecuteAsync(Guid id, ExecuteAcademicYearRolloverRequest request, ClaimsPrincipal actor, CancellationToken ct)
    {
        IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        await using (transaction)
        {
            var rollover = await db.AcademicYearRollovers.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (rollover is null) return Result<AcademicYearRolloverResponse>.Failure(EducationErrors.RolloverNotFound);
            if (rollover.Status == AcademicYearRolloverStatus.Executed) return Result<AcademicYearRolloverResponse>.Success(ToResponse(rollover));
            if (rollover.Version != request.ExpectedVersion || rollover.Status != AcademicYearRolloverStatus.Approved)
                return Result<AcademicYearRolloverResponse>.Failure(EducationErrors.RolloverInvalid);

            var now = clock.UtcNow;
            var items = await db.AcademicYearRolloverItems.Where(x => x.RolloverId == rollover.Id).ToListAsync(ct);
            var studentIds = items.Select(x => x.StudentId).Distinct().ToArray();
            var profiles = await db.EducationProfiles.Where(x => studentIds.Contains(x.StudentId)).ToDictionaryAsync(x => x.StudentId, ct);
            var schoolIds = profiles.Values.Where(x => x.SchoolId.HasValue).Select(x => x.SchoolId!.Value).Distinct().ToArray();
            var schools = await db.Schools.Where(x => schoolIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var enrollments = await db.SchoolEnrollments.Where(x => studentIds.Contains(x.StudentId) && x.IsCurrent).ToDictionaryAsync(x => x.StudentId, ct);
            var promoted = 0;
            var graduated = 0;
            var skipped = 0;
            var conflicts = 0;
            foreach (var item in items)
            {
                if (item.Action == AcademicYearRolloverItemAction.Skip) { skipped++; continue; }
                if (!profiles.TryGetValue(item.StudentId, out var profile) || profile.Version != item.ProfileVersion || profile.Status != EducationStatus.Studying ||
                    profile.AcademicYearStart != rollover.AcademicYearStart)
                { conflicts++; continue; }
                if (item.Action == AcademicYearRolloverItemAction.Graduate)
                {
                    profile.Graduate(now);
                    if (enrollments.TryGetValue(item.StudentId, out var enrollment)) enrollment.End("academic_year_graduation", now);
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
                promoted++;
            }
            rollover.Complete(UserId(actor), promoted, graduated, skipped, conflicts, now);
            WriteAudit(UserId(actor), "student.rollover.executed", "academic_year_rollover", rollover.Id, null,
                new { promoted, graduated, skipped, conflicts });
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Result<AcademicYearRolloverResponse>.Success(ToResponse(rollover));
        }
    }

    public async Task<Result<AcademicYearRolloverResponse>> GetAsync(Guid id, CancellationToken ct)
    {
        var rollover = await db.AcademicYearRollovers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return rollover is null ? Result<AcademicYearRolloverResponse>.Failure(EducationErrors.RolloverNotFound) : Result<AcademicYearRolloverResponse>.Success(ToResponse(rollover));
    }

    private void WriteAudit(Guid? actorId, string action, string resourceType, Guid resourceId, Guid? studentId, object? values)
    {
        db.EducationAuditLogs.Add(new StudentEducationAuditLog(Guid.NewGuid(), actorId, action, resourceType, resourceId.ToString(), studentId,
            null, values is null ? null : JsonSerializer.Serialize(values), null, clock.UtcNow));
    }

    private static Guid? UserId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) ? id : null;
    private static AcademicYearRolloverResponse ToResponse(AcademicYearRollover rollover) => new(rollover.Id, rollover.AcademicYearStart,
        rollover.AcademicYearEnd, rollover.Status.ToString(), rollover.PromotedCount, rollover.GraduatedCount, rollover.SkippedCount,
        rollover.ConflictCount, rollover.PreviewCreatedAtUtc, rollover.ExecutedAtUtc, rollover.Version);
}
