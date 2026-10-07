namespace Ironfront.UserService.Domain.Entities;

public sealed class UserVehicle
{
    private UserVehicle()
    {
    }

    public Guid UserId { get; private set; }

    public string VehicleId { get; private set; } = string.Empty;

    public DateTime UnlockedAtUtc { get; private set; }

    public static UserVehicle Create(
        Guid userId,
        string vehicleId,
        DateTime unlockedAtUtc)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(vehicleId))
            throw new ArgumentException("Vehicle id cannot be empty.", nameof(vehicleId));

        return new UserVehicle
        {
            UserId = userId,
            VehicleId = vehicleId.Trim(),
            UnlockedAtUtc = unlockedAtUtc
        };
    }
}