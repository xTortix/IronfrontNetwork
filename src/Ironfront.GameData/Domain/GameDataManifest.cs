namespace Ironfront.GameData.Domain;

public sealed record GameDataManifest(
    string Version,
    string ContentHash,
    int VehicleCount);