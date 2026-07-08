using Adeeb.Application.Abstractions.Localization;
using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.AcademicCatalog.Domain;

public sealed class Topic : Entity
{
    private readonly List<TopicTranslation> _translations = [];

    private Topic() { }

    public Topic(Guid id, Guid subjectId, string code, int displayOrder, DateTimeOffset now)
    {
        Id = id;
        SubjectId = subjectId;
        Code = Subject.NormalizeCode(code);
        DisplayOrder = displayOrder;
        Status = AcademicItemStatus.Draft;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid SubjectId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public AcademicItemStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }
    public string? ArchiveReason { get; private set; }
    public Subject? Subject { get; private set; }
    public IReadOnlyCollection<TopicTranslation> Translations => _translations;

    public void Update(string code, int displayOrder, AcademicItemStatus status, DateTimeOffset now)
    {
        Code = Subject.NormalizeCode(code);
        DisplayOrder = displayOrder;
        Status = status;
        ArchivedAtUtc = status == AcademicItemStatus.Archived ? ArchivedAtUtc ?? now : null;
        UpdatedAtUtc = now;
    }

    public void ReplaceTranslations(IEnumerable<TopicTranslation> translations)
    {
        _translations.Clear();
        _translations.AddRange(translations);
    }

    public void Archive(DateTimeOffset now, string reason)
    {
        Status = AcademicItemStatus.Archived;
        ArchivedAtUtc = now;
        ArchiveReason = reason;
        UpdatedAtUtc = now;
    }

    public string NameFor(SupportedLanguage language) =>
        _translations.FirstOrDefault(x => x.Language == language)?.Name
        ?? _translations.FirstOrDefault(x => x.Language == SupportedLanguage.Tajik)?.Name
        ?? Code;
}

public sealed class TopicTranslation
{
    private TopicTranslation() { }

    public TopicTranslation(Guid topicId, SupportedLanguage language, string name, string? description)
    {
        TopicId = topicId;
        Language = language;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public Guid TopicId { get; private set; }
    public SupportedLanguage Language { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
}
