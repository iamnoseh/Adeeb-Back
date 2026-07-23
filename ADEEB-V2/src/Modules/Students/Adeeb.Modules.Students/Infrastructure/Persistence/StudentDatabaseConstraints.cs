namespace Adeeb.Modules.Students.Infrastructure.Persistence;

internal static class StudentDatabaseConstraints
{
    public const string IdentityUserIdUnique = "IX_students_identity_user_id";
    public const string DailyActivityPrimaryKey = "PK_student_daily_activities";
    public const string RegionSiblingUnique = "UX_regions_parent_type_name";
    public const string RootRegionTypeNameUnique = "UX_regions_root_type_name";
    public const string SchoolNumberUnique = "UX_schools_region_number_type_live";
    public const string SchoolNameUnique = "UX_schools_region_name_type_live";
    public const string EducationProfileStudentUnique = "UX_student_education_profiles_student";
    public const string CurrentEnrollmentUnique = "UX_student_school_enrollments_current_student";
    public const string SuggestionPendingUnique = "UX_school_suggestions_pending_student_name";
    public const string RolloverAcademicYearUnique = "UX_academic_year_rollovers_year";
    public const string RolloverIdempotencyUnique = "UX_academic_year_rollovers_idempotency";
}
