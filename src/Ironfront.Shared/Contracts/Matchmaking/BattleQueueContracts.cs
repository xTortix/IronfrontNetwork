namespace Ironfront.Shared.Contracts.Matchmaking;

public sealed record InternalJoinBattleQueueRequest(
    Guid UserId,
    Guid DeckId,
    string ModeId,
    string InitialVehicleId,
    byte VehicleBattleRatingTenths,
    string VehicleClass);

public sealed record BattleQueueEntryResponse(
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

public sealed record InternalBattleQueueStatusResponse(
    bool HasActiveQueueEntry,
    BattleQueueEntryResponse? QueueEntry,
    string? MatchStatus,
    string? MapId,
    bool IsReadyToConnect);

public sealed record InternalBattleQueueLeaveResponse(
    bool HadActiveQueueEntry,
    bool WasCancelled,
    bool MatchAlreadyFormed,
    InternalBattleQueueStatusResponse Status);

public sealed record InternalBattleConnectionTicketResponse(
    Guid MatchId,
    Guid TicketId,
    string BattleServerPublicHost,
    int BattleServerPublicPort,
    DateTime ExpiresAtUtc,
    string SecretBase64);