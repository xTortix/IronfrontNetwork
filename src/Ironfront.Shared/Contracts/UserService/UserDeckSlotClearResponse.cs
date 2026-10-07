namespace Ironfront.Shared.Contracts.UserService;

public sealed record UserDeckSlotClearResponse(
    Guid UserId,
    Guid DeckId,
    int SlotIndex,
    bool WasCleared,
    DateTime? ClearedAtUtc);