namespace Adeeb.Modules.Students.Infrastructure.Persistence;

internal static class StudentDatabaseConstraints
{
    public const string IdentityUserIdUnique = "IX_students_identity_user_id";
    public const string DailyActivityPrimaryKey = "PK_student_daily_activities";
}
