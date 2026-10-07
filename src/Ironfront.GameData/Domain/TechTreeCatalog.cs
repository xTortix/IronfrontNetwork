using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Ironfront.GameData.Application;

namespace Ironfront.GameData.Domain;

public sealed class TechTreeCatalog : ITechTreeCatalog
{
    private readonly IReadOnlyList<TechTreeCatalogEntry> techTrees;
    private readonly IReadOnlyDictionary<string, TechTreeCatalogEntry> techTreesById;
    private readonly IReadOnlyDictionary<string, TechTreeNodeCatalogEntry> nodesById;

    public TechTreeCatalogManifest Manifest { get; }

    public IReadOnlyList<TechTreeCatalogEntry> TechTrees => techTrees;

    public TechTreeCatalog(
        TechTreeCatalogManifest manifest,
        IEnumerable<TechTreeCatalogEntry> techTreeEntries)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(techTreeEntries);

        ArgumentException.ThrowIfNullOrWhiteSpace(manifest.Version);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifest.ContentHash);

        List<TechTreeCatalogEntry> sortedTrees = techTreeEntries
            .OrderBy(tree => tree.TechTreeId, StringComparer.Ordinal)
            .ToList();

        if (sortedTrees.Count == 0)
        {
            throw new ArgumentException(
                "Tech tree catalog cannot be empty.",
                nameof(techTreeEntries));
        }

        Dictionary<string, TechTreeCatalogEntry> treeLookup =
            new(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, TechTreeNodeCatalogEntry> nodeLookup =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (TechTreeCatalogEntry tree in sortedTrees)
        {
            if (!treeLookup.TryAdd(tree.TechTreeId, tree))
            {
                throw new ArgumentException(
                    $"Duplicate tech tree id '{tree.TechTreeId}'.",
                    nameof(techTreeEntries));
            }

            foreach (TechTreeNodeCatalogEntry node in tree.Nodes)
            {
                if (!nodeLookup.TryAdd(node.NodeId, node))
                {
                    throw new ArgumentException(
                        $"Duplicate tech tree node id '{node.NodeId}'.",
                        nameof(techTreeEntries));
                }
            }
        }

        Manifest = manifest with
        {
            TechTreeCount = sortedTrees.Count,
            NodeCount = nodeLookup.Count
        };

        techTrees = new ReadOnlyCollection<TechTreeCatalogEntry>(
            sortedTrees);

        techTreesById =
            new ReadOnlyDictionary<string, TechTreeCatalogEntry>(
                treeLookup);

        nodesById =
            new ReadOnlyDictionary<string, TechTreeNodeCatalogEntry>(
                nodeLookup);
    }

    public bool TryGetTechTree(
        string techTreeId,
        [NotNullWhen(true)] out TechTreeCatalogEntry? techTree)
    {
        techTree = null;

        return !string.IsNullOrWhiteSpace(techTreeId)
               && techTreesById.TryGetValue(
                   techTreeId.Trim(),
                   out techTree);
    }

    public bool TryGetNode(
        string nodeId,
        [NotNullWhen(true)] out TechTreeNodeCatalogEntry? node)
    {
        node = null;

        return !string.IsNullOrWhiteSpace(nodeId)
               && nodesById.TryGetValue(
                   nodeId.Trim(),
                   out node);
    }
}