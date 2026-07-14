using Adeeb.SharedKernel.Errors;

namespace Adeeb.Modules.Mmt.Application;

public static class MmtErrors
{
    public static readonly Error ClusterNotFound = Error.NotFound("mmt.cluster_not_found", "MMT.ClusterNotFound");
    public static readonly Error UniversityNotFound = Error.NotFound("mmt.university_not_found", "MMT.UniversityNotFound");
    public static readonly Error SpecialtyNotFound = Error.NotFound("mmt.specialty_not_found", "MMT.SpecialtyNotFound");
    public static readonly Error ProgramNotFound = Error.NotFound("mmt.program_not_found", "MMT.ProgramNotFound");
    public static readonly Error ScoreNotFound = Error.NotFound("mmt.score_not_found", "MMT.ScoreNotFound");
    public static readonly Error DuplicateCluster = Error.Conflict("mmt.cluster_code_exists", "MMT.ClusterCodeExists");
    public static readonly Error DuplicateUniversity = Error.Conflict("mmt.university_exists", "MMT.UniversityExists");
    public static readonly Error DuplicateSpecialty = Error.Conflict("mmt.specialty_code_exists", "MMT.SpecialtyCodeExists");
    public static readonly Error DuplicateProgram = Error.Conflict("mmt.program_exists", "MMT.ProgramExists");
    public static readonly Error DuplicateScore = Error.Conflict("mmt.score_exists", "MMT.ScoreExists");
    public static readonly Error InactiveReference = Error.Conflict("mmt.reference_inactive", "MMT.ReferenceInactive");
    public static readonly Error PublishInvalid = Error.Conflict("mmt.program_publish_invalid", "MMT.PublishInvalid");
    public static readonly Error ImportFileInvalid = Error.Validation("mmt.import_file_invalid", "MMT.ImportFileInvalid");
    public static readonly Error ImportExistingScore = Error.Conflict("mmt.import_existing_score", "MMT.ImportExistingScore");
    public static readonly Error ImportConflict = Error.Conflict("mmt.import_conflict", "MMT.ImportConflict");
}
