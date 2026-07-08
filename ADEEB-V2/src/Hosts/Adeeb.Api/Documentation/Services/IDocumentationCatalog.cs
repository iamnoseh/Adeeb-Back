using Adeeb.Api.Documentation.Models;

namespace Adeeb.Api.Documentation.Services;

public interface IDocumentationCatalog
{
    IReadOnlyList<DocumentationItem> GetAll();
    DocumentationItem? GetBySlug(string slug);
    IReadOnlyList<DocumentationItem> Search(string query);
}
