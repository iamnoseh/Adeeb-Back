using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Adeeb.Modules.Commerce.Infrastructure.Files;

public sealed class CommerceImageStorage(IWebHostEnvironment environment)
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public Task<Result<string?>> SaveQrAsync(IFormFile? image, CancellationToken ct) =>
        SaveAsync(image, "commerce/qr", "commerce.qr_image.invalid_type", "Commerce.QrImage.InvalidType", ct);

    private async Task<Result<string?>> SaveAsync(IFormFile? image, string folder, string invalidCode, string invalidKey, CancellationToken ct)
    {
        if (image is null || image.Length == 0)
        {
            return Result<string?>.Success(null);
        }

        var extension = Path.GetExtension(image.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return Result<string?>.Failure(Error.Validation(invalidCode, invalidKey));
        }

        if (image.Length > 10 * 1024 * 1024)
        {
            return Result<string?>.Failure(Error.Validation("commerce.image.too_large", "Commerce.Image.TooLarge"));
        }

        var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        var directory = Path.Combine(webRoot, "uploads", folder);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using var stream = File.Create(Path.Combine(directory, fileName));
        await image.CopyToAsync(stream, ct);
        return Result<string?>.Success($"/uploads/{folder}/{fileName}");
    }
}
