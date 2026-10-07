namespace Ironfront.Shared.Contracts.Matchmaking;

public sealed record BattleServerCommandPollResponse(
    Guid CommandId,
    string CommandType,
    string PayloadJson,
    DateTime CreatedAtUtc,
    DateTime? DeliveredAtUtc);

public sealed record BattleServerCommandAcknowledgementRequest(
    bool Succeeded,
    string? FailureReason);

public sealed record BattleServerCommandAcknowledgementResponse(
    Guid CommandId,
    string State,
    DateTime? AcknowledgedAtUtc,
    DateTime? FailedAtUtc,
    string? FailureReason);