using Adeeb.Modules.Commerce.Application.LegacyFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.Commerce.Infrastructure.Files;

public sealed class LegacyReceiptMigrationOptions
{
    public const string SectionName = "LegacyReceiptMigration";
    public bool Enabled { get; init; }
    public bool DryRun { get; init; } = true;
}

internal sealed class LegacyReceiptMigrationHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<LegacyReceiptMigrationOptions> options,
    ILogger<LegacyReceiptMigrationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var migrator = scope.ServiceProvider.GetRequiredService<LegacyReceiptFileMigrator>();
        var report = await migrator.RunAsync(options.Value.DryRun, stoppingToken);
        logger.LogInformation(
            "Legacy receipt migration completed. DryRun={DryRun} Migrated={Migrated} AlreadyMigrated={AlreadyMigrated} MissingSource={MissingSource} Failed={Failed} Skipped={Skipped}",
            options.Value.DryRun,
            report.Migrated,
            report.AlreadyMigrated,
            report.MissingSource,
            report.Failed,
            report.Skipped);
        foreach (var item in report.Items.Where(x => x.Status is LegacyReceiptMigrationStatus.Failed or LegacyReceiptMigrationStatus.MissingSource))
        {
            logger.LogWarning(
                "Legacy receipt migration item. ReceiptId={ReceiptId} Status={Status} Source={Source} Destination={Destination} Reason={Reason}",
                item.ReceiptId,
                item.Status,
                item.Source,
                item.Destination,
                item.Reason);
        }
    }
}
