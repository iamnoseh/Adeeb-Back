using System.Security.Cryptography;
using Adeeb.Application.Abstractions.Storage;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Commerce.Application.Storage;
using Adeeb.Modules.Commerce.Domain.Payments;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.Commerce.Application.LegacyFiles;

public sealed class LegacyReceiptFileMigrator(
    CommerceDbContext db,
    IPrivateFileStorage privateStorage,
    IReceiptImageProcessor imageProcessor,
    IDateTimeProvider clock,
    IWebHostEnvironment environment)
{
    private const string PublicPrefix = "/uploads/commerce/receipts/";
    private const string RewrittenLegacyPrefix = "commerce/payment-receipts/legacy/";

    public async Task<LegacyReceiptMigrationReport> RunAsync(bool dryRun, CancellationToken cancellationToken)
    {
        var receipts = await db.PaymentReceipts.OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var results = new List<LegacyReceiptMigrationItem>();
        var candidates = new List<(PaymentReceipt Receipt, string SourcePath, string DisplaySource, string OriginalReference)>();
        foreach (var receipt in receipts)
        {
            if (!TryResolveSource(receipt.ReceiptImageObjectKey, out var sourcePath, out var displaySource, out var reason))
            {
                results.Add(new(
                    receipt.Id,
                    IsLegacyReference(receipt.ReceiptImageObjectKey)
                        ? LegacyReceiptMigrationStatus.Failed
                        : LegacyReceiptMigrationStatus.AlreadyMigrated,
                    displaySource,
                    receipt.ReceiptImageObjectKey,
                    reason));
                continue;
            }

            candidates.Add((receipt, sourcePath!, displaySource!, receipt.ReceiptImageObjectKey));
        }

        foreach (var group in candidates.GroupBy(x => x.SourcePath, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(group.Key))
            {
                results.AddRange(group.Select(x => new LegacyReceiptMigrationItem(
                    x.Receipt.Id,
                    LegacyReceiptMigrationStatus.MissingSource,
                    x.DisplaySource,
                    null,
                    "Source file does not exist.")));
                continue;
            }

            if (dryRun)
            {
                results.AddRange(group.Select(x => new LegacyReceiptMigrationItem(
                    x.Receipt.Id,
                    LegacyReceiptMigrationStatus.Skipped,
                    x.DisplaySource,
                    null,
                    "Dry run: source is eligible for migration.")));
                continue;
            }

            try
            {
                await using var source = new FileStream(group.Key, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
                var processed = await imageProcessor.ProcessAsync(source, source.Length, cancellationToken);
                await source.DisposeAsync();
                if (processed.IsFailure)
                {
                    results.AddRange(group.Select(x => new LegacyReceiptMigrationItem(
                        x.Receipt.Id,
                        LegacyReceiptMigrationStatus.Failed,
                        x.DisplaySource,
                        null,
                        processed.Error!.Code)));
                    continue;
                }

                var image = processed.Value!;
                var destination = $"commerce/payment-receipts/legacy-migrated/{image.Sha256}.webp";
                await EnsureDestinationAsync(destination, image, cancellationToken);
                var groupSucceeded = true;
                foreach (var candidate in group)
                {
                    try
                    {
                        candidate.Receipt.CompleteLegacyImageMigration(destination, clock.UtcNow);
                        await db.SaveChangesAsync(cancellationToken);
                        results.Add(new(candidate.Receipt.Id, LegacyReceiptMigrationStatus.Migrated, candidate.DisplaySource, destination, null));
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        db.Entry(candidate.Receipt).Property(x => x.ReceiptImageObjectKey).CurrentValue = candidate.OriginalReference;
                        db.Entry(candidate.Receipt).State = EntityState.Unchanged;
                        groupSucceeded = false;
                        results.Add(new(candidate.Receipt.Id, LegacyReceiptMigrationStatus.Failed, candidate.DisplaySource, destination, exception.GetType().Name));
                    }
                }

                if (groupSucceeded)
                {
                    File.Delete(group.Key);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                results.AddRange(group.Select(x => new LegacyReceiptMigrationItem(
                    x.Receipt.Id,
                    LegacyReceiptMigrationStatus.Failed,
                    x.DisplaySource,
                    null,
                    exception.GetType().Name)));
            }
        }

        return new LegacyReceiptMigrationReport(results);
    }

    private async Task EnsureDestinationAsync(
        string objectKey,
        ValidatedReceiptImage image,
        CancellationToken cancellationToken)
    {
        var existing = await privateStorage.OpenReadAsync(objectKey, cancellationToken);
        if (existing is null)
        {
            await using var content = new MemoryStream(image.Content, writable: false);
            await privateStorage.SaveAsync(content, image.ContentType, objectKey, cancellationToken);
        }
        else
        {
            await existing.Content.DisposeAsync();
        }

        var stored = await privateStorage.OpenReadAsync(objectKey, cancellationToken)
            ?? throw new IOException("Private destination could not be read after write.");
        await using var storedContent = stored.Content;
        var storedHash = Convert.ToHexString(await SHA256.HashDataAsync(storedContent, cancellationToken)).ToLowerInvariant();
        if (!string.Equals(storedHash, image.Sha256, StringComparison.Ordinal))
        {
            throw new IOException("Private destination hash verification failed.");
        }
    }

    private bool TryResolveSource(
        string reference,
        out string? sourcePath,
        out string? displaySource,
        out string? reason)
    {
        sourcePath = null;
        displaySource = reference;
        reason = null;
        if (!IsLegacyReference(reference))
        {
            return false;
        }

        var relative = reference.StartsWith(PublicPrefix, StringComparison.OrdinalIgnoreCase)
            ? reference[PublicPrefix.Length..]
            : reference[RewrittenLegacyPrefix.Length..];
        try
        {
            relative = Uri.UnescapeDataString(relative).Replace('/', Path.DirectorySeparatorChar);
        }
        catch (UriFormatException)
        {
            reason = "Legacy path encoding is invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
        {
            reason = "Legacy path is invalid.";
            return false;
        }

        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var root = Path.GetFullPath(Path.Combine(webRoot, "uploads", "commerce", "receipts"));
        var resolved = Path.GetFullPath(Path.Combine(root, relative));
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            reason = "Legacy path escapes the public receipt root.";
            return false;
        }

        sourcePath = resolved;
        displaySource = Path.GetRelativePath(webRoot, resolved).Replace(Path.DirectorySeparatorChar, '/');
        return true;
    }

    private static bool IsLegacyReference(string reference) =>
        reference.StartsWith(PublicPrefix, StringComparison.OrdinalIgnoreCase) ||
        reference.StartsWith(RewrittenLegacyPrefix, StringComparison.OrdinalIgnoreCase);
}
