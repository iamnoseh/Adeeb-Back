using Adeeb.SharedKernel.Errors;

namespace Adeeb.Modules.AcademicCatalog.Application;

public static class AcademicCatalogErrors
{
    public static readonly Error Forbidden = Error.Forbidden("common.forbidden", "Common.Forbidden");
    public static readonly Error SubjectNotFound = Error.NotFound("academic.subject_not_found", "Academic.SubjectNotFound");
    public static readonly Error TopicNotFound = Error.NotFound("academic.topic_not_found", "Academic.TopicNotFound");
    public static readonly Error DuplicateSubjectCode = Error.Conflict("academic.subject_code_exists", "Academic.SubjectCodeExists");
    public static readonly Error DuplicateTopicCode = Error.Conflict("academic.topic_code_exists", "Academic.TopicCodeExists");
}
