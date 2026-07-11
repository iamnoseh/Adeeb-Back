using System.Text.RegularExpressions;

namespace Adeeb.ArchitectureTests;

public sealed class DocumentationCoverageTests
{
    private static readonly (string Method, string Path, string Id)[] StableApiRoutes =
    [
        ("POST", "/api/v2/auth/register", "Auth.Register"),
        ("POST", "/api/v2/auth/login", "Auth.Login"),
        ("POST", "/api/v2/auth/refresh", "Auth.RefreshToken"),
        ("POST", "/api/v2/auth/logout", "Auth.Logout"),
        ("POST", "/api/v2/auth/logout-all", "Auth.LogoutAll"),
        ("GET", "/api/v2/auth/sessions", "Auth.GetSessions"),
        ("DELETE", "/api/v2/auth/sessions/{sessionId}", "Auth.RevokeSession"),
        ("GET", "/api/v2/auth/me", "Auth.Me"),
        ("POST", "/api/v2/auth/change-password", "Auth.ChangePassword"),
        ("GET", "/api/v2/commerce/me/entitlements", "Commerce.MeEntitlements")
    ];

    private static readonly string[] RequiredSections =
    [
        "## 1. Endpoint",
        "## 2. Purpose",
        "## 3. Status",
        "## 4. Module",
        "## 5. Authentication",
        "## 6. Authorization",
        "## 7. Rate Limit",
        "## 8. Localization",
        "## 9. Request Headers",
        "## 10. Path Parameters",
        "## 11. Query Parameters",
        "## 12. Request Body",
        "## 13. Field Rules",
        "## 14. Success Response",
        "## 15. Error Responses",
        "## 16. Stable Error Codes",
        "## 17. Frontend Behavior",
        "## 18. Retry Policy",
        "## 19. Caching",
        "## 20. Idempotency",
        "## 21. Security Notes",
        "## 22. Example Flow",
        "## 23. Related Endpoints",
        "## 24. Change History"
    ];

    [Fact]
    public void Every_stable_api_route_has_matching_documentation()
    {
        var docs = LoadRouteDocs();
        var failures = new List<string>();

        foreach (var route in StableApiRoutes)
        {
            var doc = docs.SingleOrDefault(x => x.Id == route.Id);
            if (doc is null)
            {
                failures.Add($"Missing documentation: {route.Method} {route.Path} ({route.Id})");
                continue;
            }

            if (!string.Equals(doc.Method, route.Method, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"Method mismatch for {route.Id}: doc={doc.Method}, actual={route.Method}");
            }

            if (!string.Equals(doc.Route, route.Path, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"Path mismatch for {route.Id}: doc={doc.Route}, actual={route.Path}");
            }

            foreach (var section in RequiredSections)
            {
                if (!doc.Content.Contains(section, StringComparison.Ordinal))
                {
                    failures.Add($"Missing required section in {route.Id}: {section}");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void Documentation_ids_and_slugs_are_unique_and_front_matter_is_valid()
    {
        var docs = LoadRouteDocs().Concat(LoadNonRouteDocs()).ToList();

        var duplicateIds = docs.GroupBy(x => x.Id).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
        var duplicateSlugs = docs.GroupBy(x => x.Slug).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
        var missingRequiredMetadata = docs.Where(x => string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.Title) || string.IsNullOrWhiteSpace(x.Status)).Select(x => x.Slug).ToList();

        Assert.True(duplicateIds.Count == 0, "Duplicate doc ids: " + string.Join(", ", duplicateIds));
        Assert.True(duplicateSlugs.Count == 0, "Duplicate doc slugs: " + string.Join(", ", duplicateSlugs));
        Assert.True(missingRequiredMetadata.Count == 0, "Malformed front matter: " + string.Join(", ", missingRequiredMetadata));
    }

    private static IReadOnlyList<Doc> LoadRouteDocs() =>
        LoadDocs().Where(x => x.Slug.StartsWith("api/", StringComparison.OrdinalIgnoreCase)).ToList();

    private static IReadOnlyList<Doc> LoadNonRouteDocs() =>
        LoadDocs().Where(x => x.Slug.StartsWith("flows/", StringComparison.OrdinalIgnoreCase) || x.Slug.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase)).ToList();

    private static IReadOnlyList<Doc> LoadDocs()
    {
        var root = FindRepoRoot();
        var docsRoot = Path.Combine(root, "docs");
        return Directory.GetFiles(docsRoot, "*.md", SearchOption.AllDirectories)
            .Where(path =>
            {
                var category = Path.GetRelativePath(docsRoot, path).Replace('\\', '/').Split('/')[0];
                return category is "api" or "flows" or "frontend";
            })
            .Select(path => ParseDoc(docsRoot, path))
            .ToList();
    }

    private static Doc ParseDoc(string docsRoot, string path)
    {
        var text = File.ReadAllText(path);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var match = Regex.Match(text, @"\A---\s*(.*?)\s*---", RegexOptions.Singleline);
        Assert.True(match.Success, $"Missing front matter: {path}");
        foreach (var line in match.Groups[1].Value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var split = line.IndexOf(':');
            Assert.True(split > 0, $"Malformed front matter line in {path}: {line}");
            metadata[line[..split].Trim()] = line[(split + 1)..].Trim().Trim('"');
        }

        var slug = Path.GetRelativePath(docsRoot, path).Replace('\\', '/')[..^3];
        return new Doc(
            slug,
            metadata.GetValueOrDefault("id", ""),
            metadata.GetValueOrDefault("title", ""),
            metadata.GetValueOrDefault("method"),
            metadata.GetValueOrDefault("route"),
            metadata.GetValueOrDefault("status", ""),
            text);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Adeeb.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
    }

    private sealed record Doc(string Slug, string Id, string Title, string? Method, string? Route, string Status, string Content);
}
