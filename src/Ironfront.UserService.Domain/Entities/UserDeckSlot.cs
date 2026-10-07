using Ironfront.UserService.Domain.Rules;

namespace Ironfront.UserService.Domain.Entities;

public sealed class UserDeckSlot
{
    private UserDeckSlot()
    {
    }

    public Guid DeckId { get; private set; }

    public int SlotIndex { get; private set; }

    public string VehicleId { get; private set; } = string.Empty;

    // Zeitpunkt, zu dem das aktuell eingetragene Fahrzeug
    // diesem Slot zugewiesen wurde.
    public DateTime AddedAtUtc { get; private set; }

    public static UserDeckSlot Create(
        Guid deckId,
        int slotIndex,
        string vehicleId,
        DateTime utcNow)
    {
        if (deckId == Guid.Empty)
        {
            throw new ArgumentException(
                "Deck ID must not be empty.",
                nameof(deckId));
        }

        if (!DeckRules.IsValidSlotIndex(slotIndex))
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotIndex),
                slotIndex,
                $"Slot index must be between 0 and {DeckRules.MaxSlotCount - 1}.");
        }

        return new UserDeckSlot
        {
            DeckId = deckId,
            SlotIndex = slotIndex,
            VehicleId = NormalizeVehicleId(vehicleId),
            AddedAtUtc = utcNow
        };
    }

    public void ReplaceVehicle(
        string vehicleId,
        DateTime utcNow)
    {
        VehicleId = NormalizeVehicleId(vehicleId);
        AddedAtUtc = utcNow;
    }

    private static string NormalizeVehicleId(
        string vehicleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vehicleId);

        string normalizedVehicleId = vehicleId.Trim();

        if (normalizedVehicleId.Length > DeckRules.MaxVehicleIdLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vehicleId),
                $"Vehicle ID must not exceed {DeckRules.MaxVehicleIdLength} characters.");
        }

        return normalizedVehicleId;
    }
}
