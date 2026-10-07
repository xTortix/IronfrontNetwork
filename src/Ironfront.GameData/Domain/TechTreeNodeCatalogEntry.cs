namespace Ironfront.GameData.Domain;

public sealed record TechTreeNodeCatalogEntry(
    string NodeId,
    string VehicleId,
    int Rank,
    long ResearchPointCost,
    long PurchaseCost,
    IReadOnlyList<string> RequiredNodeIds);