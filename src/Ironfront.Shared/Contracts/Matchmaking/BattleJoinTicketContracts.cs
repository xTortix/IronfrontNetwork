namespace Ironfront.Shared.Contracts.Matchmaking;

public sealed record DevelopmentBattleJoinTicketIssueRequest(
    Guid UserId,
    string InitialVehicleId);

public sealed record BattleJoinTicketIssueResponse(
    Guid TicketId,
    Guid MatchId,
    Guid MatchPlayerId,
    string BattleServerInstanceId,
    string BattleServerPublicHost,
    int BattleServerPublicPort,
    DateTime ExpiresAtUtc,
    string SecretBase64);
    
public sealed record BattleJoinTicketValidationRequest(
    Guid MatchId,
    Guid TicketId,
    string SecretBase64);

public sealed record BattleJoinTicketValidationResponse(
    bool Approved,
    string RejectionCode,
    Guid? MatchId,
    Guid? MatchPlayerId,
    Guid? UserId,
    string? InitialVehicleId,
    byte? TeamId,
    int? TeamSlotIndex,
    int? PlayerSlotIndex);