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
    public static readonly Error ClusterSubjectInvalid = Error.Validation("mmt.cluster_subject_invalid", "MMT.ClusterSubjectInvalid");
    public static readonly Error DuplicateUniversity = Error.Conflict("mmt.university_exists", "MMT.UniversityExists");
    public static readonly Error DuplicateSpecialty = Error.Conflict("mmt.specialty_code_exists", "MMT.SpecialtyCodeExists");
    public static readonly Error DuplicateProgram = Error.Conflict("mmt.program_exists", "MMT.ProgramExists");
    public static readonly Error DuplicateScore = Error.Conflict("mmt.score_exists", "MMT.ScoreExists");
    public static readonly Error InactiveReference = Error.Conflict("mmt.reference_inactive", "MMT.ReferenceInactive");
    public static readonly Error PublishInvalid = Error.Conflict("mmt.program_publish_invalid", "MMT.PublishInvalid");
    public static readonly Error ImportFileInvalid = Error.Validation("mmt.import_file_invalid", "MMT.ImportFileInvalid");
    public static readonly Error ImportExistingScore = Error.Conflict("mmt.import_existing_score", "MMT.ImportExistingScore");
    public static readonly Error ImportConflict = Error.Conflict("mmt.import_conflict", "MMT.ImportConflict");
    public static readonly Error UserRequired = Error.Unauthorized("mmt.user_required", "Auth.InvalidCredentials");
    public static readonly Error StudentProfileNotFound = Error.NotFound("mmt.student_profile_not_found", "MMT.StudentProfileNotFound");
    public static readonly Error EvaluationNotFound = Error.NotFound("mmt.evaluation_not_found", "MMT.EvaluationNotFound");
    public static readonly Error AdmissionYearUnavailable = Error.Conflict("mmt.admission_year_unavailable", "MMT.AdmissionYearUnavailable");
    public static readonly Error GoalProgramInvalid = Error.Conflict("mmt.goal_program_invalid", "MMT.GoalProgramInvalid");
    public static readonly Error ChoiceProgramInvalid = Error.Conflict("mmt.choice_program_invalid", "MMT.ChoiceProgramInvalid");
    public static readonly Error TooManyChoices = Error.Validation("mmt.too_many_choices", "MMT.TooManyChoices");
    public static readonly Error DuplicateChoiceProgram = Error.Validation("mmt.duplicate_choice_program", "MMT.DuplicateChoiceProgram");
    public static readonly Error DuplicateChoicePriority = Error.Validation("mmt.duplicate_choice_priority", "MMT.DuplicateChoicePriority");
    public static readonly Error InvalidChoiceOrder = Error.Validation("mmt.invalid_choice_order", "MMT.InvalidChoiceOrder");
    public static readonly Error ChoicesRequired = Error.Conflict("mmt.choices_required", "MMT.ChoicesRequired");
    public static readonly Error EvaluationScoreInvalid = Error.Validation("mmt.evaluation_score_invalid", "MMT.ScoreInvalid");
    public static readonly Error ProfileConflict = Error.Conflict("mmt.profile_conflict", "MMT.ProfileConflict");
    public static readonly Error ChoiceUpdateConflict = Error.Conflict("mmt.choice_update_conflict", "MMT.ChoiceUpdateConflict");
}
