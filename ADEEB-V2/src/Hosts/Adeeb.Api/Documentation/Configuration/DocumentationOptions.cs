namespace Adeeb.Api.Documentation.Configuration;

public sealed class DocumentationOptions
{
    public const string SectionName = "Documentation";

    public bool Enabled { get; init; } = true;
    public bool RequireAuthorization { get; init; }
    public string DocsRoot { get; init; } = "docs";
}
