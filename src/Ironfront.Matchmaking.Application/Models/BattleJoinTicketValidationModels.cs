namespace Ironfront.Matchmaking.Application.Models;

public sealed record ValidateBattleJoinTicketRequest(
    Guid MatchId,
    Guid TicketId,
    string SecretBase64);

public sealed record BattleJoinTicketValidationSnapshot(
    bool Approved,
    string RejectionCode,
    Guid? MatchId,
    Guid? MatchPlayerId,
    Guid? UserId,
    string? InitialVehicleId,
    byte? TeamId,
    int? TeamSlotIndex,
    int? PlayerSlotIndex);