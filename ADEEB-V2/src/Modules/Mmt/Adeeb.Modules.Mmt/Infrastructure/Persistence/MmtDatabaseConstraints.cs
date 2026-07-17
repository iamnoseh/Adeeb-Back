using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence;

internal static class MmtDatabaseConstraints
{
    public const string ClusterCode = "ux_mmt_clusters_code";
    public const string UniversityName = "ux_mmt_universities_normalized_name";
    public const string UniversityNameRu = "ux_mmt_universities_normalized_name_ru";
    public const string SpecialtyCode = "ux_mmt_specialties_code";
    public const string ProgramIdentity = "ux_mmt_admission_program_identity";
    public const string ProgramYearRoundScore = "ux_mmt_score_program_year_round";
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
