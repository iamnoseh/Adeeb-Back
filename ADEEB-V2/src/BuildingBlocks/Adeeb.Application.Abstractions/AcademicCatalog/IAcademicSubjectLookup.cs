using Adeeb.Application.Abstractions.Localization;

namespace Adeeb.Application.Abstractions.AcademicCatalog;

public sealed record AcademicSubjectLookupItem(Guid Id, string Code, string Name);

public interface IAcademicSubjectLookup
{
    Task<IReadOnlyList<AcademicSubjectLookupItem>> GetActiveSubjectsAsync(
        IReadOnlyCollection<Guid> subjectIds,
        SupportedLanguage language,
        CancellationToken ct);
}
