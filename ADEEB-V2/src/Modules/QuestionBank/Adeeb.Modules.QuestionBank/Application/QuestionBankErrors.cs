using Adeeb.SharedKernel.Errors;

namespace Adeeb.Modules.QuestionBank.Application;

public static class QuestionBankErrors
{
    public static readonly Error QuestionNotFound = Error.NotFound("question_bank.question_not_found", "QuestionBank.QuestionNotFound");
    public static readonly Error SubjectNotFound = Error.NotFound("academic.subject_not_found", "Academic.SubjectNotFound");
    public static readonly Error TopicNotFound = Error.NotFound("academic.topic_not_found", "Academic.TopicNotFound");
}
