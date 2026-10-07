using System.Diagnostics.CodeAnalysis;
using Ironfront.GameData.Domain;

namespace Ironfront.GameData.Application;

public interface IVehicleCatalog
{
    GameDataManifest Manifest { get; }

    IReadOnlyList<VehicleCatalogEntry> Vehicles { get; }

    bool TryGetVehicle(
        string vehicleId,
        [NotNullWhen(true)] out VehicleCatalogEntry? vehicle);
}