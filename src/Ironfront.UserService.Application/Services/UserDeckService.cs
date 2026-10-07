using Ironfront.GameData.Application;
using Ironfront.GameData.Domain;
using Ironfront.UserService.Application.Exceptions;
using Ironfront.UserService.Application.Models;
using Ironfront.UserService.Domain.Entities;
using Ironfront.UserService.Domain.Rules;



namespace Ironfront.UserService.Application.Services;

public sealed class UserDeckService : IUserDeckService
{
    private readonly IUserHangarRepository hangarRepository;
    private readonly IVehicleCatalog vehicleCatalog;
    private readonly TimeProvider timeProvider;

    public UserDeckService(
        IUserHangarRepository hangarRepository,
        IVehicleCatalog vehicleCatalog,
        TimeProvider timeProvider)
    {
        this.hangarRepository = hangarRepository;
        this.vehicleCatalog = vehicleCatalog;
        this.timeProvider = timeProvider;
    }

    public async Task<UserDeckCreationResult> CreateDeckAsync(
        CreateUserDeckCommand command,
        CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        bool userExists = await UserExistsAsync(
            command.UserId,
            cancellationToken);

        if (!userExists)
        {
            throw new UserProfileNotFoundException(command.UserId);
        }

        string canonicalNation = GetCanonicalNation(command.Nation);

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        UserDeck deck = UserDeck.Create(
            command.UserId,
            command.Name,
            canonicalNation,
            isActive: false,
            utcNow);

        hangarRepository.AddDeck(deck);

        await hangarRepository.SaveChangesAsync(cancellationToken);

        return new UserDeckCreationResult(
            deck.DeckId,
            deck.UserId,
            deck.Name,
            deck.Nation,
            deck.IsActive,
            deck.CreatedAtUtc,
            deck.UpdatedAtUtc);
    }
    
    public async Task<UserDeckActivationResult> ActivateDeckAsync(
        ActivateUserDeckCommand command,
        CancellationToken cancellationToken)
    {
        ValidateActivationCommand(command);

        UserProfile? profile = await hangarRepository.FindProfileAsync(
            command.UserId,
            cancellationToken);

        if (profile is null)
        {
            throw new UserProfileNotFoundException(command.UserId);
        }

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        UserDeck? activeDeck =
            await hangarRepository.SetActiveDeckAsync(
                command.UserId,
                command.DeckId,
                utcNow,
                cancellationToken);

        if (activeDeck is null)
        {
            throw new UserDeckNotFoundException(
                command.UserId,
                command.DeckId);
        }

        return new UserDeckActivationResult(
            activeDeck.DeckId,
            activeDeck.UserId,
            activeDeck.Name,
            activeDeck.Nation,
            activeDeck.IsActive,
            activeDeck.CreatedAtUtc,
            activeDeck.UpdatedAtUtc);
    }
    
    public async Task<UserDeckSlotAssignmentResult> AssignVehicleToDeckSlotAsync(
    AssignUserDeckVehicleCommand command,
    CancellationToken cancellationToken)
    {
        ValidateSlotAssignmentCommand(command);

        UserProfile? profile = await hangarRepository.FindProfileAsync(
            command.UserId,
            cancellationToken);

        if (profile is null)
        {
            throw new UserProfileNotFoundException(command.UserId);
        }

        UserDeck? deck = await hangarRepository.FindDeckAsync(
            command.UserId,
            command.DeckId,
            cancellationToken);

        if (deck is null)
        {
            throw new UserDeckNotFoundException(
                command.UserId,
                command.DeckId);
        }

        string requestedVehicleId = command.VehicleId.Trim();

        if (!vehicleCatalog.TryGetVehicle(
                requestedVehicleId,
                out var catalogVehicle)
            || catalogVehicle is null)
        {
            throw new ArgumentException(
                $"Vehicle '{requestedVehicleId}' does not exist in the active vehicle catalog.",
                nameof(command));
        }

        if (!string.Equals(
                deck.Nation,
                catalogVehicle.Nation,
                StringComparison.Ordinal))
        {
            throw new UserDeckVehicleNationMismatchException(
                deck.DeckId,
                deck.Nation,
                catalogVehicle.VehicleId,
                catalogVehicle.Nation);
        }

        bool ownsVehicle = await hangarRepository.UserOwnsVehicleAsync(
            command.UserId,
            catalogVehicle.VehicleId,
            cancellationToken);

        if (!ownsVehicle)
        {
            throw new UserVehicleNotOwnedException(
                command.UserId,
                catalogVehicle.VehicleId);
        }

        bool assignedToAnotherSlot =
            await hangarRepository.IsVehicleAssignedToAnotherSlotAsync(
                deck.DeckId,
                catalogVehicle.VehicleId,
                command.SlotIndex,
                cancellationToken);

        if (assignedToAnotherSlot)
        {
            throw new UserDeckVehicleAlreadyAssignedException(
                deck.DeckId,
                catalogVehicle.VehicleId);
        }

        UserDeckSlot? existingSlot =
            await hangarRepository.FindDeckSlotAsync(
                deck.DeckId,
                command.SlotIndex,
                cancellationToken);

        if (existingSlot is not null
            && string.Equals(
                existingSlot.VehicleId,
                catalogVehicle.VehicleId,
                StringComparison.Ordinal))
        {
            return new UserDeckSlotAssignmentResult(
                command.UserId,
                deck.DeckId,
                existingSlot.SlotIndex,
                existingSlot.VehicleId,
                existingSlot.AddedAtUtc);
        }

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        if (existingSlot is null)
        {
            existingSlot = UserDeckSlot.Create(
                deck.DeckId,
                command.SlotIndex,
                catalogVehicle.VehicleId,
                utcNow);

            hangarRepository.AddDeckSlot(existingSlot);
        }
        else
        {
            existingSlot.ReplaceVehicle(
                catalogVehicle.VehicleId,
                utcNow);
        }

        await hangarRepository.SaveChangesAsync(cancellationToken);

        return new UserDeckSlotAssignmentResult(
            command.UserId,
            deck.DeckId,
            existingSlot.SlotIndex,
            existingSlot.VehicleId,
            existingSlot.AddedAtUtc);
    }
    
    public async Task<UserDeckSlotClearResult> ClearDeckSlotAsync(
        ClearUserDeckSlotCommand command,
        CancellationToken cancellationToken)
    {
        ValidateSlotClearCommand(command);

        UserProfile? profile = await hangarRepository.FindProfileAsync(
            command.UserId,
            cancellationToken);

        if (profile is null)
        {
            throw new UserProfileNotFoundException(command.UserId);
        }

        UserDeck? deck = await hangarRepository.FindDeckAsync(
            command.UserId,
            command.DeckId,
            cancellationToken);

        if (deck is null)
        {
            throw new UserDeckNotFoundException(
                command.UserId,
                command.DeckId);
        }

        UserDeckSlot? existingSlot =
            await hangarRepository.FindDeckSlotAsync(
                deck.DeckId,
                command.SlotIndex,
                cancellationToken);

        if (existingSlot is null)
        {
            return new UserDeckSlotClearResult(
                command.UserId,
                deck.DeckId,
                command.SlotIndex,
                WasCleared: false,
                ClearedAtUtc: null);
        }

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        hangarRepository.RemoveDeckSlot(existingSlot);

        await hangarRepository.SaveChangesAsync(cancellationToken);

        return new UserDeckSlotClearResult(
            command.UserId,
            deck.DeckId,
            command.SlotIndex,
            WasCleared: true,
            ClearedAtUtc: utcNow);
    }
    
    private async Task<bool> UserExistsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        UserProfile? profile = await hangarRepository.FindProfileAsync(
            userId,
            cancellationToken);

        return profile is not null;
    }

    private string GetCanonicalNation(string requestedNation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedNation);

        string normalizedNation = requestedNation.Trim();

        VehicleCatalogEntry? catalogVehicle = vehicleCatalog.Vehicles
            .FirstOrDefault(vehicle =>
                string.Equals(
                    vehicle.Nation,
                    normalizedNation,
                    StringComparison.OrdinalIgnoreCase));

        if (catalogVehicle is null)
        {
            throw new ArgumentException(
                $"Nation '{normalizedNation}' does not exist in the active vehicle catalog.",
                nameof(requestedNation));
        }

        return catalogVehicle.Nation;
    }

    private static void ValidateCommand(
        CreateUserDeckCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(command));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Nation);
    }
    
    private static void ValidateActivationCommand(
        ActivateUserDeckCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(command));
        }

        if (command.DeckId == Guid.Empty)
        {
            throw new ArgumentException(
                "Deck ID must not be empty.",
                nameof(command));
        }
    }
    
    private static void ValidateSlotAssignmentCommand(
        AssignUserDeckVehicleCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(command));
        }

        if (command.DeckId == Guid.Empty)
        {
            throw new ArgumentException(
                "Deck ID must not be empty.",
                nameof(command));
        }

        if (!DeckRules.IsValidSlotIndex(command.SlotIndex))
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                command.SlotIndex,
                $"Slot index must be between 0 and {DeckRules.MaxSlotCount - 1}.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(command.VehicleId);
    }
    
    private static void ValidateSlotClearCommand(
        ClearUserDeckSlotCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(command));
        }

        if (command.DeckId == Guid.Empty)
        {
            throw new ArgumentException(
                "Deck ID must not be empty.",
                nameof(command));
        }

        if (!DeckRules.IsValidSlotIndex(command.SlotIndex))
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                command.SlotIndex,
                $"Slot index must be between 0 and {DeckRules.MaxSlotCount - 1}.");
        }
    }
}