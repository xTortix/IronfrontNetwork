namespace Ironfront.UserService.Application.Models;

public sealed record CreateUserDeckCommand(
    Guid UserId,
    string Name,
    string Nation);

public sealed record UserDeckCreationResult(
    Guid DeckId,
    Guid UserId,
    string Name,
    string Nation,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
    
public sealed record ActivateUserDeckCommand(
    Guid UserId,
    Guid DeckId);

public sealed record UserDeckActivationResult(
    Guid DeckId,
    Guid UserId,
    string Name,
    string Nation,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
    
public sealed record AssignUserDeckVehicleCommand(
    Guid UserId,
    Guid DeckId,
    int SlotIndex,
    string VehicleId);

public sealed record UserDeckSlotAssignmentResult(
    Guid UserId,
    Guid DeckId,
    int SlotIndex,
    string VehicleId,
    DateTime AddedAtUtc);
    
public sealed record ClearUserDeckSlotCommand(
    Guid UserId,
    Guid DeckId,
    int SlotIndex);

public sealed record UserDeckSlotClearResult(
    Guid UserId,
    Guid DeckId,
    int SlotIndex,
    bool WasCleared,
    DateTime? ClearedAtUtc);