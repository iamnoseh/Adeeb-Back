using System.Security.Cryptography;
using Adeeb.Application.Abstractions.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.Commerce.Infrastructure.Files;

public sealed class LocalPrivateFileStorage : IPrivateFileStorage, IPrivateFileMaintenance
{
    private readonly string _root;

    public LocalPrivateFileStorage(IWebHostEnvironment environment, IOptions<PrivateFileStorageOptions> options)
    {
        var configuredRoot = options.Value.LocalRoot;
        _root = Path.GetFullPath(Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(environment.ContentRootPath, configuredRoot));
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFile> SaveAsync(Stream stream, string contentType, string objectKey, CancellationToken cancellationToken)
    {
        var path = Resolve(objectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long size = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            hash.AppendData(buffer, 0, read);
            size += read;
        }

        return new StoredFile(objectKey, contentType, size, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }

    public Task<PrivateFileReadResult?> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        var path = Resolve(objectKey);
        if (!File.Exists(path))
        {
            return Task.FromResult<PrivateFileReadResult?>(null);
        }

        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return Task.FromResult<PrivateFileReadResult?>(new(stream, "image/webp", stream.Length));
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        var path = Resolve(objectKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public Task<string> CreateSignedReadUrlAsync(string objectKey, TimeSpan lifetime, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Local private storage uses authorized API streaming.");

    public Task<IReadOnlyList<string>> ListOlderThanAsync(string prefix, DateTimeOffset cutoffUtc, CancellationToken cancellationToken)
    {
        var prefixPath = Resolve(prefix.TrimEnd('/') + "/placeholder");
        var directory = Path.GetDirectoryName(prefixPath)!;
        if (!Directory.Exists(directory))
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(x => File.GetLastWriteTimeUtc(x) < cutoffUtc.UtcDateTime)
            .Select(x => Path.GetRelativePath(_root, x).Replace(Path.DirectorySeparatorChar, '/'))
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(files);
    }

    private string Resolve(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey) || Path.IsPathRooted(objectKey))
        {
            throw new ArgumentException("Object key is invalid.", nameof(objectKey));
        }

        var normalized = objectKey.Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(_root, normalized));
        var prefix = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Object key escapes private storage root.", nameof(objectKey));
        }

        return path;
    }
}
