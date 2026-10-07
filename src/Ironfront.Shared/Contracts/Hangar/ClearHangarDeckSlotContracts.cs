namespace Ironfront.Shared.Contracts.Hangar;

public sealed record HangarDeckSlotClearResponse(
    Guid DeckId,
    int SlotIndex,
    bool WasCleared,
    DateTime? ClearedAtUtc);