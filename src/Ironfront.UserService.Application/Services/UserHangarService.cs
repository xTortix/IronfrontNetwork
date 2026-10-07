using Ironfront.GameData.Application;
using Ironfront.UserService.Application.Exceptions;
using Ironfront.UserService.Application.Models;
using Ironfront.UserService.Domain.Entities;
using Ironfront.UserService.Domain.Rules;

namespace Ironfront.UserService.Application.Services;

public sealed class UserHangarService : IUserHangarService
{
    private readonly IUserHangarRepository hangarRepository;
    private readonly IVehicleCatalog vehicleCatalog;

    public UserHangarService(
        IUserHangarRepository hangarRepository,
        IVehicleCatalog vehicleCatalog)
    {
        this.hangarRepository = hangarRepository;
        this.vehicleCatalog = vehicleCatalog;
    }

    // Neues, deck-basiertes Hangar-Modell.
    public async Task<UserHangarSnapshot> GetHangarAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(userId));
        }

        UserProfile? profile = await hangarRepository.FindProfileAsync(
            userId,
            cancellationToken);

        if (profile is null)
        {
            throw new UserHangarNotFoundException(userId);
        }

        IReadOnlyList<UserVehicle> ownedVehicles =
            await hangarRepository.GetOwnedVehiclesAsync(
                userId,
                cancellationToken);

        if (ownedVehicles.Count == 0)
        {
            throw new InvalidOperationException(
                $"User '{userId}' owns no vehicles.");
        }

        IReadOnlyList<UserDeck> decks =
            await hangarRepository.GetDecksAsync(
                userId,
                cancellationToken);

        if (decks.Count == 0)
        {
            throw new InvalidOperationException(
                $"User '{userId}' has no decks.");
        }

        UserDeck[] activeDecks = decks
            .Where(deck => deck.IsActive)
            .ToArray();

        if (activeDecks.Length != 1)
        {
            throw new InvalidOperationException(
                $"User '{userId}' must have exactly one active deck. Found: {activeDecks.Length}.");
        }

        UserDeck activeDeck = activeDecks[0];

        IReadOnlyList<UserDeckSlot> storedDeckSlots =
            await hangarRepository.GetDeckSlotsForUserAsync(
                userId,
                cancellationToken);

        var ownedVehicleInfos = new List<HangarVehicleInfo>(
            ownedVehicles.Count);

        var vehicleInfoById =
            new Dictionary<string, HangarVehicleInfo>(
                StringComparer.Ordinal);

        foreach (UserVehicle ownedVehicle in ownedVehicles)
        {
            if (!vehicleCatalog.TryGetVehicle(
                    ownedVehicle.VehicleId,
                    out var catalogVehicle)
                || catalogVehicle is null)
            {
                throw new InvalidOperationException(
                    $"Owned vehicle '{ownedVehicle.VehicleId}' does not exist in the active vehicle catalog.");
            }

            var vehicleInfo = new HangarVehicleInfo(
                catalogVehicle.VehicleId,
                catalogVehicle.DisplayName,
                catalogVehicle.Nation,
                catalogVehicle.VehicleClass,
                catalogVehicle.BattleRatingTenths);

            if (!vehicleInfoById.TryAdd(
                    vehicleInfo.VehicleId,
                    vehicleInfo))
            {
                throw new InvalidOperationException(
                    $"User '{userId}' owns duplicate vehicle '{vehicleInfo.VehicleId}'.");
            }

            ownedVehicleInfos.Add(vehicleInfo);
        }

        var storedSlotsByDeckId = storedDeckSlots
            .GroupBy(slot => slot.DeckId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        var hangarDecks = new List<HangarDeck>(decks.Count);

        foreach (UserDeck deck in decks)
        {
            if (!storedSlotsByDeckId.TryGetValue(
                    deck.DeckId,
                    out List<UserDeckSlot>? deckStoredSlots))
            {
                deckStoredSlots = new List<UserDeckSlot>();
            }

            var slotByIndex = new Dictionary<int, UserDeckSlot>();
            var vehicleIdsInDeck = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (UserDeckSlot storedSlot in deckStoredSlots)
            {
                if (!DeckRules.IsValidSlotIndex(
                        storedSlot.SlotIndex))
                {
                    throw new InvalidOperationException(
                        $"Deck '{deck.DeckId}' contains invalid slot index '{storedSlot.SlotIndex}'.");
                }

                if (!slotByIndex.TryAdd(
                        storedSlot.SlotIndex,
                        storedSlot))
                {
                    throw new InvalidOperationException(
                        $"Deck '{deck.DeckId}' contains duplicate slot index '{storedSlot.SlotIndex}'.");
                }

                if (!vehicleIdsInDeck.Add(
                        storedSlot.VehicleId))
                {
                    throw new InvalidOperationException(
                        $"Deck '{deck.DeckId}' contains vehicle '{storedSlot.VehicleId}' more than once.");
                }

                if (!vehicleInfoById.TryGetValue(
                        storedSlot.VehicleId,
                        out HangarVehicleInfo? vehicleInfo))
                {
                    throw new InvalidOperationException(
                        $"Deck '{deck.DeckId}' contains vehicle '{storedSlot.VehicleId}', but the user does not own it.");
                }

                if (!string.Equals(
                        vehicleInfo.Nation,
                        deck.Nation,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Vehicle '{storedSlot.VehicleId}' does not match nation '{deck.Nation}' of deck '{deck.DeckId}'.");
                }
            }

            var displaySlots = new List<HangarDeckSlot>(
                DeckRules.MaxSlotCount);

            for (int slotIndex = 0;
                 slotIndex < DeckRules.MaxSlotCount;
                 slotIndex++)
            {
                if (!slotByIndex.TryGetValue(
                        slotIndex,
                        out UserDeckSlot? storedSlot))
                {
                    displaySlots.Add(new HangarDeckSlot(
                        slotIndex,
                        null));

                    continue;
                }

                HangarVehicleInfo vehicleInfo =
                    vehicleInfoById[storedSlot.VehicleId];

                displaySlots.Add(new HangarDeckSlot(
                    slotIndex,
                    vehicleInfo));
            }

            byte[] battleRatingTenths = displaySlots
                .Where(slot => slot.Vehicle is not null)
                .Select(slot => slot.Vehicle!.BattleRatingTenths)
                .ToArray();

            byte? deckBattleRatingTenths =
                battleRatingTenths.Length == 0
                    ? null
                    : battleRatingTenths.Max();

            hangarDecks.Add(new HangarDeck(
                deck.DeckId,
                deck.Name,
                deck.Nation,
                deck.IsActive,
                deckBattleRatingTenths,
                displaySlots));
        }

        return new UserHangarSnapshot(
            profile.UserId,
            activeDeck.DeckId,
            ownedVehicleInfos,
            hangarDecks);
    }
}