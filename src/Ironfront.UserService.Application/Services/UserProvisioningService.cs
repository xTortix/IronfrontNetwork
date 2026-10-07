using Ironfront.GameData.Application;
using Ironfront.GameData.Domain;
using Ironfront.UserService.Application.Models;
using Ironfront.UserService.Domain.Entities;

namespace Ironfront.UserService.Application.Services;

public sealed class UserProvisioningService : IUserProvisioningService
{
    private readonly IUserHangarRepository hangarRepository;
    private readonly IVehicleCatalog vehicleCatalog;
    private readonly StarterVehicleGrant starterVehicleGrant;
    private readonly StarterDeckGrant starterDeckGrant;
    private readonly TimeProvider timeProvider;

    public UserProvisioningService(
        IUserHangarRepository hangarRepository,
        IVehicleCatalog vehicleCatalog,
        StarterVehicleGrant starterVehicleGrant,
        StarterDeckGrant starterDeckGrant,
        TimeProvider timeProvider)
    {
        this.hangarRepository = hangarRepository;
        this.vehicleCatalog = vehicleCatalog;
        this.starterVehicleGrant = starterVehicleGrant;
        this.starterDeckGrant = starterDeckGrant;
        this.timeProvider = timeProvider;
    }

    public async Task<ProvisionedUserProfile> ProvisionAsync(
        ProvisionUserCommand command,
        CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        VehicleCatalogEntry starterVehicle =
            GetValidatedStarterVehicle();

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        UserProfile? existingProfile =
            await hangarRepository.FindProfileAsync(
                command.UserId,
                cancellationToken);

        if (existingProfile is not null)
        {
            UserDeck? activeDeck =
                await hangarRepository.FindActiveDeckAsync(
                    command.UserId,
                    cancellationToken);

            if (activeDeck is null)
            {
                throw new InvalidOperationException(
                    $"User '{command.UserId}' has a profile but no active deck.");
            }

            bool identityChanged = existingProfile.SynchronizeIdentity(
                command.Username,
                command.Email,
                utcNow);

            if (identityChanged)
            {
                await hangarRepository.SaveChangesAsync(
                    cancellationToken);
            }

            return ToResult(
                existingProfile,
                activeDeck,
                wasCreated: false);
        }

        UserProfile profile = UserProfile.Create(
            command.UserId,
            command.Username,
            command.Email,
            utcNow);

        UserVehicle starterVehicleOwnership = UserVehicle.Create(
            command.UserId,
            starterVehicle.VehicleId,
            utcNow);

        UserDeck starterDeck = UserDeck.Create(
            command.UserId,
            starterDeckGrant.Name,
            starterVehicle.Nation,
            isActive: true,
            utcNow);

        UserDeckSlot starterDeckSlot = UserDeckSlot.Create(
            starterDeck.DeckId,
            slotIndex: 0,
            starterVehicle.VehicleId,
            utcNow);

        hangarRepository.AddProfile(profile);
        hangarRepository.AddVehicle(starterVehicleOwnership);
        hangarRepository.AddDeck(starterDeck);
        hangarRepository.AddDeckSlot(starterDeckSlot);

        await hangarRepository.SaveChangesAsync(cancellationToken);

        return ToResult(
            profile,
            starterDeck,
            wasCreated: true);
    }

    private VehicleCatalogEntry GetValidatedStarterVehicle()
    {
        string vehicleId = starterVehicleGrant.VehicleId?.Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(vehicleId))
        {
            throw new UserProvisioningConfigurationException(
                "No default starter vehicle is configured.");
        }

        if (!vehicleCatalog.TryGetVehicle(
                vehicleId,
                out VehicleCatalogEntry? catalogVehicle)
            || catalogVehicle is null)
        {
            throw new UserProvisioningConfigurationException(
                $"Configured starter vehicle '{vehicleId}' does not exist in the active vehicle catalog.");
        }

        return catalogVehicle;
    }

    private static void ValidateCommand(
        ProvisionUserCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(command));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(command.Username);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Email);
    }

    private static ProvisionedUserProfile ToResult(
        UserProfile profile,
        UserDeck activeDeck,
        bool wasCreated)
    {
        return new ProvisionedUserProfile(
            profile.UserId,
            profile.Username,
            profile.Email,
            activeDeck.DeckId,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            wasCreated);
    }
}