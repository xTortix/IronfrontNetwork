namespace Ironfront.GameData.Domain;

public sealed record TechTreeCatalogManifest(
    string Version,
    string ContentHash,
    int TechTreeCount,
    int NodeCount);