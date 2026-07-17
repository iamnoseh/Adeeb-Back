namespace Adeeb.Modules.Vocabulary.Infrastructure.Persistence;

internal static class VocabularyDatabaseConstraints
{
    public const string LanguageCode = "ux_vocabulary_languages_code";
    public const string WordIdentity = "ux_vocabulary_words_language_normalized_text";
    public const string RelationIdentity = "ux_vocabulary_relations_word_related_type";
    public const string DailyWordIdentity = "pk_vocabulary_daily_words";
    public const string CourseIdentity = "pk_student_vocabulary_courses";
    public const string ProgressIdentity = "pk_student_word_progress";
    public const string SessionAnswerIdentity = "pk_vocabulary_session_answers";
    public const string InProgressSessionLookup = "ix_vocabulary_sessions_student_mode_status";
}
