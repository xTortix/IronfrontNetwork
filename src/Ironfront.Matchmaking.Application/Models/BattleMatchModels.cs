namespace Ironfront.Matchmaking.Application.Models;

public sealed record CreateBattleMatchRequest(
    string ModeId,
    int ModeRevision,
    string RulesSnapshotJson,
    string MapId,
    int ExpectedPlayerCount);

public sealed record BattleMatchSnapshot(
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

/*
 * Payload für den BattleServer-Command "match.create.v1".
 *
 * Der BattleServer bekommt damit alles, was er braucht, um ein
 * Match ohne Konsoleneingabe vorzubereiten:
 *
 * - Match-ID
 * - Mode/Map
 * - eingefrorenen Rule-Snapshot
 * - reservierte Spieler inklusive Team, Slot und Startfahrzeug
 */
public sealed record CreateBattleServerMatchPayload(
    Guid MatchId,
    string ModeId,
    int ModeRevision,
    string MapId,
    int ExpectedPlayerCount,
    string RulesSnapshotJson,
    IReadOnlyList<CreateBattleServerMatchPlayerPayload> Players);

public sealed record CreateBattleServerMatchPlayerPayload(
    Guid MatchPlayerId,
    Guid UserId,
    byte TeamId,
    int TeamSlotIndex,
    string InitialVehicleId,
    int PlayerSlotIndex);