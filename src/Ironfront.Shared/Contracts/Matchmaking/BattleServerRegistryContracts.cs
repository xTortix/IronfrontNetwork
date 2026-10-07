namespace Ironfront.Shared.Contracts.Matchmaking;

public sealed record BattleServerRegistrationRequest(
    string ServerInstanceId,
    string HostId,
    string Region,
    string PublicHost,
    int PublicPort,
    string BuildVersion,
    string CatalogVersion,
    int MaxPlayers);

public sealed record BattleServerHeartbeatRequest(
    int PlayerCount);

public sealed record BattleServerSlotResponse(
    string ServerInstanceId,
    string HostId,
    string Region,
    string PublicHost,
    int PublicPort,
    string BuildVersion,
    string CatalogVersion,
    string Status,
    int PlayerCount,
    int MaxPlayers,
    Guid? ActiveMatchId,
    DateTime RegisteredAtUtc,
    DateTime LastHeartbeatUtc,
    DateTime UpdatedAtUtc);