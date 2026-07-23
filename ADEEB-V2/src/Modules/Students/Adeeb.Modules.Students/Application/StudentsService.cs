using System.Security.Claims;
using Adeeb.Application.Abstractions.Students;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Students.Domain.Students;
using Adeeb.Modules.Students.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Adeeb.Modules.Students.Application;

public sealed class StudentsService(
    StudentsDbContext db,
    IDateTimeProvider clock,
    ILogger<StudentsService> logger) : IStudentLookup, IStudentRegistrationProvisioner, IStudentCompetitionDirectory
{
    public async Task<Result<StudentResponse>> GetCurrentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var identityUserId = GetUserId(principal);
        if (identityUserId is null)
        {
            return Result<StudentResponse>.Failure(StudentErrors.ProvisioningRequired);
        }

        var response = await ProjectStudentsByIdentityUserId(identityUserId.Value)
            .SingleOrDefaultAsync(cancellationToken);

        return response is null
            ? Result<StudentResponse>.Failure(StudentErrors.NotFound)
            : Result<StudentResponse>.Success(response);
    }

    public async Task<Result<StudentResponse>> UpdateCurrentProfileAsync(ClaimsPrincipal principal, UpdateStudentProfileRequest request, CancellationToken cancellationToken)
    {
        var identityUserId = GetUserId(principal);
        if (identityUserId is null)
        {
            return Result<StudentResponse>.Failure(StudentErrors.ProvisioningRequired);
        }

        var validation = Validation.ValidateProfile(request, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));
        if (validation.IsFailure)
        {
            return Result<StudentResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var student = await db.Students
            .Include(x => x.Profile)
            .SingleOrDefaultAsync(x => x.IdentityUserId == identityUserId.Value, cancellationToken);
        if (student is null)
        {
            return Result<StudentResponse>.Failure(StudentErrors.NotFound);
        }

        if (student.Status == StudentStatus.Suspended)
        {
            return Result<StudentResponse>.Failure(StudentErrors.Suspended);
        }

        if (student.Status == StudentStatus.Closed)
        {
            return Result<StudentResponse>.Failure(StudentErrors.Closed);
        }

        try
        {
            student.UpdateProfile(
                request.DisplayName,
                request.AvatarUrl,
                request.DateOfBirth,
                request.Region,
                request.City,
                request.SchoolName,
                request.Grade,
                request.Gender,
                clock.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return Result<StudentResponse>.Failure(StudentErrors.DateOfBirthLocked);
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "student.profile.updated student_id={StudentId} identity_user_id={IdentityUserId}",
            student.Id,
            student.IdentityUserId);
        return Result<StudentResponse>.Success(ToResponse(student));
    }

    public async Task<Result<StudentResponse>> UpdateCurrentAvatarAsync(ClaimsPrincipal principal, string avatarUrl, CancellationToken cancellationToken)
    {
        var identityUserId = GetUserId(principal);
        if (identityUserId is null)
        {
            return Result<StudentResponse>.Failure(StudentErrors.ProvisioningRequired);
        }

        var student = await db.Students
            .Include(x => x.Profile)
            .SingleOrDefaultAsync(x => x.IdentityUserId == identityUserId.Value, cancellationToken);
        if (student is null)
        {
            return Result<StudentResponse>.Failure(StudentErrors.NotFound);
        }

        if (student.Status == StudentStatus.Suspended)
        {
            return Result<StudentResponse>.Failure(StudentErrors.Suspended);
        }

        if (student.Status == StudentStatus.Closed)
        {
            return Result<StudentResponse>.Failure(StudentErrors.Closed);
        }

        student.UpdateAvatar(avatarUrl, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "student.profile.avatar.updated student_id={StudentId} identity_user_id={IdentityUserId}",
            student.Id,
            student.IdentityUserId);
        return Result<StudentResponse>.Success(ToResponse(student));
    }

    public async Task<Result<StudentResponse>> GetByIdAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var response = await ProjectStudents().SingleOrDefaultAsync(x => x.StudentId == studentId, cancellationToken);
        return response is null
            ? Result<StudentResponse>.Failure(StudentErrors.NotFound)
            : Result<StudentResponse>.Success(response);
    }

    public async Task<Result<StudentResponse>> ChangeStatusAsync(Guid studentId, ClaimsPrincipal actor, ChangeStudentStatusRequest request, CancellationToken cancellationToken)
    {
        if (!Validation.TryParseStatus(request.Status, out var status))
        {
            return Result<StudentResponse>.ValidationFailure(new Dictionary<string, IReadOnlyList<SharedKernel.Errors.Error>>
            {
                ["status"] = [SharedKernel.Errors.Error.Validation("student.status.invalid", "Student.InvalidStatus")]
            });
        }

        var student = await db.Students.Include(x => x.Profile).SingleOrDefaultAsync(x => x.Id == studentId, cancellationToken);
        if (student is null)
        {
            return Result<StudentResponse>.Failure(StudentErrors.NotFound);
        }

        var oldStatus = student.Status;
        try
        {
            student.ChangeStatus(status, clock.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return Result<StudentResponse>.Failure(StudentErrors.InvalidStatusTransition);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "student.status.changed actor_user_id={ActorUserId} student_id={StudentId} old_status={OldStatus} new_status={NewStatus} reason={Reason}",
            GetUserId(actor),
            student.Id,
            oldStatus,
            student.Status,
            request.Reason);
        return Result<StudentResponse>.Success(ToResponse(student));
    }

    public async Task<Result<StudentProvisioningReference>> ProvisionForIdentityUserAsync(Guid identityUserId, CancellationToken cancellationToken)
    {
        if (identityUserId == Guid.Empty)
        {
            return Result<StudentProvisioningReference>.Failure(StudentErrors.ProvisioningRequired);
        }

        var existing = await db.Students.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdentityUserId == identityUserId, cancellationToken);
        if (existing is not null)
        {
            return Result<StudentProvisioningReference>.Success(new(existing.Id, existing.IdentityUserId));
        }

        var now = clock.UtcNow;
        var student = new Student(Guid.NewGuid(), identityUserId, now);
        db.Students.Add(student);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresExceptionHelper.IsUniqueViolation(ex, StudentDatabaseConstraints.IdentityUserIdUnique))
        {
            db.ChangeTracker.Clear();
            var raced = await db.Students.AsNoTracking().SingleAsync(x => x.IdentityUserId == identityUserId, cancellationToken);
            return Result<StudentProvisioningReference>.Success(new(raced.Id, raced.IdentityUserId));
        }

        logger.LogInformation("student.provisioned student_id={StudentId} identity_user_id={IdentityUserId}", student.Id, student.IdentityUserId);
        return Result<StudentProvisioningReference>.Success(new(student.Id, student.IdentityUserId));
    }

    public async Task<StudentReference?> FindByIdentityUserIdAsync(Guid identityUserId, CancellationToken cancellationToken) =>
        await db.Students.AsNoTracking()
            .Where(x => x.IdentityUserId == identityUserId)
            .Select(x => new StudentReference(x.Id, x.IdentityUserId, x.Status.ToString(), x.Profile.TimeZoneId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<StudentReference?> FindByStudentIdAsync(Guid studentId, CancellationToken cancellationToken) =>
        await db.Students.AsNoTracking()
            .Where(x => x.Id == studentId)
            .Select(x => new StudentReference(x.Id, x.IdentityUserId, x.Status.ToString(), x.Profile.TimeZoneId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, StudentCompetitionReference>> GetByIdentityUserIdsAsync(
        IReadOnlyCollection<Guid> identityUserIds, CancellationToken cancellationToken)
    {
        if (identityUserIds.Count == 0) return new Dictionary<Guid, StudentCompetitionReference>();
        return await db.Students.AsNoTracking()
            .Where(x => identityUserIds.Contains(x.IdentityUserId))
            .Select(x => new StudentCompetitionReference(x.IdentityUserId, x.Profile.DisplayName,
                x.Profile.AvatarUrl, x.Status == StudentStatus.Active))
            .ToDictionaryAsync(x => x.IdentityUserId, cancellationToken);
    }

    private IQueryable<StudentResponse> ProjectStudents() => ProjectStudentsQuery(db.Students.AsNoTracking());

    private IQueryable<StudentResponse> ProjectStudentsByIdentityUserId(Guid identityUserId) =>
        ProjectStudentsQuery(db.Students.AsNoTracking().Where(x => x.IdentityUserId == identityUserId));

    private static IQueryable<StudentResponse> ProjectStudentsQuery(IQueryable<Student> query) =>
        query
            .Select(x => new StudentResponse(
                x.Id,
                x.IdentityUserId,
                x.Status.ToString(),
                x.OnboardingState.ToString(),
                new StudentProfileResponse(
                    x.Profile.DisplayName,
                    x.Profile.AvatarUrl,
                    x.Profile.DateOfBirth,
                    x.Profile.Region,
                    x.Profile.City,
                    x.Profile.SchoolName,
                    x.Profile.Grade,
                    x.Profile.Gender,
                    x.Profile.TimeZoneId,
                    x.Profile.UpdatedAtUtc),
                x.CreatedAtUtc,
                x.UpdatedAtUtc));

    private static StudentResponse ToResponse(Student student) =>
        new(
            student.Id,
            student.IdentityUserId,
            student.Status.ToString(),
            student.OnboardingState.ToString(),
            new StudentProfileResponse(
                student.Profile.DisplayName,
                student.Profile.AvatarUrl,
                student.Profile.DateOfBirth,
                student.Profile.Region,
                student.Profile.City,
                student.Profile.SchoolName,
                student.Profile.Grade,
                student.Profile.Gender,
                student.Profile.TimeZoneId,
                student.Profile.UpdatedAtUtc),
            student.CreatedAtUtc,
            student.UpdatedAtUtc);

    private static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var userId)
            ? userId
            : null;
}
