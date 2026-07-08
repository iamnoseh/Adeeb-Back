using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Adeeb.Modules.AcademicCatalog.Infrastructure.Files;

public sealed class AcademicFileStorage(IWebHostEnvironment environment)
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".svg",
        ".webp"
    };

    public async Task<Result<string?>> SaveSubjectIconAsync(IFormFile? icon, CancellationToken ct)
    {
        if (icon is null || icon.Length == 0)
        {
            return Result<string?>.Success(null);
        }

        var extension = Path.GetExtension(icon.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return Result<string?>.Failure(Error.Validation("academic.icon.invalid_type", "Academic.InvalidIconType"));
        }

        if (icon.Length > 10 * 1024 * 1024)
        {
            return Result<string?>.Failure(Error.Validation("academic.icon.too_large", "Academic.IconTooLarge"));
        }

        var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        var directory = Path.Combine(webRoot, "uploads", "courses");
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using var stream = File.Create(Path.Combine(directory, fileName));
        await icon.CopyToAsync(stream, ct);
        return Result<string?>.Success($"/uploads/courses/{fileName}");
    }
}
