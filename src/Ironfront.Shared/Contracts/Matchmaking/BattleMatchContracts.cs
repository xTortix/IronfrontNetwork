namespace Ironfront.Shared.Contracts.Matchmaking;

public sealed record DevelopmentBattleMatchCreateRequest(
    string ModeId,
    string MapId,
    int ExpectedPlayerCount);

public sealed record BattleMatchResponse(
    Guid MatchId,
    string BattleServerInstanceId,
    string ModeId,
    int ModeRevision,
    string MapId,
    int ExpectedPlayerCount,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? WaitingForPlayersAtUtc,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    string? FailureReason);
    
public sealed record BattleMatchCancellationRequest(
    string? Reason);