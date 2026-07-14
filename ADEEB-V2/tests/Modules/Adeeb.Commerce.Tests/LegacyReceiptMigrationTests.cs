using Adeeb.Application.Abstractions.Storage;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Commerce.Application.LegacyFiles;
using Adeeb.Modules.Commerce.Domain.Payments;
using Adeeb.Modules.Commerce.Infrastructure.Files;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Adeeb.Commerce.Tests;

public sealed class LegacyReceiptMigrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "adeeb-legacy-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Successful_migration_is_restartable_and_removes_public_source()
    {
        await using var db = CreateDb();
        var receipt = Receipt("commerce/payment-receipts/legacy/receipt.png");
        db.Add(receipt);
        await db.SaveChangesAsync();
        var source = await CreateSourceAsync("receipt.png");
        var storage = new RecordingStorage();
        var migrator = CreateMigrator(db, storage);

        var first = await migrator.RunAsync(dryRun: false, CancellationToken.None);
        var second = await migrator.RunAsync(dryRun: false, CancellationToken.None);

        Assert.Equal(1, first.Migrated);
        Assert.Equal(1, second.AlreadyMigrated);
        Assert.Equal(1, storage.SaveCount);
        Assert.StartsWith("commerce/payment-receipts/legacy-migrated/", receipt.ReceiptImageObjectKey, StringComparison.Ordinal);
        Assert.False(File.Exists(source));
    }

    [Fact]
    public async Task Dry_run_reports_candidate_without_writing_or_deleting()
    {
        await using var db = CreateDb();
        db.Add(Receipt("/uploads/commerce/receipts/dry-run.png"));
        await db.SaveChangesAsync();
        var source = await CreateSourceAsync("dry-run.png");
        var storage = new RecordingStorage();

        var report = await CreateMigrator(db, storage).RunAsync(dryRun: true, CancellationToken.None);

        Assert.Equal(1, report.Skipped);
        Assert.Equal(0, storage.SaveCount);
        Assert.True(File.Exists(source));
    }

    [Fact]
    public async Task Missing_source_is_reported_without_database_change()
    {
        await using var db = CreateDb();
        var receipt = Receipt("commerce/payment-receipts/legacy/missing.png");
        db.Add(receipt);
        await db.SaveChangesAsync();

        var report = await CreateMigrator(db, new RecordingStorage()).RunAsync(false, CancellationToken.None);

        Assert.Equal(1, report.MissingSource);
        Assert.Equal("commerce/payment-receipts/legacy/missing.png", receipt.ReceiptImageObjectKey);
    }

    [Fact]
    public async Task Failed_private_write_keeps_public_source_and_legacy_reference()
    {
        await using var db = CreateDb();
        var receipt = Receipt("commerce/payment-receipts/legacy/write-fails.png");
        db.Add(receipt);
        await db.SaveChangesAsync();
        var source = await CreateSourceAsync("write-fails.png");

        var report = await CreateMigrator(db, new RecordingStorage { FailSave = true }).RunAsync(false, CancellationToken.None);

        Assert.Equal(1, report.Failed);
        Assert.True(File.Exists(source));
        Assert.Equal("commerce/payment-receipts/legacy/write-fails.png", receipt.ReceiptImageObjectKey);
    }

    [Fact]
    public async Task Traversal_reference_is_rejected_before_file_access()
    {
        await using var db = CreateDb();
        db.Add(Receipt("/uploads/commerce/receipts/../../secret.png"));
        await db.SaveChangesAsync();

        var report = await CreateMigrator(db, new RecordingStorage()).RunAsync(false, CancellationToken.None);

        var item = Assert.Single(report.Items);
        Assert.Equal(LegacyReceiptMigrationStatus.Failed, item.Status);
        Assert.Contains("escapes", item.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Database_failure_after_copy_keeps_source_and_verified_destination_for_restart()
    {
        var interceptor = new FailModifiedReceiptSaveInterceptor();
        await using var db = CreateDb(interceptor);
        var receipt = Receipt("commerce/payment-receipts/legacy/db-fails.png");
        db.Add(receipt);
        await db.SaveChangesAsync();
        interceptor.Armed = true;
        var source = await CreateSourceAsync("db-fails.png");
        var storage = new RecordingStorage();

        var report = await CreateMigrator(db, storage).RunAsync(false, CancellationToken.None);

        Assert.Equal(1, report.Failed);
        Assert.True(File.Exists(source));
        Assert.Equal(1, storage.SaveCount);
        Assert.NotEmpty(storage.Files);
    }

    [Fact]
    public async Task Duplicate_references_share_verified_destination_and_delete_source_after_both_updates()
    {
        await using var db = CreateDb();
        db.AddRange(
            Receipt("commerce/payment-receipts/legacy/shared.png"),
            Receipt("commerce/payment-receipts/legacy/shared.png"));
        await db.SaveChangesAsync();
        var source = await CreateSourceAsync("shared.png");
        var storage = new RecordingStorage();

        var report = await CreateMigrator(db, storage).RunAsync(false, CancellationToken.None);

        Assert.Equal(2, report.Migrated);
        Assert.Equal(1, storage.SaveCount);
        Assert.False(File.Exists(source));
        Assert.Single(db.PaymentReceipts.Select(x => x.ReceiptImageObjectKey).Distinct());
    }

    [Fact]
    public async Task Corrupted_source_is_reported_and_remains_public_for_manual_resolution()
    {
        await using var db = CreateDb();
        var receipt = Receipt("commerce/payment-receipts/legacy/corrupt.png");
        db.Add(receipt);
        await db.SaveChangesAsync();
        var source = Path.Combine(_root, "wwwroot", "uploads", "commerce", "receipts", "corrupt.png");
        Directory.CreateDirectory(Path.GetDirectoryName(source)!);
        await File.WriteAllBytesAsync(source, "not-an-image"u8.ToArray());

        var report = await CreateMigrator(db, new RecordingStorage()).RunAsync(false, CancellationToken.None);

        Assert.Equal(1, report.Failed);
        Assert.True(File.Exists(source));
        Assert.Equal("commerce/payment-receipts/legacy/corrupt.png", receipt.ReceiptImageObjectKey);
    }

    [Fact]
    public async Task Legacy_public_route_is_blocked_before_static_file_serving()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var app = new ApplicationBuilder(provider);
        app.UseLegacyReceiptPublicAccessBlock();
        app.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Path = "/uploads/commerce/receipts/sensitive.png";

        await app.Build()(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private LegacyReceiptFileMigrator CreateMigrator(CommerceDbContext db, IPrivateFileStorage storage) =>
        new(db, storage, new ReceiptImageProcessor(), new FixedClock(), new FakeEnvironment(_root));

    private static CommerceDbContext CreateDb(SaveChangesInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString());
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new CommerceDbContext(builder.Options);
    }

    private static PaymentReceipt Receipt(string reference) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Premium", 25, "TJS", 30,
        reference, $"legacy-{Guid.NewGuid():N}", FixedClock.Now);

    private async Task<string> CreateSourceAsync(string name)
    {
        var path = Path.Combine(_root, "wwwroot", "uploads", "commerce", "receipts", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = new Image<Rgba32>(4, 4, Color.White);
        await image.SaveAsPngAsync(path);
        return path;
    }

    private sealed class RecordingStorage : IPrivateFileStorage
    {
        public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);
        public bool FailSave { get; init; }
        public int SaveCount { get; private set; }

        public async Task<StoredFile> SaveAsync(Stream stream, string contentType, string objectKey, CancellationToken cancellationToken)
        {
            SaveCount++;
            if (FailSave) throw new IOException("simulated write failure");
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            Files[objectKey] = memory.ToArray();
            return new StoredFile(objectKey, contentType, memory.Length, "test");
        }

        public Task<PrivateFileReadResult?> OpenReadAsync(string objectKey, CancellationToken cancellationToken) =>
            Task.FromResult(Files.TryGetValue(objectKey, out var content)
                ? new PrivateFileReadResult(new MemoryStream(content, writable: false), "image/webp", content.Length)
                : null);

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
        {
            Files.Remove(objectKey);
            return Task.CompletedTask;
        }

        public Task<string> CreateSignedReadUrlAsync(string objectKey, TimeSpan lifetime, CancellationToken cancellationToken) =>
            Task.FromResult(string.Empty);
    }

    private sealed class FailModifiedReceiptSaveInterceptor : SaveChangesInterceptor
    {
        public bool Armed { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            Armed
                ? ValueTask.FromException<InterceptionResult<int>>(new DbUpdateException("simulated database failure"))
                : ValueTask.FromResult(result);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-14T08:00:00Z");
        public DateTimeOffset UtcNow => Now;
        public DateTimeOffset DushanbeNow => Now.ToOffset(TimeSpan.FromHours(5));
        public DateTimeOffset ToDushanbeTime(DateTimeOffset utc) => utc.ToOffset(TimeSpan.FromHours(5));
    }

    private sealed class FakeEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Adeeb.Commerce.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
