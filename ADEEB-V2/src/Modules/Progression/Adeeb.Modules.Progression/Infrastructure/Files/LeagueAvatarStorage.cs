using Adeeb.Modules.Progression.Application;
using Adeeb.SharedKernel.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;

namespace Adeeb.Modules.Progression.Infrastructure.Files;

public sealed class LeagueAvatarStorage(IWebHostEnvironment environment)
{
    private const long MaxBytes = 2 * 1024 * 1024;
    public async Task<Result<string?>> SaveAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return Result<string?>.Success(null);
        if (file.Length > MaxBytes) return Result<string?>.Failure(ProgressionErrors.AvatarInvalid);
        try
        {
            await using var input = file.OpenReadStream();
            var format = await Image.DetectFormatAsync(input, ct);
            var extension = format?.Name.ToLowerInvariant() switch { "jpeg" => ".jpg", "png" => ".png", "webp" => ".webp", _ => null };
            if (extension is null) return Result<string?>.Failure(ProgressionErrors.AvatarInvalid);
            input.Position = 0;
            var root = string.IsNullOrWhiteSpace(environment.WebRootPath) ? Path.Combine(environment.ContentRootPath, "wwwroot") : environment.WebRootPath;
            var directory = Path.Combine(root, "uploads", "progression", "leagues"); Directory.CreateDirectory(directory);
            var name = $"{Guid.NewGuid():N}{extension}";
            await using var output = File.Create(Path.Combine(directory, name)); await input.CopyToAsync(output, ct);
            return Result<string?>.Success($"/uploads/progression/leagues/{name}");
        }
        catch (OperationCanceledException) { throw; }
        catch { return Result<string?>.Failure(ProgressionErrors.AvatarInvalid); }
    }
}
