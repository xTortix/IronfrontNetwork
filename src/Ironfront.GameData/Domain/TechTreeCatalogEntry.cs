using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Ironfront.GameData.Domain;

public sealed class TechTreeCatalogEntry
{
    private readonly IReadOnlyList<TechTreeNodeCatalogEntry> nodes;
    private readonly IReadOnlyDictionary<string, TechTreeNodeCatalogEntry> nodesById;

    public string TechTreeId { get; }
    public string Nation { get; }
    public string Branch { get; }

    public IReadOnlyList<TechTreeNodeCatalogEntry> Nodes => nodes;

    public TechTreeCatalogEntry(
        string techTreeId,
        string nation,
        string branch,
        IEnumerable<TechTreeNodeCatalogEntry> nodeEntries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(techTreeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nation);
        ArgumentException.ThrowIfNullOrWhiteSpace(branch);
        ArgumentNullException.ThrowIfNull(nodeEntries);

        TechTreeId = techTreeId;
        Nation = nation;
        Branch = branch;

        List<TechTreeNodeCatalogEntry> sortedNodes = nodeEntries
            .OrderBy(node => node.Rank)
            .ThenBy(node => node.NodeId, StringComparer.Ordinal)
            .ToList();

        Dictionary<string, TechTreeNodeCatalogEntry> lookup =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (TechTreeNodeCatalogEntry node in sortedNodes)
        {
            if (!lookup.TryAdd(node.NodeId, node))
            {
                throw new ArgumentException(
                    $"Tech tree '{TechTreeId}' contains duplicate node id '{node.NodeId}'.",
                    nameof(nodeEntries));
            }
        }

        nodes = new ReadOnlyCollection<TechTreeNodeCatalogEntry>(
            sortedNodes);

        nodesById =
            new ReadOnlyDictionary<string, TechTreeNodeCatalogEntry>(
                lookup);
    }

    public bool TryGetNode(
        string nodeId,
        [NotNullWhen(true)] out TechTreeNodeCatalogEntry? node)
    {
        node = null;

        return !string.IsNullOrWhiteSpace(nodeId)
               && nodesById.TryGetValue(nodeId.Trim(), out node);
    }
}