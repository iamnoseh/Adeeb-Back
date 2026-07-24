using Adeeb.SharedKernel.Errors;

namespace Adeeb.Modules.Students.Application;

internal static class EducationErrors
{
    public static readonly Error RegionNotFound = Error.NotFound("student.region.not_found", "Student.Region.NotFound");
    public static readonly Error RegionInactive = Error.Conflict("student.region.inactive", "Student.Region.Inactive");
    public static readonly Error RegionHierarchyInvalid = Error.Validation("student.region.hierarchy_invalid", "Student.Region.HierarchyInvalid");
    public static readonly Error RegionInUse = Error.Conflict("student.region.in_use", "Student.Region.InUse");
    public static readonly Error RegionDuplicate = Error.Conflict("student.region.duplicate", "Student.Region.Duplicate");
    public static readonly Error SchoolNotFound = Error.NotFound("student.school.not_found", "Student.School.NotFound");
    public static readonly Error SchoolNotSelectable = Error.Conflict("student.school.not_selectable", "Student.School.NotSelectable");
    public static readonly Error SchoolDuplicate = Error.Conflict("student.school.duplicate", "Student.School.Duplicate");
    public static readonly Error SchoolMergeInvalid = Error.Validation("student.school.merge_invalid", "Student.School.MergeInvalid");
    public static readonly Error ProfileConflict = Error.Conflict("student.education.concurrency_conflict", "Student.Education.ConcurrencyConflict");
    public static readonly Error ProfileInvalid = Error.Validation("student.education.invalid", "Student.Education.Invalid");
    public static readonly Error SuggestionNotFound = Error.NotFound("student.school_suggestion.not_found", "Student.SchoolSuggestion.NotFound");
    public static readonly Error SuggestionReviewInvalid = Error.Validation("student.school_suggestion.review_invalid", "Student.SchoolSuggestion.ReviewInvalid");
    public static readonly Error ImportInvalid = Error.Validation("student.education_import.invalid", "Student.EducationImport.Invalid");
    public static readonly Error ImportConflict = Error.Conflict("student.education_import.conflict", "Student.EducationImport.Conflict");
    public static readonly Error RolloverNotFound = Error.NotFound("student.rollover.not_found", "Student.Rollover.NotFound");
    public static readonly Error RolloverInvalid = Error.Conflict("student.rollover.invalid", "Student.Rollover.Invalid");
}
