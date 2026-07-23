using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Students.Domain.Students;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;

namespace Adeeb.Modules.Students.Application;

internal static class Validation
{
    public static Result ValidateProfile(UpdateStudentProfileRequest request, DateOnly today)
    {
        var errors = new Dictionary<string, IReadOnlyList<Error>>(StringComparer.OrdinalIgnoreCase);
        ValidateOptionalText(request.DisplayName, "displayName", StudentProfile.DisplayNameMaxLength, errors);
        ValidateOptionalText(request.AvatarUrl, "avatarUrl", StudentProfile.AvatarUrlMaxLength, errors);
        ValidateOptionalText(request.Region, "region", StudentProfile.RegionMaxLength, errors);
        ValidateOptionalText(request.City, "city", StudentProfile.CityMaxLength, errors);
        ValidateOptionalText(request.SchoolName, "schoolName", StudentProfile.SchoolNameMaxLength, errors);
        ValidateGender(request.Gender, errors);

        if (request.DateOfBirth.HasValue &&
            (request.DateOfBirth.Value > today || request.DateOfBirth.Value < StudentProfile.MinimumDateOfBirth))
        {
            errors["dateOfBirth"] = [Error.Validation("student.profile.date_of_birth.invalid", "Student.Profile.InvalidDateOfBirth")];
        }

        if (request.Grade.HasValue && request.Grade.Value is < StudentProfile.MinGrade or > StudentProfile.MaxGrade)
        {
            errors["grade"] = [Error.Validation("student.profile.grade.invalid", "Student.Profile.InvalidGrade")];
        }

        return errors.Count == 0 ? Result.Success() : Result.ValidationFailure(errors);
    }

    public static bool TryParseStatus(int value, out StudentStatus status)
    {
        status = StudentStatus.Active;
        if (!Enum.IsDefined(typeof(StudentStatus), value))
        {
            return false;
        }

        status = (StudentStatus)value;
        return true;
    }

    private static void ValidateOptionalText(string? value, string field, int maxLength, Dictionary<string, IReadOnlyList<Error>> errors)
    {
        if (value is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
        {
            errors[field] = [Error.Validation($"student.profile.{field}.invalid", "Student.Profile.Invalid")];
        }
    }

    private static void ValidateGender(string? value, Dictionary<string, IReadOnlyList<Error>> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalized = value.Trim();
        if (normalized.Length > StudentProfile.GenderMaxLength ||
            !string.Equals(normalized, "Male", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalized, "Female", StringComparison.OrdinalIgnoreCase))
        {
            errors["gender"] = [Error.Validation("student.profile.gender.invalid", "Student.Profile.InvalidGender")];
        }
    }
}
