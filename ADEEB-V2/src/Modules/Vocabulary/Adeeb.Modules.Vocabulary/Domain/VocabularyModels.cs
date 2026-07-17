using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.Vocabulary.Domain;

public sealed class LearningLanguage : Entity
{
    private LearningLanguage() { }
    public LearningLanguage(Guid id, string code, string nameTg, string nameRu, int displayOrder, DateTimeOffset now)
    {
        Id = id; Code = NormalizeCode(code); NameTg = nameTg.Trim(); NameRu = nameRu.Trim();
        DisplayOrder = displayOrder; IsActive = true; CreatedAtUtc = UpdatedAtUtc = now;
    }
    public string Code { get; private set; } = string.Empty;
    public string NameTg { get; private set; } = string.Empty;
    public string NameRu { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public void Update(string code, string nameTg, string nameRu, int displayOrder, bool isActive, DateTimeOffset now)
    { Code = NormalizeCode(code); NameTg = nameTg.Trim(); NameRu = nameRu.Trim(); DisplayOrder = displayOrder; IsActive = isActive; UpdatedAtUtc = now; }
    public static string NormalizeCode(string value) => value.Trim().ToLowerInvariant();
}

public sealed class VocabularyTopic : Entity
{
    private VocabularyTopic() { }
    public VocabularyTopic(Guid id, Guid languageId, VocabularyLevel level, string nameTg, string nameRu, string? descriptionTg, string? descriptionRu, DateTimeOffset now)
    { Id = id; LanguageId = languageId; Level = level; SetText(nameTg, nameRu, descriptionTg, descriptionRu); Status = VocabularyContentStatus.Draft; CreatedAtUtc = UpdatedAtUtc = now; }
    public Guid LanguageId { get; private set; }
    public VocabularyLevel Level { get; private set; }
    public string NameTg { get; private set; } = string.Empty;
    public string NameRu { get; private set; } = string.Empty;
    public string? DescriptionTg { get; private set; }
    public string? DescriptionRu { get; private set; }
    public VocabularyContentStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public void Update(Guid languageId, VocabularyLevel level, string nameTg, string nameRu, string? descriptionTg, string? descriptionRu, VocabularyContentStatus status, DateTimeOffset now)
    { LanguageId = languageId; Level = level; SetText(nameTg, nameRu, descriptionTg, descriptionRu); Status = status; UpdatedAtUtc = now; }
    private void SetText(string tg, string ru, string? dtg, string? dru) { NameTg = tg.Trim(); NameRu = ru.Trim(); DescriptionTg = Clean(dtg); DescriptionRu = Clean(dru); }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class VocabularyWord : Entity
{
    private VocabularyWord() { }
    public VocabularyWord(Guid id, Guid languageId, Guid topicId, VocabularyLevel level, string targetText, string translationTg, string translationRu,
        string? explanationTg, string? explanationRu, string exampleTarget, string exampleTg, string exampleRu, DateTimeOffset now)
    { Id = id; Status = VocabularyContentStatus.Draft; CreatedAtUtc = now; Update(languageId, topicId, level, targetText, translationTg, translationRu, explanationTg, explanationRu, exampleTarget, exampleTg, exampleRu, Status, now); }
    public Guid LanguageId { get; private set; }
    public Guid TopicId { get; private set; }
    public VocabularyLevel Level { get; private set; }
    public string TargetText { get; private set; } = string.Empty;
    public string NormalizedText { get; private set; } = string.Empty;
    public string TranslationTg { get; private set; } = string.Empty;
    public string TranslationRu { get; private set; } = string.Empty;
    public string? ExplanationTg { get; private set; }
    public string? ExplanationRu { get; private set; }
    public string ExampleTarget { get; private set; } = string.Empty;
    public string ExampleTg { get; private set; } = string.Empty;
    public string ExampleRu { get; private set; } = string.Empty;
    public VocabularyContentStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public void Update(Guid languageId, Guid topicId, VocabularyLevel level, string targetText, string translationTg, string translationRu,
        string? explanationTg, string? explanationRu, string exampleTarget, string exampleTg, string exampleRu, VocabularyContentStatus status, DateTimeOffset now)
    {
        LanguageId = languageId; TopicId = topicId; Level = level; TargetText = targetText.Trim(); NormalizedText = Normalize(targetText);
        TranslationTg = translationTg.Trim(); TranslationRu = translationRu.Trim(); ExplanationTg = Clean(explanationTg); ExplanationRu = Clean(explanationRu);
        ExampleTarget = exampleTarget.Trim(); ExampleTg = exampleTg.Trim(); ExampleRu = exampleRu.Trim(); Status = status; UpdatedAtUtc = now;
    }
    public void Archive(DateTimeOffset now) { Status = VocabularyContentStatus.Archived; UpdatedAtUtc = now; }
    public static string Normalize(string value) => string.Join(' ', value.Trim().Normalize().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class VocabularyRelation : Entity
{
    private VocabularyRelation() { }
    public VocabularyRelation(Guid id, Guid wordId, Guid relatedWordId, VocabularyRelationType type, DateTimeOffset now)
    { Id = id; WordId = wordId; RelatedWordId = relatedWordId; Type = type; CreatedAtUtc = now; }
    public Guid WordId { get; private set; }
    public Guid RelatedWordId { get; private set; }
    public VocabularyRelationType Type { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public sealed class VocabularyQuestion : Entity
{
    private readonly List<VocabularyQuestionOption> _options = [];
    private VocabularyQuestion() { }
    public VocabularyQuestion(Guid id, Guid wordId, VocabularyQuestionType type, string promptTarget, string promptTg, string promptRu, int? correctTokenIndex, DateTimeOffset now)
    { Id = id; WordId = wordId; Type = type; PromptTarget = promptTarget.Trim(); PromptTg = promptTg.Trim(); PromptRu = promptRu.Trim(); CorrectTokenIndex = correctTokenIndex; Status = VocabularyContentStatus.Draft; CreatedAtUtc = UpdatedAtUtc = now; }
    public Guid WordId { get; private set; }
    public VocabularyQuestionType Type { get; private set; }
    public string PromptTarget { get; private set; } = string.Empty;
    public string PromptTg { get; private set; } = string.Empty;
    public string PromptRu { get; private set; } = string.Empty;
    public int? CorrectTokenIndex { get; private set; }
    public VocabularyContentStatus Status { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<VocabularyQuestionOption> Options => _options;
    public void Replace(string promptTarget, string promptTg, string promptRu, int? correctTokenIndex, IEnumerable<VocabularyQuestionOption> options, DateTimeOffset now)
    { PromptTarget = promptTarget.Trim(); PromptTg = promptTg.Trim(); PromptRu = promptRu.Trim(); CorrectTokenIndex = correctTokenIndex; _options.Clear(); _options.AddRange(options); Status = VocabularyContentStatus.Draft; ReviewedBy = null; ReviewedAtUtc = null; UpdatedAtUtc = now; }
    public void ChangeDefinition(Guid wordId, VocabularyQuestionType type, string promptTarget, string promptTg, string promptRu, int? correctTokenIndex, IEnumerable<VocabularyQuestionOption> options, DateTimeOffset now)
    { WordId = wordId; Type = type; Replace(promptTarget, promptTg, promptRu, correctTokenIndex, options, now); }
    public void Publish(Guid reviewerId, DateTimeOffset now) { Status = VocabularyContentStatus.Published; ReviewedBy = reviewerId; ReviewedAtUtc = now; UpdatedAtUtc = now; }
    public void Archive(DateTimeOffset now) { Status = VocabularyContentStatus.Archived; UpdatedAtUtc = now; }
}

public sealed class VocabularyQuestionOption
{
    private VocabularyQuestionOption() { }
    public VocabularyQuestionOption(Guid id, Guid questionId, Guid? wordId, string valueTarget, string valueTg, string valueRu, int displayOrder, bool isCorrect, int? correctOrder)
    { Id = id; QuestionId = questionId; WordId = wordId; ValueTarget = valueTarget.Trim(); ValueTg = valueTg.Trim(); ValueRu = valueRu.Trim(); DisplayOrder = displayOrder; IsCorrect = isCorrect; CorrectOrder = correctOrder; }
    public Guid Id { get; private set; }
    public Guid QuestionId { get; private set; }
    public Guid? WordId { get; private set; }
    public string ValueTarget { get; private set; } = string.Empty;
    public string ValueTg { get; private set; } = string.Empty;
    public string ValueRu { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public bool IsCorrect { get; private set; }
    public int? CorrectOrder { get; private set; }
}

public sealed class VocabularyDailyWord
{
    private VocabularyDailyWord() { }
    public VocabularyDailyWord(Guid languageId, DateOnly localDate, Guid wordId, bool isAutomatic, DateTimeOffset createdAtUtc)
    { LanguageId = languageId; LocalDate = localDate; WordId = wordId; IsAutomatic = isAutomatic; CreatedAtUtc = createdAtUtc; }
    public Guid LanguageId { get; private set; }
    public DateOnly LocalDate { get; private set; }
    public Guid WordId { get; private set; }
    public bool IsAutomatic { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public void Replace(Guid wordId, DateTimeOffset now) { WordId = wordId; IsAutomatic = false; CreatedAtUtc = now; }
}

public sealed class StudentVocabularyCourse
{
    private StudentVocabularyCourse() { }
    public StudentVocabularyCourse(Guid studentId, Guid languageId, VocabularyLevel level, DateTimeOffset now)
    { StudentId = studentId; LanguageId = languageId; Level = level; UpdatedAtUtc = now; }
    public Guid StudentId { get; private set; }
    public Guid LanguageId { get; private set; }
    public VocabularyLevel Level { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public void Change(Guid languageId, VocabularyLevel level, DateTimeOffset now) { LanguageId = languageId; Level = level; UpdatedAtUtc = now; }
}

public sealed class StudentWordProgress
{
    private StudentWordProgress() { }
    public StudentWordProgress(Guid studentId, Guid wordId) { StudentId = studentId; WordId = wordId; }
    public Guid StudentId { get; private set; }
    public Guid WordId { get; private set; }
    public int MasteryLevel { get; private set; }
    public int CorrectCount { get; private set; }
    public int WrongCount { get; private set; }
    public DateTimeOffset? LastPracticedAtUtc { get; private set; }
    public DateOnly? NextReviewDate { get; private set; }
    public void Apply(bool correct, DateOnly localDate, DateTimeOffset now)
    {
        LastPracticedAtUtc = now;
        if (!correct) { WrongCount++; MasteryLevel = 0; NextReviewDate = localDate.AddDays(1); return; }
        CorrectCount++; MasteryLevel = Math.Min(5, MasteryLevel + 1);
        int[] intervals = [1, 3, 7, 14, 30]; NextReviewDate = localDate.AddDays(intervals[MasteryLevel - 1]);
    }
}

public sealed class VocabularySession : Entity
{
    private VocabularySession() { }
    public VocabularySession(Guid id, Guid studentId, Guid languageId, VocabularySessionMode mode, VocabularyLevel level, Guid? topicId, DateOnly localDate, int questionCount, DateTimeOffset now)
    { Id = id; StudentId = studentId; LanguageId = languageId; Mode = mode; Level = level; TopicId = topicId; LocalDate = localDate; QuestionCount = questionCount; Status = VocabularySessionStatus.InProgress; StartedAtUtc = now; }
    public Guid StudentId { get; private set; }
    public Guid LanguageId { get; private set; }
    public VocabularySessionMode Mode { get; private set; }
    public VocabularyLevel Level { get; private set; }
    public Guid? TopicId { get; private set; }
    public DateOnly LocalDate { get; private set; }
    public int QuestionCount { get; private set; }
    public VocabularySessionStatus Status { get; private set; }
    public int CorrectCount { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public void Complete(int correctCount, DateTimeOffset now) { CorrectCount = correctCount; Status = VocabularySessionStatus.Completed; CompletedAtUtc = now; }
}

public sealed class VocabularySessionQuestion
{
    private VocabularySessionQuestion() { }
    public VocabularySessionQuestion(Guid sessionId, Guid questionId, Guid wordId, int order, VocabularyQuestionType type, string prompt, int? correctTokenIndex, string optionsJson, string correctAnswerJson)
    { SessionId = sessionId; QuestionId = questionId; WordId = wordId; Order = order; Type = type; Prompt = prompt; CorrectTokenIndex = correctTokenIndex; OptionsJson = optionsJson; CorrectAnswerJson = correctAnswerJson; }
    public Guid SessionId { get; private set; }
    public Guid QuestionId { get; private set; }
    public Guid WordId { get; private set; }
    public int Order { get; private set; }
    public VocabularyQuestionType Type { get; private set; }
    public string Prompt { get; private set; } = string.Empty;
    public int? CorrectTokenIndex { get; private set; }
    public string OptionsJson { get; private set; } = "[]";
    public string CorrectAnswerJson { get; private set; } = "{}";
}

public sealed class VocabularySessionAnswer
{
    private VocabularySessionAnswer() { }
    public VocabularySessionAnswer(Guid sessionId, Guid questionId, string submissionJson, bool isCorrect, DateTimeOffset answeredAtUtc)
    { SessionId = sessionId; QuestionId = questionId; SubmissionJson = submissionJson; IsCorrect = isCorrect; AnsweredAtUtc = answeredAtUtc; }
    public Guid SessionId { get; private set; }
    public Guid QuestionId { get; private set; }
    public string SubmissionJson { get; private set; } = "{}";
    public bool IsCorrect { get; private set; }
    public DateTimeOffset AnsweredAtUtc { get; private set; }
}
