namespace Ironfront.UserService.Domain.Rules;

public static class DeckRules
{
    public const int MaxSlotCount = 5;

    public const int MaxVehicleIdLength = 128;
    
    public const int MaxDeckNameLength = 64;

    public const int MaxNationLength = 32;

    public static bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0
               && slotIndex < MaxSlotCount;
    }
}