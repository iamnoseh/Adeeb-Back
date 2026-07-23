using Adeeb.SharedKernel.Errors;

namespace Adeeb.Modules.Students.Application;

public static class StudentErrors
{
    public static readonly Error NotFound = Error.NotFound("student.not_found", "Student.NotFound");
    public static readonly Error AlreadyExists = Error.Conflict("student.already_exists", "Student.AlreadyExists");
    public static readonly Error ProvisioningRequired = Error.Conflict("student.provisioning_required", "Student.ProvisioningRequired");
    public static readonly Error Suspended = Error.Forbidden("student.suspended", "Student.Suspended");
    public static readonly Error Closed = Error.Forbidden("student.closed", "Student.Closed");
    public static readonly Error InvalidStatusTransition = Error.Validation("student.invalid_status_transition", "Student.InvalidStatusTransition");
    public static readonly Error AvatarInvalid = Error.Validation("student.avatar.invalid", "Student.Avatar.Invalid");
    public static readonly Error DateOfBirthLocked = Error.Conflict("student.profile.date_of_birth_locked", "Student.Profile.DateOfBirthLocked");
}
