namespace Adeeb.Application.Abstractions.Storage;

public sealed record StoredFile(
    string ObjectKey,
    string ContentType,
    long Size,
    string Sha256);

public sealed record PrivateFileReadResult(
    Stream Content,
    string ContentType,
    long? Size);

public interface IPrivateFileStorage
{
    Task<StoredFile> SaveAsync(
        Stream stream,
        string contentType,
        string objectKey,
        CancellationToken cancellationToken);

    Task<PrivateFileReadResult?> OpenReadAsync(
        string objectKey,
        CancellationToken cancellationToken);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);

    Task<string> CreateSignedReadUrlAsync(
        string objectKey,
        TimeSpan lifetime,
        CancellationToken cancellationToken);
}
