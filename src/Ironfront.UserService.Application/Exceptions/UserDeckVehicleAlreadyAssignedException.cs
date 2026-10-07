namespace Ironfront.UserService.Application.Exceptions;

public sealed class UserDeckVehicleAlreadyAssignedException : Exception
{
    public UserDeckVehicleAlreadyAssignedException(
        Guid deckId,
        string vehicleId)
        : base(
            $"Vehicle '{vehicleId}' is already assigned to another slot in deck '{deckId}'.")
    {
        DeckId = deckId;
        VehicleId = vehicleId;
    }

    public Guid DeckId { get; }

    public string VehicleId { get; }
}