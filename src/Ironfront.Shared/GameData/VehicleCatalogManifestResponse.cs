namespace Ironfront.Shared.Contracts.GameData;

public sealed class VehicleCatalogManifestResponse
{
    public string Version { get; init; } = string.Empty;

    public string ContentHash { get; init; } = string.Empty;

    public int VehicleCount { get; init; }
}