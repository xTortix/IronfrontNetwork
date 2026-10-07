namespace Ironfront.GameData.Domain;

public sealed record BattleModeCatalogManifest(
    string Version,
    string ContentHash,
    int ModeCount);