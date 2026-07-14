namespace Adeeb.Modules.Commerce.Infrastructure.Files;

public sealed class PrivateFileStorageOptions
{
    public const string SectionName = "PrivateFileStorage";

    public string Provider { get; init; } = "Local";
    public string LocalRoot { get; init; } = "data/private";
    public string? ServiceUrl { get; init; }
    public string? Region { get; init; }
    public string? Bucket { get; init; }
    public string? AccessKey { get; init; }
    public string? SecretKey { get; init; }
    public bool ForcePathStyle { get; init; } = true;
}
