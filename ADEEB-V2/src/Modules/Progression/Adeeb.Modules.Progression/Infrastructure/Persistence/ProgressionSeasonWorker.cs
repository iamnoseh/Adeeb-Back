using Adeeb.Modules.Progression.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Adeeb.Modules.Progression.Infrastructure.Persistence;

internal sealed class ProgressionSeasonWorker(IServiceScopeFactory scopes, ILogger<ProgressionSeasonWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<ProgressionService>().ProcessExpiredSeasonAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Progression season processing failed."); }
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
