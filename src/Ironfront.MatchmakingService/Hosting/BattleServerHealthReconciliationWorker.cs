using Ironfront.Matchmaking.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ironfront.MatchmakingService.Hosting;

public sealed class BattleServerHealthReconciliationWorker
    : BackgroundService
{
    private const int MaximumSlotsPerCycle = 32;

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<
        BattleServerHealthReconciliationWorker> logger;

    private readonly TimeSpan interval;

    public BattleServerHealthReconciliationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<BattleServerHealthReconciliationWorker> logger,
        BattleServerHealthReconciliationOptions options)
    {
        this.scopeFactory = scopeFactory
            ?? throw new ArgumentNullException(
                nameof(scopeFactory));

        this.logger = logger
            ?? throw new ArgumentNullException(
                nameof(logger));

        ArgumentNullException.ThrowIfNull(options);

        interval = options.Interval;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Battle server health reconciliation worker started. " +
            "Interval: {IntervalSeconds} seconds.",
            interval.TotalSeconds);

        /*
         * Ein Durchlauf direkt beim Service-Start räumt Slots auf,
         * die während eines Service-Neustarts stale geworden sind.
         */
        await ReconcileCycleAsync(stoppingToken);

        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ReconcileCycleAsync(stoppingToken);
        }
    }

    private async Task ReconcileCycleAsync(
        CancellationToken stoppingToken)
    {
        int reconciledSlotCount = 0;

        for (int index = 0;
             index < MaximumSlotsPerCycle;
             index++)
        {
            try
            {
                await using AsyncServiceScope scope =
                    scopeFactory.CreateAsyncScope();

                var reconciler =
                    scope.ServiceProvider.GetRequiredService<
                        IBattleServerHealthReconciler>();

                bool reconciled =
                    await reconciler
                        .ReconcileOneStaleSlotAsync(
                            stoppingToken);

                if (!reconciled)
                    break;

                reconciledSlotCount++;
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Battle server health reconciliation failed.");

                break;
            }
        }

        if (reconciledSlotCount > 0)
        {
            logger.LogWarning(
                "Reconciled {ReconciledSlotCount} stale " +
                "battle server slot(s).",
                reconciledSlotCount);
        }
    }
}