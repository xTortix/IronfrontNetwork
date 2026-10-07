namespace Ironfront.Shared.Contracts.Matchmaking;

public sealed record CreateBattleServerMatchPayloadContract(
    Guid MatchId,
    string ModeId,
    int ModeRevision,
    string MapId,
    int ExpectedPlayerCount,
    string RulesSnapshotJson,
    IReadOnlyList<CreateBattleServerMatchPlayerPayloadContract> Players);

public sealed record CreateBattleServerMatchPlayerPayloadContract(
    Guid MatchPlayerId,
    Guid UserId,
    byte TeamId,
    int TeamSlotIndex,
    string InitialVehicleId,
    int PlayerSlotIndex);