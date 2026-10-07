using Ironfront.GameData.Application;
using Ironfront.GameData.Domain;
using Ironfront.Matchmaking.Application.Models;
using Ironfront.Matchmaking.Domain.Entities;
using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Application.Services;

public sealed class BattleQueueLifecycle
    : IBattleQueueLifecycle
{
    private readonly IBattleModeCatalog modeCatalog;

    private readonly IBattleQueueEntryRepository
        queueEntryRepository;

    private readonly IBattleMatchRepository
        matchRepository;

    private readonly IBattleMatchPlayerRepository
        matchPlayerRepository;

    private readonly IBattleJoinTicketIssuer
        battleJoinTicketIssuer;

    private readonly IMatchmakingUnitOfWork
        unitOfWork;

    private readonly TimeProvider timeProvider;

    public BattleQueueLifecycle(
        IBattleModeCatalog modeCatalog,
        IBattleQueueEntryRepository queueEntryRepository,
        IBattleMatchRepository matchRepository,
        IBattleMatchPlayerRepository matchPlayerRepository,
        IBattleJoinTicketIssuer battleJoinTicketIssuer,
        IMatchmakingUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        this.modeCatalog = modeCatalog
            ?? throw new ArgumentNullException(
                nameof(modeCatalog));

        this.queueEntryRepository =
            queueEntryRepository
            ?? throw new ArgumentNullException(
                nameof(queueEntryRepository));

        this.matchRepository = matchRepository
            ?? throw new ArgumentNullException(
                nameof(matchRepository));

        this.matchPlayerRepository =
            matchPlayerRepository
            ?? throw new ArgumentNullException(
                nameof(matchPlayerRepository));

        this.battleJoinTicketIssuer =
            battleJoinTicketIssuer
            ?? throw new ArgumentNullException(
                nameof(battleJoinTicketIssuer));

        this.unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));

        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(
                nameof(timeProvider));
    }

    public Task<BattleQueueStatusSnapshot> JoinAsync(
        JoinBattleQueueCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.UserId == Guid.Empty)
        {
            throw new ArgumentException(
                "User id must not be empty.",
                nameof(command));
        }

        if (command.DeckId == Guid.Empty)
        {
            throw new ArgumentException(
                "Deck id must not be empty.",
                nameof(command));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            command.ModeId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            command.InitialVehicleId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            command.VehicleClass);

        if (command.VehicleBattleRatingTenths == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                "Vehicle battle rating must be greater than zero.");
        }

        if (!modeCatalog.TryGetMode(
                command.ModeId,
                out BattleModeDefinition? mode))
        {
            throw new ArgumentException(
                $"Battle mode '{command.ModeId}' does not exist.",
                nameof(command));
        }

        DateTime utcNow =
            timeProvider.GetUtcNow().UtcDateTime;

        if (!mode.IsAvailableAt(utcNow))
        {
            throw new InvalidOperationException(
                $"Battle mode '{mode.ModeId}' is currently unavailable.");
        }

        if (!IsVehicleAllowedByMode(
                mode,
                command.VehicleBattleRatingTenths,
                command.VehicleClass))
        {
            throw new InvalidOperationException(
                $"Vehicle '{command.InitialVehicleId}' does not satisfy " +
                $"the rules of battle mode '{mode.ModeId}'.");
        }

        return unitOfWork.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                BattleQueueEntry? existingEntry =
                    await queueEntryRepository
                        .FindActiveByUserIdAsync(
                            command.UserId,
                            transactionCancellationToken);

                if (existingEntry is not null)
                {
                    if (RepresentsSameQueueRequest(
                            existingEntry,
                            command,
                            mode))
                    {
                        return await BuildStatusAsync(
                            existingEntry,
                            transactionCancellationToken);
                    }

                    throw new InvalidOperationException(
                        $"User '{command.UserId:D}' already has an active " +
                        $"queue entry '{existingEntry.QueueEntryId:D}'.");
                }

                BattleQueueEntry queueEntry =
                    BattleQueueEntry.Create(
                        userId: command.UserId,
                        deckId: command.DeckId,
                        modeId: mode.ModeId,
                        modeRevision: mode.Revision,
                        initialVehicleId:
                            command.InitialVehicleId,
                        vehicleBattleRatingTenths:
                            command.VehicleBattleRatingTenths,
                        vehicleClass:
                            command.VehicleClass,
                        utcNow:
                            timeProvider
                                .GetUtcNow()
                                .UtcDateTime);

                queueEntryRepository.Add(queueEntry);

                return new BattleQueueStatusSnapshot(
                    HasActiveQueueEntry: true,
                    QueueEntry: ToSnapshot(queueEntry),
                    MatchStatus: null,
                    MapId: null,
                    IsReadyToConnect: false);
            },
            cancellationToken);
    }

    public async Task<BattleQueueStatusSnapshot>
        GetStatusAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User id must not be empty.",
                nameof(userId));
        }

        BattleQueueEntry? queueEntry =
            await queueEntryRepository
                .FindActiveByUserIdAsync(
                    userId,
                    cancellationToken);

        if (queueEntry is null)
        {
            return EmptyStatus();
        }

        return await BuildStatusAsync(
            queueEntry,
            cancellationToken);
    }

    public Task<BattleQueueLeaveSnapshot> LeaveAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User id must not be empty.",
                nameof(userId));
        }

        return unitOfWork.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                BattleQueueEntry? queueEntry =
                    await queueEntryRepository
                        .FindActiveByUserIdAsync(
                            userId,
                            transactionCancellationToken);

                if (queueEntry is null)
                {
                    return new BattleQueueLeaveSnapshot(
                        HadActiveQueueEntry: false,
                        WasCancelled: false,
                        MatchAlreadyFormed: false,
                        Status: EmptyStatus());
                }

                if (queueEntry.Status ==
                    BattleQueueEntryStatus.Matched)
                {
                    return new BattleQueueLeaveSnapshot(
                        HadActiveQueueEntry: true,
                        WasCancelled: false,
                        MatchAlreadyFormed: true,
                        Status: await BuildStatusAsync(
                            queueEntry,
                            transactionCancellationToken));
                }

                queueEntry.Cancel(
                    timeProvider.GetUtcNow().UtcDateTime);

                return new BattleQueueLeaveSnapshot(
                    HadActiveQueueEntry: true,
                    WasCancelled: true,
                    MatchAlreadyFormed: false,
                    Status: new BattleQueueStatusSnapshot(
                        HasActiveQueueEntry: false,
                        QueueEntry: null,
                        MatchStatus: null,
                        MapId: null,
                        IsReadyToConnect: false));
            },
            cancellationToken);
    }

    public async Task<BattleJoinTicketIssueSnapshot>
        IssueConnectionTicketAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User id must not be empty.",
                nameof(userId));
        }

        BattleQueueEntry? queueEntry =
            await queueEntryRepository
                .FindActiveByUserIdAsync(
                    userId,
                    cancellationToken);

        if (queueEntry is null)
        {
            throw new InvalidOperationException(
                $"User '{userId:D}' does not have an active queue entry.");
        }

        if (queueEntry.Status !=
            BattleQueueEntryStatus.Matched)
        {
            throw new InvalidOperationException(
                $"Queue entry '{queueEntry.QueueEntryId:D}' " +
                "has not been matched yet.");
        }

        if (queueEntry.MatchId is not Guid matchId)
        {
            throw new InvalidOperationException(
                $"Matched queue entry '{queueEntry.QueueEntryId:D}' " +
                "does not reference a battle match.");
        }

        BattleMatch? battleMatch =
            await matchRepository.FindByIdAsync(
                matchId,
                cancellationToken);

        if (battleMatch is null)
        {
            throw new InvalidOperationException(
                $"Battle match '{matchId:D}' no longer exists.");
        }

        if (battleMatch.Status !=
            BattleMatchStatus.WaitingForPlayers)
        {
            throw new InvalidOperationException(
                $"Battle match '{matchId:D}' is not ready for " +
                "player connections yet.");
        }

        BattleMatchPlayer? matchPlayer =
            await matchPlayerRepository
                .FindByMatchAndUserAsync(
                    battleMatch.MatchId,
                    userId,
                    cancellationToken);

        if (matchPlayer is null)
        {
            throw new InvalidOperationException(
                $"User '{userId:D}' is not reserved for " +
                $"battle match '{battleMatch.MatchId:D}'.");
        }

        return await battleJoinTicketIssuer.IssueAsync(
            new IssueBattleJoinTicketRequest(
                battleMatch.MatchId,
                userId,
                matchPlayer.InitialVehicleId),
            cancellationToken);
    }

    private async Task<BattleQueueStatusSnapshot>
        BuildStatusAsync(
            BattleQueueEntry queueEntry,
            CancellationToken cancellationToken)
    {
        if (queueEntry.Status !=
            BattleQueueEntryStatus.Matched ||
            queueEntry.MatchId is not Guid matchId)
        {
            return new BattleQueueStatusSnapshot(
                HasActiveQueueEntry: true,
                QueueEntry: ToSnapshot(queueEntry),
                MatchStatus: null,
                MapId: null,
                IsReadyToConnect: false);
        }

        BattleMatch? battleMatch =
            await matchRepository.FindByIdAsync(
                matchId,
                cancellationToken);

        if (battleMatch is null)
        {
            return new BattleQueueStatusSnapshot(
                HasActiveQueueEntry: true,
                QueueEntry: ToSnapshot(queueEntry),
                MatchStatus: "missing",
                MapId: null,
                IsReadyToConnect: false);
        }

        bool isReadyToConnect =
            battleMatch.Status ==
            BattleMatchStatus.WaitingForPlayers;

        return new BattleQueueStatusSnapshot(
            HasActiveQueueEntry: true,
            QueueEntry: ToSnapshot(queueEntry),
            MatchStatus: ToStatusValue(
                battleMatch.Status),
            MapId: battleMatch.MapId,
            IsReadyToConnect: isReadyToConnect);
    }

    private static bool IsVehicleAllowedByMode(
        BattleModeDefinition mode,
        byte vehicleBattleRatingTenths,
        string vehicleClass)
    {
        if (vehicleBattleRatingTenths <
                mode.VehicleRules
                    .MinimumBattleRatingTenths ||
            vehicleBattleRatingTenths >
                mode.VehicleRules
                    .MaximumBattleRatingTenths)
        {
            return false;
        }

        return mode.VehicleRules
            .AllowedVehicleClasses
            .Any(
                allowedVehicleClass =>
                    string.Equals(
                        allowedVehicleClass,
                        vehicleClass.Trim(),
                        StringComparison.OrdinalIgnoreCase));
    }

    private static bool RepresentsSameQueueRequest(
        BattleQueueEntry existingEntry,
        JoinBattleQueueCommand command,
        BattleModeDefinition mode)
    {
        return existingEntry.DeckId == command.DeckId &&
               string.Equals(
                   existingEntry.ModeId,
                   mode.ModeId,
                   StringComparison.OrdinalIgnoreCase) &&
               existingEntry.ModeRevision == mode.Revision &&
               string.Equals(
                   existingEntry.InitialVehicleId,
                   command.InitialVehicleId.Trim(),
                   StringComparison.Ordinal) &&
               existingEntry.VehicleBattleRatingTenths ==
                   command.VehicleBattleRatingTenths &&
               string.Equals(
                   existingEntry.VehicleClass,
                   command.VehicleClass.Trim(),
                   StringComparison.OrdinalIgnoreCase);
    }

    private static BattleQueueEntrySnapshot ToSnapshot(
        BattleQueueEntry queueEntry)
    {
        return new BattleQueueEntrySnapshot(
            queueEntry.QueueEntryId,
            queueEntry.UserId,
            queueEntry.DeckId,
            queueEntry.ModeId,
            queueEntry.ModeRevision,
            queueEntry.InitialVehicleId,
            queueEntry.VehicleBattleRatingTenths,
            queueEntry.VehicleClass,
            queueEntry.Status.ToString(),
            queueEntry.MatchId,
            queueEntry.QueuedAtUtc,
            queueEntry.UpdatedAtUtc,
            queueEntry.MatchedAtUtc,
            queueEntry.FailureReason);
    }

    private static BattleQueueStatusSnapshot EmptyStatus()
    {
        return new BattleQueueStatusSnapshot(
            HasActiveQueueEntry: false,
            QueueEntry: null,
            MatchStatus: null,
            MapId: null,
            IsReadyToConnect: false);
    }

    private static string ToStatusValue(
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