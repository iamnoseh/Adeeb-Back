using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.QuestionBank.Application;
using Adeeb.Modules.QuestionBank.Contracts;
using Adeeb.Modules.QuestionBank.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence;

internal sealed class ExpiredTestAttemptFinalizer(
    IServiceScopeFactory scopeFactory,
    IDateTimeProvider clock,
    IOptions<StudentTestingOptions> options,
    ILogger<ExpiredTestAttemptFinalizer> logger) : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await FinalizeBatchAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(options.Value.ExpiredAttemptSweepIntervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
    }

    internal async Task<int> FinalizeBatchAsync(CancellationToken ct)
    {
        List<(Guid AttemptId, Guid UserId)> expired;
        using (var queryScope = scopeFactory.CreateScope())
        {
            var db = queryScope.ServiceProvider.GetRequiredService<QuestionBankDbContext>();
            var rows = await db.TestAttempts.AsNoTracking()
                .Where(x => x.Status == TestAttemptStatus.InProgress && x.ExpiresAtUtc <= clock.UtcNow)
                .OrderBy(x => x.ExpiresAtUtc)
                .Take(options.Value.ExpiredAttemptSweepBatchSize)
                .Select(x => new { AttemptId = x.Id, x.UserId })
                .ToListAsync(ct);
            expired = rows.Select(x => (x.AttemptId, x.UserId)).ToList();
        }

        var finalized = 0;
        foreach (var candidate in expired)
        {
            try
            {
                using var attemptScope = scopeFactory.CreateScope();
                var service = attemptScope.ServiceProvider.GetRequiredService<StudentTestingService>();
                var result = await service.SubmitAsync(candidate.UserId, candidate.AttemptId, new SubmitAttemptRequest([]), ct);
                if (result.IsSuccess)
                {
                    finalized++;
                }
                else if (result.Error?.Code != StudentTestingErrors.AttemptAlreadySubmitted.Code)
                {
                    logger.LogWarning("Expired test attempt {AttemptId} was not finalized: {ErrorCode}",
                        candidate.AttemptId, result.Error?.Code);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to auto-submit expired test attempt {AttemptId}", candidate.AttemptId);
            }
        }

        return finalized;
    }
}
