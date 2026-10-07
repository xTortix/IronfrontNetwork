namespace Ironfront.Shared.Contracts.Hangar;

public sealed record HangarSnapshotResponse(
    Guid ActiveDeckId,
    IReadOnlyList<HangarVehicleResponse> OwnedVehicles,
    IReadOnlyList<HangarDeckResponse> Decks);

public sealed record HangarVehicleResponse(
    string VehicleId,
    string DisplayName,
    string Nation,
    string VehicleClass,
    byte BattleRatingTenths);

public sealed record HangarDeckResponse(
    Guid DeckId,
    string Name,
    string Nation,
    bool IsActive,
    byte? BattleRatingTenths,
    IReadOnlyList<HangarDeckSlotResponse> Slots);

public sealed record HangarDeckSlotResponse(
    int SlotIndex,
    HangarVehicleResponse? Vehicle);