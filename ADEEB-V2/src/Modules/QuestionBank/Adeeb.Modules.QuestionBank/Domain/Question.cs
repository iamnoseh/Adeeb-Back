using Adeeb.Application.Abstractions.Localization;
using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.QuestionBank.Domain;

public sealed class Question : Entity
{
    private readonly List<QuestionTranslation> _translations = [];
    private readonly List<AnswerOption> _answerOptions = [];

    private Question() { }

    public Question(Guid id, Guid subjectId, Guid? topicId, string? topic, QuestionType type, DifficultyLevel difficulty, string? imageUrl, DateTimeOffset now)
    {
        Id = id;
        SubjectId = subjectId;
        TopicId = topicId;
        Topic = string.IsNullOrWhiteSpace(topic) ? null : topic.Trim();
        Type = type;
        Difficulty = difficulty;
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        Status = QuestionStatus.Draft;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid SubjectId { get; private set; }
    public Guid? TopicId { get; private set; }
    public string? Topic { get; private set; }
    public QuestionType Type { get; private set; }
    public DifficultyLevel Difficulty { get; private set; }
    public QuestionStatus Status { get; private set; }
    public string? ImageUrl { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }
    public string? ArchiveReason { get; private set; }
    public uint Version { get; private set; }
    public IReadOnlyCollection<QuestionTranslation> Translations => _translations;
    public IReadOnlyCollection<AnswerOption> AnswerOptions => _answerOptions;

    public void Update(Guid subjectId, Guid? topicId, string? topic, QuestionType type, DifficultyLevel difficulty, QuestionStatus status, string? imageUrl, DateTimeOffset now)
    {
        SubjectId = subjectId;
        TopicId = topicId;
        Topic = string.IsNullOrWhiteSpace(topic) ? null : topic.Trim();
        Type = type;
        Difficulty = difficulty;
        Status = status;
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        ArchivedAtUtc = status == QuestionStatus.Archived ? ArchivedAtUtc ?? now : null;
        UpdatedAtUtc = now;
    }

    public void ReplaceContent(IEnumerable<QuestionTranslation> translations, IEnumerable<AnswerOption> answerOptions)
    {
        foreach (var t in translations)
        {
            var existing = _translations.FirstOrDefault(x => x.Language == t.Language);
            if (existing is not null)
            {
                existing.Update(t.Content, t.Explanation);
            }
            else
            {
                _translations.Add(t);
            }
        }

        var newLanguages = translations.Select(x => x.Language).ToHashSet();
        _translations.RemoveAll(x => !newLanguages.Contains(x.Language));

        foreach (var newOpt in answerOptions)
        {
            var existingOpt = _answerOptions.FirstOrDefault(x => x.DisplayOrder == newOpt.DisplayOrder);
            if (existingOpt is not null)
            {
                existingOpt.Update(newOpt.IsCorrect);
                existingOpt.ReplaceTranslations(newOpt.Translations);
            }
            else
            {
                _answerOptions.Add(newOpt);
            }
        }

        var newDisplayOrders = answerOptions.Select(x => x.DisplayOrder).ToHashSet();
        _answerOptions.RemoveAll(x => !newDisplayOrders.Contains(x.DisplayOrder));
    }

    public void Archive(DateTimeOffset now, string reason)
    {
        Status = QuestionStatus.Archived;
        ArchivedAtUtc = now;
        ArchiveReason = reason;
        UpdatedAtUtc = now;
    }

    public string ContentFor(SupportedLanguage language) =>
        _translations.FirstOrDefault(x => x.Language == language)?.Content
        ?? _translations.FirstOrDefault(x => x.Language == SupportedLanguage.Tajik)?.Content
        ?? string.Empty;
}

public sealed class QuestionTranslation
{
    private QuestionTranslation() { }

    public QuestionTranslation(Guid questionId, SupportedLanguage language, string content, string? explanation)
    {
        QuestionId = questionId;
        Language = language;
        Content = content.Trim();
        Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim();
    }

    public Guid QuestionId { get; private set; }
    public SupportedLanguage Language { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string? Explanation { get; private set; }

    public void Update(string content, string? explanation)
    {
        Content = content.Trim();
        Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim();
    }
}

public sealed class AnswerOption
{
    private readonly List<AnswerOptionTranslation> _translations = [];

    private AnswerOption() { }

    public AnswerOption(Guid id, Guid questionId, int displayOrder, bool isCorrect)
    {
        Id = id;
        QuestionId = questionId;
        DisplayOrder = displayOrder;
        IsCorrect = isCorrect;
    }

    public Guid Id { get; private set; }
    public Guid QuestionId { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsCorrect { get; private set; }
    public IReadOnlyCollection<AnswerOptionTranslation> Translations => _translations;

    public void Update(bool isCorrect)
    {
        IsCorrect = isCorrect;
    }

    public void ReplaceTranslations(IEnumerable<AnswerOptionTranslation> translations)
    {
        foreach (var t in translations)
        {
            var existing = _translations.FirstOrDefault(x => x.Language == t.Language);
            if (existing is not null)
            {
                existing.Update(t.Text, t.MatchPairText);
            }
            else
            {
                _translations.Add(new AnswerOptionTranslation(Id, t.Language, t.Text, t.MatchPairText));
            }
        }

        var newLanguages = translations.Select(x => x.Language).ToHashSet();
        _translations.RemoveAll(x => !newLanguages.Contains(x.Language));
    }
}

public sealed class AnswerOptionTranslation
{
    private AnswerOptionTranslation() { }

    public AnswerOptionTranslation(Guid answerOptionId, SupportedLanguage language, string text, string? matchPairText)
    {
        AnswerOptionId = answerOptionId;
        Language = language;
        Text = text.Trim();
        MatchPairText = string.IsNullOrWhiteSpace(matchPairText) ? null : matchPairText.Trim();
    }

    public Guid AnswerOptionId { get; private set; }
    public SupportedLanguage Language { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public string? MatchPairText { get; private set; }

    public void Update(string text, string? matchPairText)
    {
        Text = text.Trim();
        MatchPairText = string.IsNullOrWhiteSpace(matchPairText) ? null : matchPairText.Trim();
    }
}
