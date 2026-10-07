using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Application.Models;

public sealed record BattleServerRegistrationCommand(
    string ServerInstanceId,
    string HostId,
    string Region,
    string PublicHost,
    int PublicPort,
    string BuildVersion,
    string CatalogVersion,
    int MaxPlayers);

public sealed record BattleServerHeartbeatCommand(
    string ServerInstanceId,
    int PlayerCount);

public sealed record BattleServerSlotSnapshot(
    string ServerInstanceId,
    string HostId,
    string Region,
    string PublicHost,
    int PublicPort,
    string BuildVersion,
    string CatalogVersion,
    BattleServerSlotStatus Status,
    int PlayerCount,
    int MaxPlayers,
    Guid? ActiveMatchId,
    DateTime RegisteredAtUtc,
    DateTime LastHeartbeatUtc,
    DateTime UpdatedAtUtc);