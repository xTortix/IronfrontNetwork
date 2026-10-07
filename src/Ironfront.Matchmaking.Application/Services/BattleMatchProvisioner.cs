using System.Text.Json;
using Ironfront.Matchmaking.Application.Configuration;
using Ironfront.Matchmaking.Application.Exceptions;
using Ironfront.Matchmaking.Application.Models;
using Ironfront.Matchmaking.Domain.Entities;
using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Application.Services;

public sealed class BattleMatchProvisioner
    : IBattleMatchProvisioner
{
    private const string CreateMatchCommandType =
        "match.create.v1";

    private static readonly JsonSerializerOptions
        PayloadJsonOptions =
            new(JsonSerializerDefaults.Web);

    private readonly IBattleServerSlotRepository
        slotRepository;

    private readonly IBattleMatchRepository
        matchRepository;

    private readonly IBattleServerCommandRepository
        commandRepository;

    private readonly IMatchmakingUnitOfWork
        unitOfWork;

    private readonly BattleServerSelectionPolicy
        selectionPolicy;

    private readonly TimeProvider timeProvider;

    public BattleMatchProvisioner(
        IBattleServerSlotRepository slotRepository,
        IBattleMatchRepository matchRepository,
        IBattleServerCommandRepository commandRepository,
        IMatchmakingUnitOfWork unitOfWork,
        BattleServerSelectionPolicy selectionPolicy,
        TimeProvider timeProvider)
    {
        this.slotRepository = slotRepository
            ?? throw new ArgumentNullException(
                nameof(slotRepository));

        this.matchRepository = matchRepository
            ?? throw new ArgumentNullException(
                nameof(matchRepository));

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

    public Task<BattleMatchSnapshot> ProvisionAsync(
        CreateBattleMatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return unitOfWork.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                DateTime utcNow =
                    timeProvider.GetUtcNow().UtcDateTime;

                DateTime minimumLastHeartbeatUtc =
                    utcNow - selectionPolicy.MaximumHeartbeatAge;

                BattleServerSlot? slot =
                    await slotRepository
                        .TryAcquireReadyForReservationAsync(
                            minimumLastHeartbeatUtc,
                            request.ExpectedPlayerCount,
                            transactionCancellationToken);

                if (slot is null)
                {
                    throw new NoReadyBattleServerSlotException();
                }

                BattleMatch battleMatch =
                    BattleMatch.Create(
                        battleServerInstanceId:
                        slot.ServerInstanceId,

                        modeId:
                        request.ModeId,

                        modeRevision:
                        request.ModeRevision,

                        rulesSnapshotJson:
                        request.RulesSnapshotJson,

                        mapId:
                        request.MapId,

                        expectedPlayerCount:
                        request.ExpectedPlayerCount,

                        utcNow:
                        utcNow);

                slot.ReserveForMatch(
                    battleMatch.MatchId,
                    battleMatch.ExpectedPlayerCount,
                    utcNow);

                var commandPayload =
                    new CreateBattleServerMatchPayload(
                        battleMatch.MatchId,
                        battleMatch.ModeId,
                        battleMatch.ModeRevision,
                        battleMatch.MapId,
                        battleMatch.ExpectedPlayerCount,
                        battleMatch.RulesSnapshotJson,
                        Array.Empty<CreateBattleServerMatchPlayerPayload>());

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
                commandRepository.Add(command);

                return ToSnapshot(battleMatch);
            },
            cancellationToken);
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
            BattleMatchStatus.Provisioning => "provisioning",
            BattleMatchStatus.WaitingForPlayers =>
                "waiting_for_players",
            BattleMatchStatus.Running => "running",
            BattleMatchStatus.Finishing => "finishing",
            BattleMatchStatus.Completed => "completed",
            BattleMatchStatus.Failed => "failed",
            BattleMatchStatus.Cancelled => "cancelled",
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown battle match status.")
        };
    }
}