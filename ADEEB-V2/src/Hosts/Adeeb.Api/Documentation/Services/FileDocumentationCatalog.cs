using System.Text.Json;
using Adeeb.Api.Documentation.Configuration;
using Adeeb.Api.Documentation.Models;
using Adeeb.Api.Documentation.Rendering;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Adeeb.Api.Documentation.Services;

public sealed class FileDocumentationCatalog(
    IOptions<DocumentationOptions> options,
    IWebHostEnvironment environment,
    SafeMarkdownRenderer renderer) : IDocumentationCatalog
{
    private readonly object _lock = new();
    private IReadOnlyList<DocumentationItem>? _cache;

    public IReadOnlyList<DocumentationItem> GetAll()
    {
        if (environment.IsDevelopment())
        {
            return Load();
        }

        if (_cache is not null)
        {
            return _cache;
        }

        lock (_lock)
        {
            _cache ??= Load();
            return _cache;
        }
    }

    public DocumentationItem? GetBySlug(string slug) =>
        GetAll().FirstOrDefault(x => string.Equals(x.Slug, slug.Trim('/'), StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<DocumentationItem> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return GetAll();
        }

        var q = query.Trim();
        return GetAll()
            .Where(x => Contains(x.Title, q) || Contains(x.Route, q) || Contains(x.Method, q) || Contains(x.Module, q) || Contains(x.Id, q) || Contains(x.Markdown, q))
            .ToList();
    }

    private IReadOnlyList<DocumentationItem> Load()
    {
        var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "..", options.Value.DocsRoot));
        if (!Directory.Exists(root))
        {
            root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.DocsRoot));
        }

        if (!Directory.Exists(root))
        {
            return [];
        }

        var allowedCategories = new HashSet<string>(["api", "flows", "frontend"], StringComparer.OrdinalIgnoreCase);
        return Directory.GetFiles(root, "*.md", SearchOption.AllDirectories)
            .Where(file => allowedCategories.Contains(Path.GetRelativePath(root, file).Replace('\\', '/').Split('/')[0]))
            .Select(file => Parse(root, file))
            .OrderBy(x => CategoryOrder(x.Category))
            .ThenBy(x => x.Module)
            .ThenBy(x => x.Order)
            .ThenBy(x => x.Title)
            .ToList();
    }

    private DocumentationItem Parse(string root, string file)
    {
        var text = File.ReadAllText(file);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var markdown = text;

        if (text.StartsWith("---", StringComparison.Ordinal))
        {
            var end = text.IndexOf("\n---", 3, StringComparison.Ordinal);
            if (end > 0)
            {
                var frontMatter = text[3..end].Trim();
                markdown = text[(end + 4)..].TrimStart();
                foreach (var line in frontMatter.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var split = line.IndexOf(':');
                    if (split > 0)
                    {
                        metadata[line[..split].Trim()] = line[(split + 1)..].Trim().Trim('"');
                    }
                }
            }
        }

        var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
        var slug = relative[..^3];
        var category = slug.Split('/')[0];
        return new DocumentationItem(
            Get(metadata, "id", slug),
            Get(metadata, "title", Path.GetFileNameWithoutExtension(file)),
            Get(metadata, "module", category),
            GetNullable(metadata, "method"),
            GetNullable(metadata, "route"),
            Get(metadata, "status", "Stable"),
            Get(metadata, "auth", "Unknown"),
            bool.TryParse(Get(metadata, "frontendReady", "false"), out var frontendReady) && frontendReady,
            int.TryParse(Get(metadata, "order", "1000"), out var order) ? order : 1000,
            slug,
            category,
            markdown,
            renderer.Render(markdown));
    }

    private static string Get(Dictionary<string, string> metadata, string key, string fallback) =>
        metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    private static string? GetNullable(Dictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static bool Contains(string? value, string query) =>
        value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;

    private static int CategoryOrder(string category) =>
        category switch
        {
            "api" => 10,
            "flows" => 20,
            "frontend" => 30,
            _ => 100
        };
}
