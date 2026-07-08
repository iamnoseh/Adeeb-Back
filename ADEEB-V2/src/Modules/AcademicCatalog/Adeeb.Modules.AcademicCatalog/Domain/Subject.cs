using Adeeb.Application.Abstractions.Localization;
using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.AcademicCatalog.Domain;

public sealed class Subject : Entity
{
    private readonly List<SubjectTranslation> _translations = [];
    private readonly List<Topic> _topics = [];

    private Subject() { }

    public Subject(Guid id, string code, string? iconUrl, int displayOrder, DateTimeOffset now)
    {
        Id = id;
        Code = NormalizeCode(code);
        IconUrl = string.IsNullOrWhiteSpace(iconUrl) ? null : iconUrl.Trim();
        DisplayOrder = displayOrder;
        Status = AcademicItemStatus.Draft;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public string Code { get; private set; } = string.Empty;
    public string? IconUrl { get; private set; }
    public int DisplayOrder { get; private set; }
    public AcademicItemStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }
    public string? ArchiveReason { get; private set; }
    public IReadOnlyCollection<SubjectTranslation> Translations => _translations;
    public IReadOnlyCollection<Topic> Topics => _topics;

    public void Update(string code, string? iconUrl, int displayOrder, AcademicItemStatus status, DateTimeOffset now)
    {
        Code = NormalizeCode(code);
        IconUrl = string.IsNullOrWhiteSpace(iconUrl) ? null : iconUrl.Trim();
        DisplayOrder = displayOrder;
        Status = status;
        ArchivedAtUtc = status == AcademicItemStatus.Archived ? ArchivedAtUtc ?? now : null;
        UpdatedAtUtc = now;
    }

    public void ReplaceTranslations(IEnumerable<SubjectTranslation> translations)
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

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}

public sealed class SubjectTranslation
{
    private SubjectTranslation() { }

    public SubjectTranslation(Guid subjectId, SupportedLanguage language, string name, string? description)
    {
        SubjectId = subjectId;
        Language = language;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public Guid SubjectId { get; private set; }
    public SupportedLanguage Language { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
}
