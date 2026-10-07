namespace Ironfront.UserService.Application.Models;

public sealed record ProvisionUserCommand(
    Guid UserId,
    string Username,
    string Email);

public sealed record ProvisionedUserProfile(
    Guid UserId,
    string Username,
    string Email,
    Guid ActiveDeckId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    bool WasCreated);

public sealed record StarterVehicleGrant(
    string VehicleId);

public sealed record StarterDeckGrant(string Name);