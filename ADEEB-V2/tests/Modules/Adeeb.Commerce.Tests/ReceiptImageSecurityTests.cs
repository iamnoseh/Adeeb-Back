using System.Security.Claims;
using Adeeb.Application.Abstractions.Storage;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Commerce.Application;
using Adeeb.Modules.Commerce.Application.Storage;
using Adeeb.Modules.Commerce.Application.PaymentReceipts;
using Adeeb.Modules.Commerce.Application.Auditing;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.Modules.Commerce.Domain.Tariffs;
using Adeeb.Modules.Commerce.Infrastructure.Files;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Adeeb.Modules.Students.Contracts;
using Adeeb.SharedKernel.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace Adeeb.Commerce.Tests;

public sealed class ReceiptImageSecurityTests
{
    [Fact]
    public async Task FakeAndCorruptedImages_AreRejected()
    {
        var processor = new ReceiptImageProcessor();

        var fake = await processor.ProcessAsync(new MemoryStream("not-an-image"u8.ToArray()), 12, CancellationToken.None);
        var corruptedJpeg = await processor.ProcessAsync(
            new MemoryStream([0xff, 0xd8, 0xff, 0xe0, 0x00, 0x10, 0x4a, 0x46, 0x49, 0x46]),
            10,
            CancellationToken.None);

        Assert.True(fake.IsFailure);
        Assert.True(corruptedJpeg.IsFailure);
    }

    [Fact]
    public async Task OversizedAndExcessiveDimensions_AreRejected()
    {
        var processor = new ReceiptImageProcessor();
        var oversized = await processor.ProcessAsync(new MemoryStream([1]), ReceiptImageProcessor.MaxFileSize + 1, CancellationToken.None);
        await using var source = new MemoryStream();
        using (var image = new Image<Rgba32>(ReceiptImageProcessor.MaxWidth + 1, 1))
        {
            await image.SaveAsync(source, new PngEncoder());
        }

        source.Position = 0;
        var dimensions = await processor.ProcessAsync(source, source.Length, CancellationToken.None);

        Assert.Equal(CommerceErrors.ImageTooLarge.Code, oversized.Error!.Code);
        Assert.Equal(CommerceErrors.ImageDimensionsInvalid.Code, dimensions.Error!.Code);
    }

    [Fact]
    public async Task Actual_stream_size_cannot_bypass_limit_with_false_declared_length()
    {
        var content = new byte[(int)ReceiptImageProcessor.MaxFileSize + 1];

        var result = await new ReceiptImageProcessor().ProcessAsync(
            new MemoryStream(content),
            declaredLength: 1,
            CancellationToken.None);

        Assert.Equal(CommerceErrors.ImageTooLarge.Code, result.Error!.Code);
    }

    [Fact]
    public async Task ValidImage_IsReencodedAsWebpAndMetadataIsRemoved()
    {
        await using var source = new MemoryStream();
        using (var image = new Image<Rgba32>(4, 4))
        {
            image.Metadata.ExifProfile = new ExifProfile();
            await image.SaveAsync(source, new PngEncoder());
        }

        source.Position = 0;
        var result = await new ReceiptImageProcessor().ProcessAsync(source, source.Length, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("image/webp", result.Value!.ContentType);
        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(result.Value.Content, 0, 4));
        using var decoded = Image.Load(result.Value.Content);
        Assert.Null(decoded.Metadata.ExifProfile);
        Assert.Null(decoded.Metadata.XmpProfile);
    }

    [Fact]
    public async Task Magic_bytes_are_used_instead_of_a_claimed_extension()
    {
        await using var pngWithJpegName = new MemoryStream();
        using (var image = new Image<Rgba32>(2, 2, Color.White))
        {
            await image.SaveAsPngAsync(pngWithJpegName);
        }

        pngWithJpegName.Position = 0;
        var accepted = await new ReceiptImageProcessor().ProcessAsync(
            pngWithJpegName,
            pngWithJpegName.Length,
            CancellationToken.None);

        Assert.True(accepted.IsSuccess);
        Assert.Equal("image/webp", accepted.Value!.ContentType);
    }

    [Fact]
    public async Task Valid_maximum_width_boundary_is_accepted()
    {
        await using var source = new MemoryStream();
        using (var image = new Image<Rgba32>(ReceiptImageProcessor.MaxWidth, 1, Color.White))
        {
            await image.SaveAsPngAsync(source);
        }

        source.Position = 0;
        var result = await new ReceiptImageProcessor().ProcessAsync(source, source.Length, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReceiptImageProcessor.MaxWidth, result.Value!.Width);
    }

    [Fact]
    public async Task LocalStorage_RejectsPathTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "adeeb-private-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalPrivateFileStorage(
            new TestEnvironment(root),
            Options.Create(new PrivateFileStorageOptions { LocalRoot = "private" }));

        await Assert.ThrowsAsync<ArgumentException>(() => storage.SaveAsync(
            new MemoryStream([1, 2, 3]),
            "image/webp",
            "../escape.webp",
            CancellationToken.None));
    }

    [Fact]
    public async Task DuplicateRetry_DoesNotUploadSecondObject()
    {
        var studentId = Guid.NewGuid();
        var identityId = Guid.NewGuid();
        await using var db = CreateDb();
        var tariff = new CommerceTariff(Guid.NewGuid(), "Premium", 25, "TJS", 30, "qr.webp", FixedClock.Now);
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();
        var storage = new RecordingStorage();
        var useCases = new PaymentReceiptUseCases(
            db,
            new FixedStudentLookup(new StudentReference(studentId, identityId, "Active")),
            new FixedClock(),
            new ValidProcessor(),
            storage,
            new RecordingAudit());
        var principal = Principal(identityId);
        var request = new SubmitPaymentReceiptFormRequest { IdempotencyKey = "same-key" };

        var first = await useCases.SubmitAsync(principal, tariff.Id, request, new MemoryStream([1]), 1, CancellationToken.None);
        var second = await useCases.SubmitAsync(principal, tariff.Id, request, new MemoryStream([1]), 1, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.ReceiptId, second.Value!.ReceiptId);
        Assert.Equal(1, storage.SaveCount);
        Assert.StartsWith($"commerce/payment-receipts/{studentId:N}/", storage.LastObjectKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DatabaseFailure_DeletesUploadedObject()
    {
        var databaseName = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid();
        var identityId = Guid.NewGuid();
        var tariff = new CommerceTariff(Guid.NewGuid(), "Premium", 25, "TJS", 30, "qr.webp", FixedClock.Now);
        await using (var seed = CreateDb(databaseName))
        {
            seed.Tariffs.Add(tariff);
            await seed.SaveChangesAsync();
        }

        var options = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(new PaymentSaveFailureInterceptor())
            .Options;
        await using var db = new CommerceDbContext(options);
        var storage = new RecordingStorage();
        var useCases = new PaymentReceiptUseCases(
            db,
            new FixedStudentLookup(new StudentReference(studentId, identityId, "Active")),
            new FixedClock(),
            new ValidProcessor(),
            storage,
            new RecordingAudit());

        await Assert.ThrowsAsync<DbUpdateException>(() => useCases.SubmitAsync(
            Principal(identityId),
            tariff.Id,
            new SubmitPaymentReceiptFormRequest { IdempotencyKey = "db-failure" },
            new MemoryStream([1]),
            1,
            CancellationToken.None));
        Assert.Equal(1, storage.DeleteCount);
    }

    private static CommerceDbContext CreateDb(string? name = null) => new(
        new DbContextOptionsBuilder<CommerceDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options);

    private static ClaimsPrincipal Principal(Guid identityId) =>
        new(new ClaimsIdentity([new Claim("sub", identityId.ToString())], "Test"));

    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FixedStudentLookup(StudentReference student) : IStudentLookup
    {
        public Task<StudentReference?> FindByIdentityUserIdAsync(Guid identityUserId, CancellationToken cancellationToken) =>
            Task.FromResult<StudentReference?>(student.IdentityUserId == identityUserId ? student : null);
        public Task<StudentReference?> FindByStudentIdAsync(Guid studentId, CancellationToken cancellationToken) =>
            Task.FromResult<StudentReference?>(student.StudentId == studentId ? student : null);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-13T08:00:00Z");
        public DateTimeOffset UtcNow => Now;
        public DateTimeOffset DushanbeNow => ToDushanbeTime(Now);
        public DateTimeOffset ToDushanbeTime(DateTimeOffset utc) => utc.ToOffset(TimeSpan.FromHours(5));
    }

    private sealed class ValidProcessor : IReceiptImageProcessor
    {
        public Task<Result<ValidatedReceiptImage>> ProcessAsync(Stream input, long declaredLength, CancellationToken cancellationToken) =>
            Task.FromResult(Result<ValidatedReceiptImage>.Success(new([1, 2, 3], "image/webp", "hash", 1, 1)));
    }

    private sealed class RecordingStorage : IPrivateFileStorage
    {
        public int SaveCount { get; private set; }
        public int DeleteCount { get; private set; }
        public string LastObjectKey { get; private set; } = string.Empty;

        public Task<StoredFile> SaveAsync(Stream stream, string contentType, string objectKey, CancellationToken cancellationToken)
        {
            SaveCount++;
            LastObjectKey = objectKey;
            return Task.FromResult(new StoredFile(objectKey, contentType, 3, "hash"));
        }

        public Task<PrivateFileReadResult?> OpenReadAsync(string objectKey, CancellationToken cancellationToken) =>
            Task.FromResult<PrivateFileReadResult?>(null);
        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
        {
            DeleteCount++;
            return Task.CompletedTask;
        }
        public Task<string> CreateSignedReadUrlAsync(string objectKey, TimeSpan lifetime, CancellationToken cancellationToken) =>
            Task.FromResult("https://example.invalid/signed");
    }

    private sealed class PaymentSaveFailureInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException<InterceptionResult<int>>(new DbUpdateException("simulated database failure"));
    }

    private sealed class RecordingAudit : ICommerceAuditWriter
    {
        public void Write(
            string action,
            string resourceType,
            Guid resourceId,
            Guid? studentId = null,
            IReadOnlyDictionary<string, object?>? oldValues = null,
            IReadOnlyDictionary<string, object?>? newValues = null)
        {
        }
    }
}
