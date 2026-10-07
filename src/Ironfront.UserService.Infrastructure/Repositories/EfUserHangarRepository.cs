using Ironfront.UserService.Application;
using Ironfront.UserService.Domain.Entities;
using Ironfront.UserService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ironfront.UserService.Infrastructure.Repositories;

public sealed class EfUserHangarRepository : IUserHangarRepository
{
    private readonly UserDbContext dbContext;

    public EfUserHangarRepository(UserDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<UserProfile?> FindProfileAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.UserProfiles
        .SingleOrDefaultAsync(
            profile => profile.UserId == userId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<UserVehicle>> GetOwnedVehiclesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.UserVehicles
        .AsNoTracking()
        .Where(vehicle => vehicle.UserId == userId)
        .OrderBy(vehicle => vehicle.UnlockedAtUtc)
        .ToListAsync(cancellationToken);
    }
    
    public Task<UserDeck?> FindActiveDeckAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.UserDecks
            .SingleOrDefaultAsync(
                deck => deck.UserId == userId && deck.IsActive,
                cancellationToken);
    }
    
    public Task<UserDeck?> FindDeckAsync(
        Guid userId,
        Guid deckId,
        CancellationToken cancellationToken)
    {
        return dbContext.UserDecks
            .SingleOrDefaultAsync(
                deck => deck.UserId == userId
                        && deck.DeckId == deckId,
                cancellationToken);
    }

    public Task<UserDeckSlot?> FindDeckSlotAsync(
        Guid deckId,
        int slotIndex,
        CancellationToken cancellationToken)
    {
        // Absichtlich ohne AsNoTracking:
        // Ein bestehender Slot muss später per ReplaceVehicle(...)
        // cancellationToken)
        {
            // Absichtlich ohne AsNoTracking:
            // Ein bestehender Slot muss später per Replace verändert und gespeichert werden können.
            return dbContext.UserDeckSlots
                .SingleOrDefaultAsync(
                    slot => slot.DeckId == deckId
                            && slot.SlotIndex == slotIndex,
                    cancellationToken);
        }
    }
        
    public Task<bool> UserOwnsVehicleAsync(
            Guid userId,
            string vehicleId,
            CancellationToken cancellationToken)
    {
        return dbContext.UserVehicles.AnyAsync(
            vehicle => vehicle.UserId == userId 
                       && vehicle.VehicleId 
                       == vehicleId,
                    cancellationToken);
    }

    public Task<bool> IsVehicleAssignedToAnotherSlotAsync(
            Guid deckId,
            string vehicleId,
            int targetSlotIndex,
            CancellationToken cancellationToken)
    {
        return dbContext.UserDeckSlots.AnyAsync(
                slot => slot.DeckId == deckId 
                        && slot.VehicleId == vehicleId 
                        && slot.SlotIndex != targetSlotIndex,
                cancellationToken);
    }
    
    public async Task<UserDeck?> SetActiveDeckAsync(
        Guid userId,
        Guid deckId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        UserDeck? targetDeck = await dbContext.UserDecks
            .SingleOrDefaultAsync(
                deck => deck.UserId == userId
                        && deck.DeckId == deckId,
                cancellationToken);

        if (targetDeck is null)
        {
            await transaction.RollbackAsync(cancellationToken);

            return null;
        }

        await dbContext.UserDecks
            .Where(deck => deck.UserId == userId
                           && deck.IsActive
                           && deck.DeckId != deckId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        deck => deck.IsActive,
                        false)
                    .SetProperty(
                        deck => deck.UpdatedAtUtc,
                        utcNow),
                cancellationToken);

        targetDeck.SetActive(
            isActive: true,
            utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return targetDeck;
    }
    
    public async Task<IReadOnlyList<UserDeck>> GetDecksAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.UserDecks
            .AsNoTracking()
            .Where(deck => deck.UserId == userId)
            .OrderByDescending(deck => deck.IsActive)
            .ThenBy(deck => deck.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserDeckSlot>> GetDeckSlotsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await (
                from slot in dbContext.UserDeckSlots.AsNoTracking()
                join deck in dbContext.UserDecks.AsNoTracking()
                    on slot.DeckId equals deck.DeckId
                where deck.UserId == userId
                orderby deck.IsActive descending,
                    deck.CreatedAtUtc,
                    slot.SlotIndex
                select slot)
            .ToListAsync(cancellationToken);
    }
    
    public void AddProfile(UserProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        dbContext.UserProfiles.Add(profile);
    }

    public void AddVehicle(UserVehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        dbContext.UserVehicles.Add(vehicle);
    }
    
    public void AddDeck(UserDeck deck)
    {
        ArgumentNullException.ThrowIfNull(deck);

        dbContext.UserDecks.Add(deck);
    }

    public void AddDeckSlot(UserDeckSlot deckSlot)
    {
        ArgumentNullException.ThrowIfNull(deckSlot);

        dbContext.UserDeckSlots.Add(deckSlot);
    }
    
    public void RemoveDeckSlot(UserDeckSlot deckSlot)
    {
        ArgumentNullException.ThrowIfNull(deckSlot);

        dbContext.UserDeckSlots.Remove(deckSlot);
    }
    
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
