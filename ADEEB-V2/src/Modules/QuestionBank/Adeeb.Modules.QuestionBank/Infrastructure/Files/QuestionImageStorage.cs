using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Adeeb.Modules.QuestionBank.Infrastructure.Files;

public sealed class QuestionImageStorage(IWebHostEnvironment environment)
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public async Task<Result<string?>> SaveAsync(IFormFile? image, CancellationToken ct)
    {
        if (image is null || image.Length == 0)
        {
            return Result<string?>.Success(null);
        }

        var extension = Path.GetExtension(image.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return Result<string?>.Failure(Error.Validation("question.image.invalid_type", "QuestionBank.InvalidImageType"));
        }

        if (image.Length > 5 * 1024 * 1024)
        {
            return Result<string?>.Failure(Error.Validation("question.image.too_large", "QuestionBank.ImageTooLarge"));
        }

        var webRoot = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(environment.ContentRootPath, "wwwroot");
        }

        var directory = Path.Combine(webRoot, "uploads", "questions");
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var path = Path.Combine(directory, fileName);
        await using var stream = File.Create(path);
        await image.CopyToAsync(stream, ct);
        return Result<string?>.Success($"/uploads/questions/{fileName}");
    }
}
