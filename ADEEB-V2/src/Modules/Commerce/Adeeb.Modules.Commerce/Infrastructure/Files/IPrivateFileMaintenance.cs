namespace Adeeb.Modules.Commerce.Infrastructure.Files;

public interface IPrivateFileMaintenance
{
    Task<IReadOnlyList<string>> ListOlderThanAsync(
        string prefix,
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken);
}
