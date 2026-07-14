using Adeeb.Application.Abstractions.Storage;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Adeeb.Modules.Commerce.Infrastructure.Files;

public sealed class OrphanReceiptFileCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<OrphanReceiptFileCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromHours(1);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelay, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Commerce orphan receipt file cleanup failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    internal async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        var maintenance = scope.ServiceProvider.GetRequiredService<IPrivateFileMaintenance>();
        var storage = scope.ServiceProvider.GetRequiredService<IPrivateFileStorage>();
        var attached = await db.PaymentReceipts.AsNoTracking()
            .Select(x => x.ReceiptImageObjectKey)
            .ToHashSetAsync(cancellationToken);
        var candidates = await maintenance.ListOlderThanAsync(
            "commerce/payment-receipts/",
            DateTimeOffset.UtcNow.AddHours(-24),
            cancellationToken);
        foreach (var objectKey in candidates.Where(x => !attached.Contains(x)))
        {
            await storage.DeleteAsync(objectKey, cancellationToken);
            logger.LogInformation("Deleted orphan Commerce receipt object {ObjectKey}", objectKey);
        }
    }
}
