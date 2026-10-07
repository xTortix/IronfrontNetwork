namespace Ironfront.Shared.Contracts.Matchmaking;

public sealed record JoinBattleQueueRequest(
    Guid DeckId,
    string ModeId,
    string VehicleId);

public sealed record BattleModeResponse(
    string ModeId,
    int Revision,
    string DisplayName,
    string Description,
    string Kind,
    byte MinimumBattleRatingTenths,
    byte MaximumBattleRatingTenths,
    IReadOnlyList<BattleModeTeamResponse> Teams);

public sealed record BattleModeTeamResponse(
    byte TeamId,
    string DisplayName,
    byte RequiredPlayerCount);

public sealed record BattleQueueEntryStatusResponse(
    Guid QueueEntryId,
    Guid DeckId,
    string ModeId,
    int ModeRevision,
    string VehicleId,
    string Status,
    Guid? MatchId,
    DateTime QueuedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? MatchedAtUtc,
    string? FailureReason);

public sealed record BattleQueueStatusResponse(
    bool HasActiveQueueEntry,
    BattleQueueEntryStatusResponse? QueueEntry,
    string? MatchStatus,
    string? MapId,
    bool IsReadyToConnect);

public sealed record BattleQueueLeaveResponse(
    bool HadActiveQueueEntry,
    bool WasCancelled,
    bool MatchAlreadyFormed,
    BattleQueueStatusResponse Status);

public sealed record BattleConnectionTicketResponse(
    Guid MatchId,
    Guid TicketId,
    string BattleServerPublicHost,
    int BattleServerPublicPort,
    DateTime ExpiresAtUtc,
    string SecretBase64);