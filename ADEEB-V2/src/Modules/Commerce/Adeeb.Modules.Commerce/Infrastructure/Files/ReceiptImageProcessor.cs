using System.Security.Cryptography;
using Adeeb.Modules.Commerce.Application;
using Adeeb.Modules.Commerce.Application.Storage;
using Adeeb.SharedKernel.Results;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace Adeeb.Modules.Commerce.Infrastructure.Files;

public sealed class ReceiptImageProcessor : IReceiptImageProcessor
{
    public const long MaxFileSize = 10 * 1024 * 1024;
    public const int MaxWidth = 6000;
    public const int MaxHeight = 6000;
    public const long MaxPixels = 24_000_000;

    private static readonly HashSet<string> AllowedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "JPEG",
        "PNG",
        "WEBP"
    };

    public async Task<Result<ValidatedReceiptImage>> ProcessAsync(Stream input, long declaredLength, CancellationToken cancellationToken)
    {
        if (declaredLength <= 0)
        {
            return Result<ValidatedReceiptImage>.Failure(CommerceErrors.ReceiptImageRequired);
        }

        if (declaredLength > MaxFileSize)
        {
            return Result<ValidatedReceiptImage>.Failure(CommerceErrors.ImageTooLarge);
        }

        try
        {
            using var source = new MemoryStream((int)declaredLength);
            await input.CopyToAsync(source, cancellationToken);
            if (source.Length == 0 || source.Length > MaxFileSize)
            {
                return Result<ValidatedReceiptImage>.Failure(
                    source.Length == 0 ? CommerceErrors.ReceiptImageRequired : CommerceErrors.ImageTooLarge);
            }

            var bytes = source.ToArray();
            var format = Image.DetectFormat(bytes);
            if (format is null || !AllowedFormats.Contains(format.Name))
            {
                return Result<ValidatedReceiptImage>.Failure(CommerceErrors.ReceiptImageInvalidType);
            }

            using var image = Image.Load(bytes);
            if (image.Width > MaxWidth || image.Height > MaxHeight || (long)image.Width * image.Height > MaxPixels)
            {
                return Result<ValidatedReceiptImage>.Failure(CommerceErrors.ImageDimensionsInvalid);
            }

            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.XmpProfile = null;
            await using var output = new MemoryStream();
            await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = 85 }, cancellationToken);
            var content = output.ToArray();
            var sha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
            return Result<ValidatedReceiptImage>.Success(new(content, "image/webp", sha256, image.Width, image.Height));
        }
        catch (UnknownImageFormatException)
        {
            return Result<ValidatedReceiptImage>.Failure(CommerceErrors.ReceiptImageInvalidType);
        }
        catch (InvalidImageContentException)
        {
            return Result<ValidatedReceiptImage>.Failure(CommerceErrors.ReceiptImageCorrupted);
        }
    }
}
