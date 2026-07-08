using Adeeb.SharedKernel.Errors;

namespace Adeeb.Modules.QuestionBank.Application;

public static class QuestionBankErrors
{
    public static readonly Error QuestionNotFound = Error.NotFound("question_bank.question_not_found", "QuestionBank.QuestionNotFound");
    public static readonly Error SubjectNotFound = Error.NotFound("academic.subject_not_found", "Academic.SubjectNotFound");
    public static readonly Error TopicNotFound = Error.NotFound("academic.topic_not_found", "Academic.TopicNotFound");
    public static readonly Error ImportExtractorNotFound = Error.Validation("question_import.extractor_not_found", "QuestionImport.ExtractorNotFound");
    public static readonly Error ImportNoExtractableText = Error.Validation("question_import.no_extractable_text", "QuestionImport.NoExtractableText");
}
