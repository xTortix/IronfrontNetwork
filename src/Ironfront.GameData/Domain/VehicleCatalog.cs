using System.Collections.ObjectModel;
using Ironfront.GameData.Application;
using System.Diagnostics.CodeAnalysis;
namespace Ironfront.GameData.Domain;

public sealed class VehicleCatalog : IVehicleCatalog
{
    private readonly IReadOnlyList<VehicleCatalogEntry> vehicles;
    private readonly IReadOnlyDictionary<string, VehicleCatalogEntry> vehiclesById;

    public GameDataManifest Manifest { get; }

    public IReadOnlyList<VehicleCatalogEntry> Vehicles => vehicles;

    public VehicleCatalog(
        GameDataManifest manifest,
        IEnumerable<VehicleCatalogEntry> vehicleEntries)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(vehicleEntries);

        if (string.IsNullOrWhiteSpace(manifest.Version))
            throw new ArgumentException("Catalog version cannot be empty.", nameof(manifest));

        if (string.IsNullOrWhiteSpace(manifest.ContentHash))
            throw new ArgumentException("Catalog content hash cannot be empty.", nameof(manifest));

        List<VehicleCatalogEntry> sortedVehicles = vehicleEntries
            .OrderBy(x => x.VehicleId, StringComparer.Ordinal)
            .ToList();

        if (sortedVehicles.Count == 0)
            throw new ArgumentException("Vehicle catalog cannot be empty.", nameof(vehicleEntries));

        Dictionary<string, VehicleCatalogEntry> lookup =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (VehicleCatalogEntry vehicle in sortedVehicles)
        {
            ValidateVehicle(vehicle);

            if (!lookup.TryAdd(vehicle.VehicleId, vehicle))
            {
                throw new ArgumentException(
                    $"Duplicate vehicle id '{vehicle.VehicleId}'.",
                    nameof(vehicleEntries));
            }
        }

        Manifest = manifest with
        {
            VehicleCount = sortedVehicles.Count
        };

        vehicles = new ReadOnlyCollection<VehicleCatalogEntry>(sortedVehicles);
        vehiclesById =
            new ReadOnlyDictionary<string, VehicleCatalogEntry>(lookup);
    }

    public bool TryGetVehicle(
        string vehicleId,
        [NotNullWhen(true)] out VehicleCatalogEntry? vehicle)
    {
        vehicle = null;

        if (string.IsNullOrWhiteSpace(vehicleId))
            return false;

        return vehiclesById.TryGetValue(vehicleId.Trim(), out vehicle);
    }

    private static void ValidateVehicle(VehicleCatalogEntry vehicle)
    {
        if (string.IsNullOrWhiteSpace(vehicle.VehicleId))
            throw new ArgumentException("Vehicle id cannot be empty.");

        if (string.IsNullOrWhiteSpace(vehicle.DisplayName))
            throw new ArgumentException(
                $"Vehicle '{vehicle.VehicleId}' has no display name.");

        if (string.IsNullOrWhiteSpace(vehicle.Nation))
            throw new ArgumentException(
                $"Vehicle '{vehicle.VehicleId}' has no nation.");

        if (string.IsNullOrWhiteSpace(vehicle.VehicleClass))
            throw new ArgumentException(
                $"Vehicle '{vehicle.VehicleId}' has no vehicle class.");

        if (vehicle.BattleRatingTenths == 0)
        {
            throw new ArgumentException(
                $"Vehicle '{vehicle.VehicleId}' has an invalid battle rating.");
        }
    }
}