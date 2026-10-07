namespace Ironfront.Shared.Contracts.Hangar;

public sealed record AssignVehicleToHangarDeckSlotRequest(
    string VehicleId);

public sealed record HangarDeckSlotVehicleAssignmentResponse(
    Guid DeckId,
    int SlotIndex,
    string VehicleId,
    DateTime AddedAtUtc);