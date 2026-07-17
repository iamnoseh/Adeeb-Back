using Adeeb.SharedKernel.Errors;

namespace Adeeb.Modules.Vocabulary.Application;

internal static class VocabularyErrors
{
    public static readonly Error NotFound = Error.NotFound("vocabulary.not_found", "Vocabulary.NotFound");
    public static readonly Error LanguageNotFound = Error.NotFound("vocabulary.language_not_found", "Vocabulary.LanguageNotFound");
    public static readonly Error TopicNotFound = Error.NotFound("vocabulary.topic_not_found", "Vocabulary.TopicNotFound");
    public static readonly Error WordNotFound = Error.NotFound("vocabulary.word_not_found", "Vocabulary.WordNotFound");
    public static readonly Error QuestionNotFound = Error.NotFound("vocabulary.question_not_found", "Vocabulary.QuestionNotFound");
    public static readonly Error SessionNotFound = Error.NotFound("vocabulary.session_not_found", "Vocabulary.SessionNotFound");
    public static readonly Error CourseRequired = Error.Conflict("vocabulary.course_required", "Vocabulary.CourseRequired");
    public static readonly Error StudentRequired = Error.Conflict("vocabulary.student_required", "Vocabulary.StudentRequired");
    public static readonly Error StudentUnavailable = Error.Forbidden("vocabulary.student_unavailable", "Vocabulary.StudentUnavailable");
    public static readonly Error Duplicate = Error.Conflict("vocabulary.duplicate", "Vocabulary.Duplicate");
    public static readonly Error PublishInvalid = Error.Conflict("vocabulary.publish_invalid", "Vocabulary.PublishInvalid");
    public static readonly Error NotEnoughQuestions = Error.Conflict("vocabulary.not_enough_questions", "Vocabulary.NotEnoughQuestions");
    public static readonly Error SessionCompleted = Error.Conflict("vocabulary.session_completed", "Vocabulary.SessionCompleted");
    public static readonly Error AnswerLocked = Error.Conflict("vocabulary.answer_locked", "Vocabulary.AnswerLocked");
}
