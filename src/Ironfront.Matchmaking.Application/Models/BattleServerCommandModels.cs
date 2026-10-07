using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Application.Models;

public sealed record BattleServerCommandSnapshot(
    Guid CommandId,
    string BattleServerInstanceId,
    string CommandType,
    string PayloadJson,
    BattleServerCommandState State,
    DateTime CreatedAtUtc,
    DateTime? DeliveredAtUtc,
    DateTime? AcknowledgedAtUtc,
    DateTime? FailedAtUtc,
    string? FailureReason);

public sealed record BattleServerCommandAcknowledgement(
    Guid CommandId,
    bool Succeeded,
    string? FailureReason);