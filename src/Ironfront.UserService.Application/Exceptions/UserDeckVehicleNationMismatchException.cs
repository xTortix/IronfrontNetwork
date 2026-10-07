namespace Ironfront.UserService.Application.Exceptions;

public sealed class UserDeckVehicleNationMismatchException : Exception
{
    public UserDeckVehicleNationMismatchException(
        Guid deckId,
        string deckNation,
        string vehicleId,
        string vehicleNation)
        : base(
            $"Vehicle '{vehicleId}' belongs to nation '{vehicleNation}' and cannot be assigned to deck '{deckId}' with nation '{deckNation}'.")
    {
        DeckId = deckId;
        DeckNation = deckNation;
        VehicleId = vehicleId;
        VehicleNation = vehicleNation;
    }

    public Guid DeckId { get; }

    public string DeckNation { get; }

    public string VehicleId { get; }

    public string VehicleNation { get; }
}