using Microsoft.AspNetCore.Http;

namespace Adeeb.Modules.AcademicCatalog.Contracts;

public sealed record TranslationRequest(int Language, string Name, string? Description);
public sealed record SubjectUpsertRequest(string Code, string? IconUrl, int DisplayOrder, int Status, IReadOnlyList<TranslationRequest> Translations);
public sealed record TopicUpsertRequest(Guid SubjectId, string Code, int DisplayOrder, int Status, IReadOnlyList<TranslationRequest> Translations);
public sealed class SubjectFormRequest
{
    public string? Name { get; init; }
    public string? NameTg { get; init; }
    public string? NameRu { get; init; }
    public string? NameEn { get; init; }
    public string? DescriptionTg { get; init; }
    public string? DescriptionRu { get; init; }
    public string? DescriptionEn { get; init; }
    public IFormFile? Icon { get; init; }
    public int? Status { get; init; }
    public int? DisplayOrder { get; init; }
}
public sealed record AcademicListQuery(string? Search, int? Status, int Page = 1, int PageSize = 20, string? Sort = null);
public sealed record TranslationResponse(int Language, string Name, string? Description);
public sealed record SubjectResponse(Guid Id, string Code, string Name, string? IconUrl, int DisplayOrder, int Status, IReadOnlyList<TranslationResponse> Translations);
public sealed record TopicResponse(Guid Id, Guid SubjectId, string Code, string Name, int DisplayOrder, int Status, IReadOnlyList<TranslationResponse> Translations);
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record CatalogLookupItem(Guid Id, string Code, string Name, int Status);
public sealed record AcademicSubjectLookupItem(Guid Id, string Code, string Name);

public interface IAcademicCatalogLookup
{
    Task<bool> SubjectExistsAsync(Guid subjectId, CancellationToken ct);
    Task<bool> TopicBelongsToSubjectAsync(Guid topicId, Guid subjectId, CancellationToken ct);
    Task<IReadOnlyList<AcademicSubjectLookupItem>> GetActiveSubjectsAsync(
        IReadOnlyCollection<Guid> subjectIds,
        Adeeb.Application.Abstractions.Localization.SupportedLanguage language,
        CancellationToken ct);
}
