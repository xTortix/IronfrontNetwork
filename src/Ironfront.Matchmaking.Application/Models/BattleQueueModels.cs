namespace Ironfront.Matchmaking.Application.Models;

public sealed record JoinBattleQueueCommand(
    Guid UserId,
    Guid DeckId,
    string ModeId,
    string InitialVehicleId,
    byte VehicleBattleRatingTenths,
    string VehicleClass);

public sealed record BattleQueueEntrySnapshot(
    Guid QueueEntryId,
    Guid UserId,
    Guid DeckId,
    string ModeId,
    int ModeRevision,
    string InitialVehicleId,
    byte VehicleBattleRatingTenths,
    string VehicleClass,
    string Status,
    Guid? MatchId,
    DateTime QueuedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? MatchedAtUtc,
    string? FailureReason);

public sealed record BattleQueueStatusSnapshot(
    bool HasActiveQueueEntry,
    BattleQueueEntrySnapshot? QueueEntry,
    string? MatchStatus,
    string? MapId,
    bool IsReadyToConnect);

public sealed record BattleQueueLeaveSnapshot(
    bool HadActiveQueueEntry,
    bool WasCancelled,
    bool MatchAlreadyFormed,
    BattleQueueStatusSnapshot Status);