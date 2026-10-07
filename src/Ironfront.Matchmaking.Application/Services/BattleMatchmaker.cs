using System.Text.Json;
using Ironfront.GameData.Application;
using Ironfront.GameData.Domain;
using Ironfront.Matchmaking.Application.Configuration;
using Ironfront.Matchmaking.Application.Matchmaking;
using Ironfront.Matchmaking.Application.Models;
using Ironfront.Matchmaking.Domain.Entities;
using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Application.Services;

public sealed class BattleMatchmaker
    : IBattleMatchmaker
{
    private const string CreateMatchCommandType =
        "match.create.v1";

    private const int MaximumCandidateCount = 16;

    private static readonly JsonSerializerOptions
        PayloadJsonOptions =
            new(JsonSerializerDefaults.Web);

    private readonly IBattleModeCatalog modeCatalog;

    private readonly IBattleQueueEntryRepository
        queueEntryRepository;

    private readonly IBattleServerSlotRepository
        slotRepository;

    private readonly IBattleMatchRepository
        matchRepository;

    private readonly IBattleMatchPlayerRepository
        matchPlayerRepository;

    private readonly IBattleServerCommandRepository
        commandRepository;

    private readonly IMatchmakingUnitOfWork
        unitOfWork;

    private readonly BattleServerSelectionPolicy
        selectionPolicy;

    private readonly TimeProvider timeProvider;

    public BattleMatchmaker(
        IBattleModeCatalog modeCatalog,
        IBattleQueueEntryRepository queueEntryRepository,
        IBattleServerSlotRepository slotRepository,
        IBattleMatchRepository matchRepository,
        IBattleMatchPlayerRepository matchPlayerRepository,
        IBattleServerCommandRepository commandRepository,
        IMatchmakingUnitOfWork unitOfWork,
        BattleServerSelectionPolicy selectionPolicy,
        TimeProvider timeProvider)
    {
        this.modeCatalog = modeCatalog
            ?? throw new ArgumentNullException(
                nameof(modeCatalog));

        this.queueEntryRepository = queueEntryRepository
            ?? throw new ArgumentNullException(
                nameof(queueEntryRepository));

        this.slotRepository = slotRepository
            ?? throw new ArgumentNullException(
                nameof(slotRepository));

        this.matchRepository = matchRepository
            ?? throw new ArgumentNullException(
                nameof(matchRepository));

        this.matchPlayerRepository =
            matchPlayerRepository
            ?? throw new ArgumentNullException(
                nameof(matchPlayerRepository));

        this.commandRepository = commandRepository
            ?? throw new ArgumentNullException(
                nameof(commandRepository));

        this.unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));

        this.selectionPolicy = selectionPolicy
            ?? throw new ArgumentNullException(
                nameof(selectionPolicy));

        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(
                nameof(timeProvider));
    }

    public Task<BattleMatchSnapshot?> TryFormNextMatchAsync(
        string modeId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            modeId);

        if (!modeCatalog.TryGetMode(
                modeId,
                out BattleModeDefinition? mode))
        {
            return Task.FromResult<BattleMatchSnapshot?>(
                null);
        }

        if (!IsSupportedByCurrentFormationStrategy(mode))
        {
            return Task.FromResult<BattleMatchSnapshot?>(
                null);
        }

        return unitOfWork.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                DateTime utcNow =
                    timeProvider.GetUtcNow().UtcDateTime;

                if (!mode.IsAvailableAt(utcNow))
                {
                    return null;
                }

                BattleQueueEntry? firstEntry =
                    await queueEntryRepository
                        .TryAcquireOldestQueuedForUpdateAsync(
                            mode.ModeId,
                            mode.Revision,
                            transactionCancellationToken);

                if (firstEntry is null)
                {
                    return null;
                }

                if (!BattleRatingMatchmakingWindow
                    .IsEligibleForMode(
                        firstEntry,
                        mode))
                {
                    firstEntry.MarkFailed(
                        "Queued vehicle no longer satisfies this battle mode's rules.",
                        utcNow);

                    return null;
                }

                byte broadSearchRange =
                    BattleRatingMatchmakingWindow
                        .GetBroadCandidateSearchRangeTenths(
                            mode,
                            firstEntry,
                            utcNow);

                int minimumRating =
                    Math.Max(
                        mode.VehicleRules
                            .MinimumBattleRatingTenths,
                        firstEntry
                            .VehicleBattleRatingTenths -
                        broadSearchRange);

                int maximumRating =
                    Math.Min(
                        mode.VehicleRules
                            .MaximumBattleRatingTenths,
                        firstEntry
                            .VehicleBattleRatingTenths +
                        broadSearchRange);

                IReadOnlyList<BattleQueueEntry> candidates =
                    await queueEntryRepository
                        .TryAcquireQueuedCandidatesForUpdateAsync(
                            mode.ModeId,
                            mode.Revision,
                            firstEntry.QueueEntryId,
                            firstEntry.VehicleBattleRatingTenths,
                            (byte)minimumRating,
                            (byte)maximumRating,
                            MaximumCandidateCount,
                            transactionCancellationToken);

                BattleQueueEntry? secondEntry =
                    null;

                foreach (BattleQueueEntry candidate in candidates)
                {
                    if (!BattleRatingMatchmakingWindow
                        .IsEligibleForMode(
                            candidate,
                            mode))
                    {
                        candidate.MarkFailed(
                            "Queued vehicle no longer satisfies this battle mode's rules.",
                            utcNow);

                        continue;
                    }

                    if (BattleRatingMatchmakingWindow
                        .AreCompatible(
                            firstEntry,
                            candidate,
                            mode,
                            utcNow))
                    {
                        secondEntry = candidate;
                        break;
                    }
                }

                if (secondEntry is null)
                {
                    return null;
                }

                DateTime minimumLastHeartbeatUtc =
                    utcNow -
                    selectionPolicy.MaximumHeartbeatAge;

                BattleServerSlot? slot =
                    await slotRepository
                        .TryAcquireReadyForReservationAsync(
                            minimumLastHeartbeatUtc,
                            mode.ExpectedPlayerCount,
                            transactionCancellationToken);

                if (slot is null)
                {
                    /*
                     * Die Queue-Einträge bleiben Queued.
                     * Sobald ein BattleServer wieder Ready ist,
                     * kann der nächste Worker-Durchlauf sie erneut
                     * matchen.
                     */
                    return null;
                }

                string mapId =
                    BattleModeMapSelector.SelectMapId(
                        mode);

                string rulesSnapshotJson =
                    BattleModeRulesSnapshotSerializer
                        .Serialize(mode);

                BattleMatch battleMatch =
                    BattleMatch.Create(
                        battleServerInstanceId:
                            slot.ServerInstanceId,

                        modeId:
                            mode.ModeId,

                        modeRevision:
                            mode.Revision,

                        rulesSnapshotJson:
                            rulesSnapshotJson,

                        mapId:
                            mapId,

                        expectedPlayerCount:
                            mode.ExpectedPlayerCount,

                        utcNow:
                            utcNow);

                slot.ReserveForMatch(
                    battleMatch.MatchId,
                    battleMatch.ExpectedPlayerCount,
                    utcNow);

                BattleMatchPlayer alphaPlayer =
                    BattleMatchPlayer.Create(
                        matchId: battleMatch.MatchId,
                        userId: firstEntry.UserId,
                        initialVehicleId:
                            firstEntry.InitialVehicleId,
                        teamId:
                            mode.Teams[0].TeamId,
                        teamSlotIndex: 0,
                        playerSlotIndex: 0,
                        utcNow: utcNow);

                BattleMatchPlayer bravoPlayer =
                    BattleMatchPlayer.Create(
                        matchId: battleMatch.MatchId,
                        userId: secondEntry.UserId,
                        initialVehicleId:
                            secondEntry.InitialVehicleId,
                        teamId:
                            mode.Teams[1].TeamId,
                        teamSlotIndex: 0,
                        playerSlotIndex: 1,
                        utcNow: utcNow);

                firstEntry.MarkMatched(
                    battleMatch.MatchId,
                    utcNow);

                secondEntry.MarkMatched(
                    battleMatch.MatchId,
                    utcNow);
                
                CreateBattleServerMatchPayload commandPayload =
                    new(
                        battleMatch.MatchId,
                        battleMatch.ModeId,
                        battleMatch.ModeRevision,
                        battleMatch.MapId,
                        battleMatch.ExpectedPlayerCount,
                        rulesSnapshotJson,
                        new[]
                        {
                            new CreateBattleServerMatchPlayerPayload(
                                alphaPlayer.MatchPlayerId,
                                alphaPlayer.UserId,
                                alphaPlayer.TeamId,
                                alphaPlayer.TeamSlotIndex,
                                alphaPlayer.InitialVehicleId,
                                alphaPlayer.PlayerSlotIndex),

                            new CreateBattleServerMatchPlayerPayload(
                                bravoPlayer.MatchPlayerId,
                                bravoPlayer.UserId,
                                bravoPlayer.TeamId,
                                bravoPlayer.TeamSlotIndex,
                                bravoPlayer.InitialVehicleId,
                                bravoPlayer.PlayerSlotIndex)
                        });

                string payloadJson =
                    JsonSerializer.Serialize(
                        commandPayload,
                        PayloadJsonOptions);

                BattleServerCommand command =
                    BattleServerCommand.Create(
                        battleMatch.BattleServerInstanceId,
                        CreateMatchCommandType,
                        payloadJson,
                        utcNow,
                        battleMatch.MatchId);

                matchRepository.Add(battleMatch);

                matchPlayerRepository.Add(alphaPlayer);
                matchPlayerRepository.Add(bravoPlayer);

                commandRepository.Add(command);

                return ToSnapshot(battleMatch);
            },
            cancellationToken);
    }

    private static bool IsSupportedByCurrentFormationStrategy(
        BattleModeDefinition mode)
    {
        return mode.Teams.Count == 2 &&
               mode.Teams[0].RequiredPlayerCount == 1 &&
               mode.Teams[1].RequiredPlayerCount == 1 &&
               mode.ExpectedPlayerCount == 2;
    }

    private static BattleMatchSnapshot ToSnapshot(
        BattleMatch battleMatch)
    {
        return new BattleMatchSnapshot(
            battleMatch.MatchId,
            battleMatch.BattleServerInstanceId,
            battleMatch.ModeId,
            battleMatch.ModeRevision,
            battleMatch.MapId,
            battleMatch.ExpectedPlayerCount,
            ToApiStatus(battleMatch.Status),
            battleMatch.CreatedAtUtc,
            battleMatch.UpdatedAtUtc,
            battleMatch.WaitingForPlayersAtUtc,
            battleMatch.StartedAtUtc,
            battleMatch.FinishedAtUtc,
            battleMatch.FailureReason);
    }

    private static string ToApiStatus(
        BattleMatchStatus status)
    {
        return status switch
        {
            BattleMatchStatus.Provisioning =>
                "provisioning",

            BattleMatchStatus.WaitingForPlayers =>
                "waiting_for_players",

            BattleMatchStatus.Running =>
                "running",

            BattleMatchStatus.Finishing =>
                "finishing",

            BattleMatchStatus.Completed =>
                "completed",

            BattleMatchStatus.Failed =>
                "failed",

            BattleMatchStatus.Cancelled =>
                "cancelled",

            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown battle match status.")
        };
    }
}