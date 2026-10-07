using Ironfront.UserService.Application.Models;

namespace Ironfront.UserService.Application;

public interface IUserDeckService
{
    Task<UserDeckCreationResult> CreateDeckAsync(
        CreateUserDeckCommand command,
        CancellationToken cancellationToken);
    
    Task<UserDeckActivationResult> ActivateDeckAsync(
        ActivateUserDeckCommand command,
        CancellationToken cancellationToken);
    
    Task<UserDeckSlotAssignmentResult> AssignVehicleToDeckSlotAsync(
        AssignUserDeckVehicleCommand command,
        CancellationToken cancellationToken);
    
    Task<UserDeckSlotClearResult> ClearDeckSlotAsync(
        ClearUserDeckSlotCommand command,
        CancellationToken cancellationToken);
}