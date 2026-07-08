namespace Adeeb.Api.Documentation.Models;

public sealed record DocumentationItem(
    string Id,
    string Title,
    string Module,
    string? Method,
    string? Route,
    string Status,
    string Auth,
    bool FrontendReady,
    int Order,
    string Slug,
    string Category,
    string Markdown,
    string Html);
