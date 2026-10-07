using Ironfront.UserService.Domain.Entities;

namespace Ironfront.UserService.Application;

public interface IUserHangarRepository
{
    Task<UserProfile?> FindProfileAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserVehicle>> GetOwnedVehiclesAsync(
        Guid userId,
        CancellationToken cancellationToken);
    
    void AddProfile(UserProfile profile);

    void AddVehicle(UserVehicle vehicle);

    Task SaveChangesAsync(CancellationToken cancellationToken);
    
    Task<IReadOnlyList<UserDeck>> GetDecksAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserDeckSlot>> GetDeckSlotsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);
    
    Task<UserDeck?> FindActiveDeckAsync(
        Guid userId,
        CancellationToken cancellationToken);
    
    Task<UserDeck?> FindDeckAsync(
        Guid userId,
        Guid deckId,
        CancellationToken cancellationToken);

    Task<UserDeckSlot?> FindDeckSlotAsync(
        Guid deckId,
        int slotIndex,
        CancellationToken cancellationToken);

    Task<bool> UserOwnsVehicleAsync(
        Guid userId,
        string vehicleId,
        CancellationToken cancellationToken);

    Task<bool> IsVehicleAssignedToAnotherSlotAsync(
        Guid deckId,
        string vehicleId,
        int targetSlotIndex,
        CancellationToken cancellationToken);
    
    Task<UserDeck?> SetActiveDeckAsync(
        Guid userId,
        Guid deckId,
        DateTime utcNow,
        CancellationToken cancellationToken);
    
    void AddDeck(UserDeck deck);

    void AddDeckSlot(UserDeckSlot deckSlot);
    
    void RemoveDeckSlot(UserDeckSlot deckSlot);
}
