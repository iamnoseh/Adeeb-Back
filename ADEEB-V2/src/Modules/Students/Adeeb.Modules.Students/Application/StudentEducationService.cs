using System.Security.Claims;
using System.Text.Json;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Students.Domain.Education;
using Adeeb.Modules.Students.Domain.Students;
using Adeeb.Modules.Students.Infrastructure.Persistence;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Adeeb.Modules.Students.Application;

public sealed class StudentEducationService(
    StudentsDbContext db,
    AcademicYearService academicYears,
    IDateTimeProvider clock,
    ILogger<StudentEducationService> logger)
{
    public async Task<Result<StudentEducationProfileResponse>> GetCurrentAsync(ClaimsPrincipal principal, bool russian, CancellationToken ct)
    {
        var student = await FindStudentAsync(principal, ct);
        var accessError = AccessError(student);
        if (accessError is not null) return Result<StudentEducationProfileResponse>.Failure(accessError);
        return Result<StudentEducationProfileResponse>.Success(await BuildResponseAsync(student!.Id, russian, ct));
    }

    public async Task<Result<StudentEducationProfileResponse>> UpsertCurrentAsync(ClaimsPrincipal principal,
        UpsertStudentEducationProfileRequest request, bool russian, CancellationToken ct)
    {
        if (request.ResidenceRegionId == Guid.Empty || request.SchoolId == Guid.Empty || request.CurrentGrade is < StudentProfile.MinGrade or > StudentProfile.MaxGrade ||
            !ValidOptional(request.AddressText, School.AddressTextMaxLength))
            return Result<StudentEducationProfileResponse>.ValidationFailure(Invalid("education", "student.education.invalid", "Student.Education.Invalid"));
        var identityUserId = UserId(principal);
        if (identityUserId is null) return Result<StudentEducationProfileResponse>.Failure(StudentErrors.ProvisioningRequired);

        IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        await using (transaction)
        {
            var student = await db.Students.Include(x => x.Profile).SingleOrDefaultAsync(x => x.IdentityUserId == identityUserId, ct);
            var accessError = AccessError(student);
            if (accessError is not null) return Result<StudentEducationProfileResponse>.Failure(accessError);
            var currentStudent = student!;
            var region = await db.Regions.SingleOrDefaultAsync(x => x.Id == request.ResidenceRegionId, ct);
            if (region is null) return Result<StudentEducationProfileResponse>.Failure(EducationErrors.RegionNotFound);
            if (!region.IsActive) return Result<StudentEducationProfileResponse>.Failure(EducationErrors.RegionInactive);
            var school = await db.Schools.SingleOrDefaultAsync(x => x.Id == request.SchoolId, ct);
            if (school is null) return Result<StudentEducationProfileResponse>.Failure(EducationErrors.SchoolNotFound);
            if (!school.IsSelectableByStudent) return Result<StudentEducationProfileResponse>.Failure(EducationErrors.SchoolNotSelectable);

            var profile = await db.EducationProfiles.SingleOrDefaultAsync(x => x.StudentId == currentStudent.Id, ct);
            if (profile is not null && request.ExpectedVersion.HasValue && profile.Version != request.ExpectedVersion.Value)
                return Result<StudentEducationProfileResponse>.Failure(EducationErrors.ProfileConflict);
            if (profile is null && request.ExpectedVersion.HasValue)
                return Result<StudentEducationProfileResponse>.Failure(EducationErrors.ProfileConflict);

            var now = clock.UtcNow;
            var year = academicYears.Current();
            var graduation = AcademicYearService.ExpectedGraduationYear(year.End, request.CurrentGrade);
            var previousSchoolId = profile?.SchoolId;
            var previousGrade = profile?.CurrentGrade;
            var changed = profile is null || previousSchoolId != school.Id || previousGrade != request.CurrentGrade ||
                profile!.ResidenceRegionId != region.Id || profile.AcademicYearStart != year.Start;
            if (profile is null)
            {
                profile = new StudentEducationProfile(currentStudent.Id, region.Id, school.Id, null, request.CurrentGrade, year.Start, year.End,
                    graduation, EducationStatus.Studying, Trim(request.AddressText), now);
                db.EducationProfiles.Add(profile);
            }
            else
            {
                profile.SetStudying(region.Id, school.Id, request.CurrentGrade, year.Start, year.End, graduation, Trim(request.AddressText), now);
            }

            if (changed)
            {
                var currentEnrollment = await db.SchoolEnrollments.SingleOrDefaultAsync(x => x.StudentId == currentStudent.Id && x.IsCurrent, ct);
                currentEnrollment?.End("student_profile_updated", now);
                db.SchoolEnrollments.Add(new StudentSchoolEnrollment(Guid.NewGuid(), currentStudent.Id, school.Id, school.RegionId, request.CurrentGrade,
                    year.Start, year.End, EnrollmentSource.StudentProfile, null, now));
            }
            currentStudent.Profile.UpdateEducationSnapshot(region.NameTg, region.NameTg, school.NameTg ?? school.NameRu, request.CurrentGrade, now);
            WriteAudit(UserId(principal), "student.education.updated", "education_profile", currentStudent.Id, currentStudent.Id,
                new { schoolId = previousSchoolId, grade = previousGrade }, new { schoolId = school.Id, grade = request.CurrentGrade, year.Start });
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            logger.LogInformation("student.education.updated student_id={StudentId} school_id={SchoolId} grade={Grade}", currentStudent.Id, school.Id, request.CurrentGrade);
            return Result<StudentEducationProfileResponse>.Success(await BuildResponseAsync(currentStudent.Id, russian, ct));
        }
    }

    public async Task<Result<SchoolSuggestionResponse>> CreateSuggestionAsync(ClaimsPrincipal principal, CreateSchoolSuggestionRequest request,
        bool russian, CancellationToken ct)
    {
        if (request.ResidenceRegionId == Guid.Empty || !ValidName(request.SuggestedName, SchoolSuggestion.NameMaxLength) || request.SuggestedNumber is <= 0 ||
            request.CurrentGrade is < StudentProfile.MinGrade or > StudentProfile.MaxGrade || !ValidOptional(request.AddressText, SchoolSuggestion.AddressMaxLength))
            return Result<SchoolSuggestionResponse>.ValidationFailure(Invalid("suggestion", "student.school_suggestion.invalid", "Student.SchoolSuggestion.Invalid"));
        var identityUserId = UserId(principal);
        if (identityUserId is null) return Result<SchoolSuggestionResponse>.Failure(StudentErrors.ProvisioningRequired);
        IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        await using (transaction)
        {
            var student = await db.Students.Include(x => x.Profile).SingleOrDefaultAsync(x => x.IdentityUserId == identityUserId, ct);
            var accessError = AccessError(student);
            if (accessError is not null) return Result<SchoolSuggestionResponse>.Failure(accessError);
            var currentStudent = student!;
            var region = await db.Regions.SingleOrDefaultAsync(x => x.Id == request.ResidenceRegionId, ct);
            if (region is null) return Result<SchoolSuggestionResponse>.Failure(EducationErrors.RegionNotFound);
            if (!region.IsActive) return Result<SchoolSuggestionResponse>.Failure(EducationErrors.RegionInactive);
            var normalized = EducationNormalization.Key(request.SuggestedName);
            var suggestion = await db.SchoolSuggestions.SingleOrDefaultAsync(x => x.SubmittedByStudentId == currentStudent.Id && x.RegionId == region.Id &&
                x.NormalizedName == normalized && x.Status == SchoolSuggestionStatus.Pending, ct);
            var now = clock.UtcNow;
            if (suggestion is null)
            {
                suggestion = new SchoolSuggestion(Guid.NewGuid(), currentStudent.Id, request.SuggestedName.Trim(), request.SuggestedNumber, region.Id,
                    normalized, Trim(request.AddressText), now);
                db.SchoolSuggestions.Add(suggestion);
            }
            var profile = await db.EducationProfiles.SingleOrDefaultAsync(x => x.StudentId == currentStudent.Id, ct);
            if (profile is not null && request.ExpectedProfileVersion.HasValue && profile.Version != request.ExpectedProfileVersion.Value)
                return Result<SchoolSuggestionResponse>.Failure(EducationErrors.ProfileConflict);
            var year = academicYears.Current();
            var graduation = AcademicYearService.ExpectedGraduationYear(year.End, request.CurrentGrade);
            if (profile is null)
            {
                profile = new StudentEducationProfile(currentStudent.Id, region.Id, null, suggestion.Id, request.CurrentGrade, year.Start, year.End,
                    graduation, EducationStatus.PendingSchoolReview, Trim(request.AddressText), now);
                db.EducationProfiles.Add(profile);
            }
            else
            {
                profile.SetPendingSchool(region.Id, suggestion.Id, request.CurrentGrade, year.Start, year.End, graduation, Trim(request.AddressText), now);
            }
            var currentEnrollment = await db.SchoolEnrollments.SingleOrDefaultAsync(x => x.StudentId == currentStudent.Id && x.IsCurrent, ct);
            currentEnrollment?.End("pending_school_suggestion", now);
            currentStudent.Profile.UpdateEducationSnapshot(region.NameTg, region.NameTg, request.SuggestedName, request.CurrentGrade, now);
            WriteAudit(UserId(principal), "student.school_suggestion.created", "school_suggestion", suggestion.Id, currentStudent.Id, null,
                new { suggestion.RegionId, suggestion.SuggestedNumber });
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Result<SchoolSuggestionResponse>.Success(ToSuggestionResponse(suggestion, region, russian));
        }
    }

    public async Task<Result<SchoolSuggestionResponse>> GetCurrentSuggestionAsync(ClaimsPrincipal principal, bool russian, CancellationToken ct)
    {
        var student = await FindStudentAsync(principal, ct);
        var accessError = AccessError(student);
        if (accessError is not null) return Result<SchoolSuggestionResponse>.Failure(accessError);
        var profile = await db.EducationProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.StudentId == student!.Id, ct);
        if (profile?.PendingSchoolSuggestionId is not Guid id) return Result<SchoolSuggestionResponse>.Failure(EducationErrors.SuggestionNotFound);
        var suggestion = await db.SchoolSuggestions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (suggestion is null) return Result<SchoolSuggestionResponse>.Failure(EducationErrors.SuggestionNotFound);
        var region = await db.Regions.AsNoTracking().SingleAsync(x => x.Id == suggestion.RegionId, ct);
        return Result<SchoolSuggestionResponse>.Success(ToSuggestionResponse(suggestion, region, russian));
    }

    public async Task<Result<PagedResponse<SchoolSuggestionResponse>>> GetSuggestionsAsync(AdminSchoolSuggestionFilter request, bool russian, CancellationToken ct)
    {
        if (request.Page < 1 || request.PageSize < 1 ||
            (request.Status.HasValue && !Enum.IsDefined((SchoolSuggestionStatus)request.Status.Value)))
            return Result<PagedResponse<SchoolSuggestionResponse>>.ValidationFailure(Invalid("suggestions", "student.school_suggestion.invalid", "Student.SchoolSuggestion.Invalid"));

        var pageSize = Math.Min(request.PageSize, 50);
        var normalizedSearch = EducationNormalization.Key(request.Search);
        var query = db.SchoolSuggestions.AsNoTracking().Join(db.Regions.AsNoTracking(), suggestion => suggestion.RegionId, region => region.Id,
            (suggestion, region) => new { suggestion, region });
        if (request.Status.HasValue) query = query.Where(x => (int)x.suggestion.Status == request.Status.Value);
        if (request.RegionId.HasValue) query = query.Where(x => x.suggestion.RegionId == request.RegionId.Value);
        if (!string.IsNullOrEmpty(normalizedSearch))
            query = query.Where(x => x.suggestion.NormalizedName.Contains(normalizedSearch) || (x.suggestion.SuggestedNumber.HasValue && x.suggestion.SuggestedNumber.Value.ToString() == normalizedSearch));

        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.suggestion.Status).ThenBy(x => x.suggestion.CreatedAtUtc).ThenBy(x => x.suggestion.Id)
            .Skip((request.Page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Result<PagedResponse<SchoolSuggestionResponse>>.Success(new(rows.Select(x => ToSuggestionResponse(x.suggestion, x.region, russian)).ToArray(), request.Page, pageSize, total));
    }

    public async Task<Result<SchoolSuggestionResponse>> ReviewSuggestionAsync(Guid suggestionId, ReviewSchoolSuggestionRequest request,
        ClaimsPrincipal actor, bool russian, CancellationToken ct)
    {
        var rejectionReason = Trim(request.RejectionReason);
        var isRejection = rejectionReason is not null;
        if (suggestionId == Guid.Empty || (!isRejection && request.ExistingSchoolId.HasValue == (request.NewSchool is not null)) ||
            (isRejection && (request.ExistingSchoolId.HasValue || request.NewSchool is not null)) ||
            (request.NewSchool is not null && !request.VerifyNewSchool) ||
            (!string.IsNullOrWhiteSpace(request.RejectionReason) && request.RejectionReason.Trim().Length > SchoolSuggestion.RejectionReasonMaxLength))
            return Result<SchoolSuggestionResponse>.ValidationFailure(Invalid("review", "student.school_suggestion.review_invalid", "Student.SchoolSuggestion.ReviewInvalid"));

        IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        await using (transaction)
        {
            var suggestion = await db.SchoolSuggestions.SingleOrDefaultAsync(x => x.Id == suggestionId, ct);
            if (suggestion is null) return Result<SchoolSuggestionResponse>.Failure(EducationErrors.SuggestionNotFound);
            if (suggestion.Version != request.ExpectedVersion || suggestion.Status != SchoolSuggestionStatus.Pending)
                return Result<SchoolSuggestionResponse>.Failure(EducationErrors.ProfileConflict);
            var region = await db.Regions.SingleOrDefaultAsync(x => x.Id == suggestion.RegionId, ct);
            if (region is null || !region.IsActive) return Result<SchoolSuggestionResponse>.Failure(EducationErrors.RegionInactive);
            var now = clock.UtcNow;
            var actorId = UserId(actor);

            if (isRejection)
            {
                suggestion.Reject(rejectionReason!, actorId, now);
                var pendingProfiles = await db.EducationProfiles.Where(x => x.PendingSchoolSuggestionId == suggestion.Id).ToListAsync(ct);
                foreach (var profile in pendingProfiles) profile.RejectPendingSchool(now);
                WriteAudit(actorId, "student.school_suggestion.rejected", "school_suggestion", suggestion.Id, suggestion.SubmittedByStudentId, null,
                    new { request.RejectionReason });
                await db.SaveChangesAsync(ct);
                if (transaction is not null) await transaction.CommitAsync(ct);
                return Result<SchoolSuggestionResponse>.Success(ToSuggestionResponse(suggestion, region, russian));
            }

            School school;
            var createdAsNew = request.NewSchool is not null;
            if (request.ExistingSchoolId is Guid existingSchoolId)
            {
                var existingSchool = await db.Schools.SingleOrDefaultAsync(x => x.Id == existingSchoolId, ct);
                if (existingSchool is null) return Result<SchoolSuggestionResponse>.Failure(EducationErrors.SchoolNotFound);
                school = existingSchool;
                if (school.Status != SchoolStatus.Verified) return Result<SchoolSuggestionResponse>.Failure(EducationErrors.SchoolNotSelectable);
            }
            else
            {
                var newSchool = request.NewSchool!;
                if (newSchool.RegionId != suggestion.RegionId || !TrySchoolType(newSchool.Type, out var type) || !ValidName(newSchool.NameRu, School.NameMaxLength) ||
                    newSchool.Number is <= 0 || !ValidOptional(newSchool.NameTg, School.NameMaxLength) || !ValidOptional(newSchool.AddressText, School.AddressTextMaxLength))
                    return Result<SchoolSuggestionResponse>.ValidationFailure(Invalid("newSchool", "student.school.invalid", "Student.School.Invalid"));
                var normalized = EducationNormalization.Key(newSchool.NameRu);
                var duplicate = await db.Schools.AnyAsync(x => x.RegionId == newSchool.RegionId && x.Type == type &&
                    (x.Status == SchoolStatus.Draft || x.Status == SchoolStatus.Verified || x.Status == SchoolStatus.Inactive) &&
                    (newSchool.Number.HasValue ? x.Number == newSchool.Number : x.Number == null && x.NormalizedName == normalized), ct);
                if (duplicate) return Result<SchoolSuggestionResponse>.Failure(EducationErrors.SchoolDuplicate);
                school = new School(Guid.NewGuid(), newSchool.RegionId, Trim(newSchool.NameTg), newSchool.NameRu.Trim(), Trim(newSchool.ShortName), newSchool.Number,
                    type, normalized, EducationNormalization.SearchText(newSchool.NameTg, newSchool.NameRu, newSchool.ShortName, newSchool.Number), Trim(newSchool.AddressText), actorId, now);
                school.Verify(actorId, now);
                db.Schools.Add(school);
            }

            suggestion.Approve(school.Id, createdAsNew, actorId, now);
            var profiles = await db.EducationProfiles.Where(x => x.PendingSchoolSuggestionId == suggestion.Id).ToListAsync(ct);
            var profileStudentIds = profiles.Select(x => x.StudentId).ToArray();
            var students = await db.Students.Include(x => x.Profile).Where(x => profileStudentIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            foreach (var profile in profiles)
            {
                profile.ReassignSchoolAfterSuggestion(school.Id, now);
                db.SchoolEnrollments.Add(new StudentSchoolEnrollment(Guid.NewGuid(), profile.StudentId, school.Id, school.RegionId,
                    profile.CurrentGrade!.Value, profile.AcademicYearStart!.Value, profile.AcademicYearEnd!.Value,
                    EnrollmentSource.SchoolSuggestionReview, "school_suggestion_approved", now));
                students[profile.StudentId].Profile.UpdateEducationSnapshot(region.NameTg, region.NameTg, school.NameTg ?? school.NameRu, profile.CurrentGrade, now);
            }
            WriteAudit(actorId, "student.school_suggestion.approved", "school_suggestion", suggestion.Id, suggestion.SubmittedByStudentId, null,
                new { schoolId = school.Id, createdAsNew });
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Result<SchoolSuggestionResponse>.Success(ToSuggestionResponse(suggestion, region, russian));
        }
    }

    public async Task<Result<StudentEducationProfileResponse>> CorrectByAdminAsync(Guid studentId, AdminCorrectEducationProfileRequest request,
        ClaimsPrincipal actor, bool russian, CancellationToken ct)
    {
        if (studentId == Guid.Empty || request.ResidenceRegionId == Guid.Empty || request.SchoolId == Guid.Empty ||
            request.CurrentGrade is < StudentProfile.MinGrade or > StudentProfile.MaxGrade || !ValidName(request.Reason, StudentSchoolEnrollment.ReasonMaxLength))
            return Result<StudentEducationProfileResponse>.ValidationFailure(Invalid("education", "student.education.invalid", "Student.Education.Invalid"));

        IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        await using (transaction)
        {
            var student = await db.Students.Include(x => x.Profile).SingleOrDefaultAsync(x => x.Id == studentId, ct);
            if (student is null) return Result<StudentEducationProfileResponse>.Failure(StudentErrors.NotFound);
            var accessError = AccessError(student);
            if (accessError is not null) return Result<StudentEducationProfileResponse>.Failure(accessError);
            var region = await db.Regions.SingleOrDefaultAsync(x => x.Id == request.ResidenceRegionId && x.IsActive, ct);
            if (region is null) return Result<StudentEducationProfileResponse>.Failure(EducationErrors.RegionNotFound);
            var school = await db.Schools.SingleOrDefaultAsync(x => x.Id == request.SchoolId, ct);
            if (school is null) return Result<StudentEducationProfileResponse>.Failure(EducationErrors.SchoolNotFound);
            if (school.Status != SchoolStatus.Verified) return Result<StudentEducationProfileResponse>.Failure(EducationErrors.SchoolNotSelectable);
            var profile = await db.EducationProfiles.SingleOrDefaultAsync(x => x.StudentId == studentId, ct);
            if (profile is not null && request.ExpectedVersion.HasValue && profile.Version != request.ExpectedVersion.Value)
                return Result<StudentEducationProfileResponse>.Failure(EducationErrors.ProfileConflict);
            if (profile is null && request.ExpectedVersion.HasValue) return Result<StudentEducationProfileResponse>.Failure(EducationErrors.ProfileConflict);

            var now = clock.UtcNow;
            var year = academicYears.Current();
            var graduation = AcademicYearService.ExpectedGraduationYear(year.End, request.CurrentGrade);
            var old = profile is null ? null : new { profile.SchoolId, profile.CurrentGrade, profile.ResidenceRegionId };
            if (profile is null)
            {
                profile = new StudentEducationProfile(studentId, region.Id, school.Id, null, request.CurrentGrade, year.Start, year.End, graduation,
                    EducationStatus.Studying, null, now);
                db.EducationProfiles.Add(profile);
            }
            else
            {
                profile.SetStudying(region.Id, school.Id, request.CurrentGrade, year.Start, year.End, graduation, profile.AddressText, now);
            }
            var currentEnrollment = await db.SchoolEnrollments.SingleOrDefaultAsync(x => x.StudentId == studentId && x.IsCurrent, ct);
            currentEnrollment?.End(request.Reason.Trim(), now);
            db.SchoolEnrollments.Add(new StudentSchoolEnrollment(Guid.NewGuid(), studentId, school.Id, school.RegionId, request.CurrentGrade, year.Start, year.End,
                EnrollmentSource.AdminCorrection, request.Reason.Trim(), now));
            student.Profile.UpdateEducationSnapshot(region.NameTg, region.NameTg, school.NameTg ?? school.NameRu, request.CurrentGrade, now);
            WriteAudit(UserId(actor), "student.education.admin_corrected", "education_profile", studentId, studentId, old,
                new { schoolId = school.Id, grade = request.CurrentGrade, request.Reason });
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Result<StudentEducationProfileResponse>.Success(await BuildResponseAsync(studentId, russian, ct));
        }
    }

    private async Task<StudentEducationProfileResponse> BuildResponseAsync(Guid studentId, bool russian, CancellationToken ct)
    {
        var profile = await db.EducationProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.StudentId == studentId, ct);
        if (profile is null) return new(studentId, null, null, null, null, null, null, null, EducationStatus.Incomplete.ToString(), null, 0, DateTimeOffset.MinValue);
        var region = profile.ResidenceRegionId.HasValue ? await db.Regions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == profile.ResidenceRegionId, ct) : null;
        var school = profile.SchoolId.HasValue ? await db.Schools.AsNoTracking().SingleOrDefaultAsync(x => x.Id == profile.SchoolId, ct) : null;
        Region? schoolRegion = school is null ? null : await db.Regions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == school.RegionId, ct);
        return new(studentId, region is null ? null : ToRegionResponse(region, russian), school is null || schoolRegion is null ? null : ToSchoolResponse(school, schoolRegion, russian),
            profile.PendingSchoolSuggestionId, profile.CurrentGrade, profile.AcademicYearStart, profile.AcademicYearEnd, profile.ExpectedGraduationYear,
            profile.Status.ToString(), profile.AddressText, profile.Version, profile.UpdatedAtUtc);
    }

    private async Task<Student?> FindStudentAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var id = UserId(principal);
        return id is null ? null : await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.IdentityUserId == id, ct);
    }

    private void WriteAudit(Guid? actorId, string action, string resourceType, Guid resourceId, Guid? studentId, object? oldValues, object? newValues)
    {
        db.EducationAuditLogs.Add(new StudentEducationAuditLog(Guid.NewGuid(), actorId, action, resourceType, resourceId.ToString(), studentId,
            oldValues is null ? null : JsonSerializer.Serialize(oldValues), newValues is null ? null : JsonSerializer.Serialize(newValues), null, clock.UtcNow));
    }

    private static Error? AccessError(Student? student) => student?.Status switch
    {
        null => StudentErrors.NotFound,
        StudentStatus.Suspended => StudentErrors.Suspended,
        StudentStatus.Closed => StudentErrors.Closed,
        _ => null
    };
    private static Guid? UserId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) ? id : null;
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool ValidName(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= max;
    private static bool ValidOptional(string? value, int max) => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= max;
    private static bool TrySchoolType(int value, out SchoolType type) { type = (SchoolType)value; return Enum.IsDefined(type); }
    private static Dictionary<string, IReadOnlyList<Error>> Invalid(string field, string code, string message) => new() { [field] = [Error.Validation(code, message)] };
    private static RegionResponse ToRegionResponse(Region region, bool russian) => new(region.Id, region.ParentId, (int)region.Type,
        russian ? region.NameRu : region.NameTg, russian ? region.FullPathRu : region.FullPathTg, region.Depth, region.SortOrder, region.IsActive, region.Version);
    private static SchoolResponse ToSchoolResponse(School school, Region region, bool russian) => new(school.Id, school.RegionId,
        russian || string.IsNullOrWhiteSpace(school.NameTg) ? school.NameRu : school.NameTg!, school.NameTg, school.NameRu, school.ShortName,
        school.Number, (int)school.Type, school.Status.ToString(), russian ? region.FullPathRu : region.FullPathTg, school.AddressText, school.Version);
    private static SchoolSuggestionResponse ToSuggestionResponse(SchoolSuggestion suggestion, Region region, bool russian) => new(suggestion.Id,
        suggestion.SuggestedName, suggestion.SuggestedNumber, ToRegionResponse(region, russian), suggestion.Status.ToString(), suggestion.ApprovedSchoolId,
        suggestion.RejectionReason, suggestion.CreatedAtUtc, suggestion.ReviewedAtUtc, suggestion.Version);
}
