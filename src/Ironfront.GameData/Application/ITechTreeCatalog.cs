using System.Diagnostics.CodeAnalysis;
using Ironfront.GameData.Domain;

namespace Ironfront.GameData.Application;

public interface ITechTreeCatalog
{
    TechTreeCatalogManifest Manifest { get; }

    IReadOnlyList<TechTreeCatalogEntry> TechTrees { get; }

    bool TryGetTechTree(
        string techTreeId,
        [NotNullWhen(true)] out TechTreeCatalogEntry? techTree);

    bool TryGetNode(
        string nodeId,
        [NotNullWhen(true)] out TechTreeNodeCatalogEntry? node);
}