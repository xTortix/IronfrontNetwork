namespace Ironfront.Matchmaking.Application.Models;

public sealed record IssueBattleJoinTicketRequest(
    Guid MatchId,
    Guid UserId,
    string InitialVehicleId);

public sealed record BattleJoinTicketIssueSnapshot(
    Guid TicketId,
    Guid MatchId,
    Guid MatchPlayerId,
    string BattleServerInstanceId,
    string BattleServerPublicHost,
    int BattleServerPublicPort,
    DateTime ExpiresAtUtc,
    string SecretBase64);