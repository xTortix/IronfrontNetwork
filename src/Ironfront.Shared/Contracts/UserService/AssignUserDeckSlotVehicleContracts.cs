namespace Ironfront.Shared.Contracts.UserService;

public sealed record AssignUserDeckSlotVehicleRequest(
    string VehicleId);

public sealed record UserDeckSlotAssignmentResponse(
    Guid UserId,
    Guid DeckId,
    int SlotIndex,
    string VehicleId,
    DateTime AddedAtUtc);