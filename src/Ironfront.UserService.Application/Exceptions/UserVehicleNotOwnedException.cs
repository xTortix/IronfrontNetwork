namespace Ironfront.UserService.Application.Exceptions;

public sealed class UserVehicleNotOwnedException : Exception
{
    public UserVehicleNotOwnedException(
        Guid userId,
        string vehicleId)
        : base(
            $"Vehicle '{vehicleId}' is not owned by user '{userId}'.")
    {
        UserId = userId;
        VehicleId = vehicleId;
    }

    public Guid UserId { get; }

    public string VehicleId { get; }
}