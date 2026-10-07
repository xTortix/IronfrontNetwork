using Ironfront.GameData.Application;
using Ironfront.GameData.Domain;
using Ironfront.Matchmaking.Application;
using Ironfront.Matchmaking.Application.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ironfront.MatchmakingService.Hosting;

public sealed class BattleMatchmakerWorker
    : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;

    private readonly IBattleModeCatalog modeCatalog;

    private readonly TimeProvider timeProvider;

    private readonly ILogger<BattleMatchmakerWorker> logger;

    private readonly BattleMatchmakingOptions options;

    public BattleMatchmakerWorker(
        IServiceScopeFactory scopeFactory,
        IBattleModeCatalog modeCatalog,
        TimeProvider timeProvider,
        ILogger<BattleMatchmakerWorker> logger,
        BattleMatchmakingOptions options)
    {
        this.scopeFactory = scopeFactory
            ?? throw new ArgumentNullException(
                nameof(scopeFactory));

        this.modeCatalog = modeCatalog
            ?? throw new ArgumentNullException(
                nameof(modeCatalog));

        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(
                nameof(timeProvider));

        this.logger = logger
            ?? throw new ArgumentNullException(
                nameof(logger));

        this.options = options
            ?? throw new ArgumentNullException(
                nameof(options));
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Battle matchmaking worker started. " +
            "Interval: {IntervalMilliseconds} ms. " +
            "Maximum matches per mode per cycle: {MaximumMatchesPerModePerCycle}.",
            options.Interval.TotalMilliseconds,
            options.MaximumMatchesPerModePerCycle);

        try
        {
            await RunCycleAsync(stoppingToken);

            using var timer =
                new PeriodicTimer(options.Interval);

            while (await timer.WaitForNextTickAsync(
                       stoppingToken))
            {
                await RunCycleAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal service shutdown.
        }
    }

    private async Task RunCycleAsync(
        CancellationToken stoppingToken)
    {
        DateTime utcNow =
            timeProvider.GetUtcNow().UtcDateTime;

        IReadOnlyList<BattleModeDefinition> availableModes =
            modeCatalog.GetAvailableModes(utcNow)
                .OrderBy(mode => mode.ModeId, StringComparer.Ordinal)
                .ToArray();

        foreach (BattleModeDefinition mode in availableModes)
        {
            int formedMatchCount = 0;

            for (int index = 0;
                 index < options.MaximumMatchesPerModePerCycle;
                 index++)
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    await using AsyncServiceScope scope =
                        scopeFactory.CreateAsyncScope();

                    IBattleMatchmaker matchmaker =
                        scope.ServiceProvider.GetRequiredService<
                            IBattleMatchmaker>();

                    BattleMatchSnapshot? battleMatch =
                        await matchmaker.TryFormNextMatchAsync(
                            mode.ModeId,
                            stoppingToken);

                    if (battleMatch is null)
                    {
                        break;
                    }

                    formedMatchCount++;

                    logger.LogInformation(
                        "Formed battle match {MatchId}. " +
                        "Mode: {ModeId} revision {ModeRevision}. " +
                        "Map: {MapId}. " +
                        "Players: {ExpectedPlayerCount}. " +
                        "Battle server slot: {BattleServerInstanceId}.",
                        battleMatch.MatchId,
                        battleMatch.ModeId,
                        battleMatch.ModeRevision,
                        battleMatch.MapId,
                        battleMatch.ExpectedPlayerCount,
                        battleMatch.BattleServerInstanceId);
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
                        "Could not form a match for mode {ModeId}.",
                        mode.ModeId);

                    /*
                     * Nicht endlos denselben fehlerhaften Modus in
                     * einem einzelnen Durchlauf erneut versuchen.
                     * Andere verfügbare Modi können trotzdem weiterlaufen.
                     */
                    break;
                }
            }

            if (formedMatchCount > 0)
            {
                logger.LogInformation(
                    "Formed {FormedMatchCount} battle match(es) " +
                    "for mode {ModeId} in this cycle.",
                    formedMatchCount,
                    mode.ModeId);
            }
        }
    }
}