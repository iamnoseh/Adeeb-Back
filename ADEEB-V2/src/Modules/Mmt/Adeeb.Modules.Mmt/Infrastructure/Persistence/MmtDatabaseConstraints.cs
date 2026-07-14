using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence;

internal static class MmtDatabaseConstraints
{
    public const string ClusterCode = "ux_mmt_clusters_code";
    public const string UniversityName = "ux_mmt_universities_normalized_name";
    public const string SpecialtyCode = "ux_mmt_specialties_code";
    public const string ProgramIdentity = "ux_mmt_admission_program_identity";
    public const string ProgramYearScore = "ix_mmt_score_latest";
    public const string ActiveStudentProfile = "ux_mmt_student_profile_active_year";
    public const string ChoicePriority = "ux_mmt_choice_profile_priority";
    public const string ChoiceProgram = "ux_mmt_choice_profile_program";

    public static bool IsUniqueViolation(DbUpdateException exception, string constraint) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: var name
        } && string.Equals(name, constraint, StringComparison.Ordinal);
}
