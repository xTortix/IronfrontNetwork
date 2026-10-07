namespace Ironfront.UserService.Application.Models;

public sealed record HangarVehicleInfo(
    string VehicleId,
    string DisplayName,
    string Nation,
    string VehicleClass,
    byte BattleRatingTenths);

public sealed record HangarDeckSlot(
    int SlotIndex,
    HangarVehicleInfo? Vehicle);

public sealed record HangarDeck(
    Guid DeckId,
    string Name,
    string Nation,
    bool IsActive,
    byte? BattleRatingTenths,
    IReadOnlyList<HangarDeckSlot> Slots);

public sealed record UserHangarSnapshot(
    Guid UserId,
    Guid ActiveDeckId,
    IReadOnlyList<HangarVehicleInfo> OwnedVehicles,
    IReadOnlyList<HangarDeck> Decks);