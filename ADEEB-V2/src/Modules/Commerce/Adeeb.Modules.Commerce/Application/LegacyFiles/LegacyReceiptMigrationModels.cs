namespace Adeeb.Modules.Commerce.Application.LegacyFiles;

public enum LegacyReceiptMigrationStatus
{
    Migrated,
    AlreadyMigrated,
    MissingSource,
    Failed,
    Skipped
}

public sealed record LegacyReceiptMigrationItem(
    Guid ReceiptId,
    LegacyReceiptMigrationStatus Status,
    string? Source,
    string? Destination,
    string? Reason);

public sealed record LegacyReceiptMigrationReport(IReadOnlyList<LegacyReceiptMigrationItem> Items)
{
    public int Migrated => Items.Count(x => x.Status == LegacyReceiptMigrationStatus.Migrated);
    public int AlreadyMigrated => Items.Count(x => x.Status == LegacyReceiptMigrationStatus.AlreadyMigrated);
    public int MissingSource => Items.Count(x => x.Status == LegacyReceiptMigrationStatus.MissingSource);
    public int Failed => Items.Count(x => x.Status == LegacyReceiptMigrationStatus.Failed);
    public int Skipped => Items.Count(x => x.Status == LegacyReceiptMigrationStatus.Skipped);
}
