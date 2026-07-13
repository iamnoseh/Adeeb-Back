using Amazon.S3;
using Amazon.S3.Model;
using Adeeb.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.Commerce.Infrastructure.Files;

public sealed class S3PrivateFileStorage(IAmazonS3 client, IOptions<PrivateFileStorageOptions> options) : IPrivateFileStorage, IPrivateFileMaintenance
{
    private readonly string _bucket = options.Value.Bucket!;

    public async Task<StoredFile> SaveAsync(Stream stream, string contentType, string objectKey, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        var sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
        memory.Position = 0;
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = objectKey,
            InputStream = memory,
            ContentType = contentType,
            AutoCloseStream = false
        }, cancellationToken);
        return new StoredFile(objectKey, contentType, bytes.LongLength, sha256);
    }

    public async Task<PrivateFileReadResult?> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.GetObjectAsync(_bucket, objectKey, cancellationToken);
            return new PrivateFileReadResult(response.ResponseStream, response.Headers.ContentType ?? "application/octet-stream", response.ContentLength);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
        client.DeleteObjectAsync(_bucket, objectKey, cancellationToken);

    public Task<string> CreateSignedReadUrlAsync(string objectKey, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromMinutes(5))
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime), "Signed URL lifetime must be between zero and five minutes.");
        }

        return client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(lifetime),
            Verb = HttpVerb.GET
        });
    }

    public async Task<IReadOnlyList<string>> ListOlderThanAsync(string prefix, DateTimeOffset cutoffUtc, CancellationToken cancellationToken)
    {
        var keys = new List<string>();
        string? continuationToken = null;
        do
        {
            var response = await client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucket,
                Prefix = prefix,
                ContinuationToken = continuationToken
            }, cancellationToken);
            keys.AddRange(response.S3Objects
                .Where(x => x.LastModified < cutoffUtc.UtcDateTime)
                .Select(x => x.Key));
            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (continuationToken is not null);

        return keys;
    }
}
