using Adeeb.Application.Abstractions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Adeeb.Modules.Commerce.Infrastructure.Files;

internal sealed class PrivateFileStorageHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var objectKey = $"health/{Guid.NewGuid():N}.probe";
        IPrivateFileStorage? storage = null;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            storage = scope.ServiceProvider.GetRequiredService<IPrivateFileStorage>();
            await using var content = new MemoryStream([0x41], writable: false);
            await storage.SaveAsync(content, "application/octet-stream", objectKey, cancellationToken);
            await storage.DeleteAsync(objectKey, cancellationToken);
            return HealthCheckResult.Healthy("Private file storage is writable.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (storage is not null)
            {
                try
                {
                    await storage.DeleteAsync(objectKey, CancellationToken.None);
                }
                catch
                {
                    // The unhealthy result retains the original probe failure.
                }
            }

            return HealthCheckResult.Unhealthy("Private file storage probe failed.", exception);
        }
    }
}
