using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Ironfront.GameData.Application;
using Ironfront.GameData.Domain;

namespace Ironfront.GameData.Infrastructure.Json;

public sealed class JsonTechTreeCatalogLoader : ITechTreeCatalogLoader
{
    private static readonly Regex Sha256HashPattern = new(
        "^[a-f0-9]{64}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false
    };

    public ITechTreeCatalog LoadFromFile(
        string catalogPath,
        IVehicleCatalog vehicleCatalog)
    {
        ArgumentNullException.ThrowIfNull(vehicleCatalog);

        if (string.IsNullOrWhiteSpace(catalogPath))
        {
            throw new TechTreeCatalogLoadException(
                catalogPath ?? string.Empty,
                new[] { "Catalog path is empty." });
        }

        string fullPath = Path.GetFullPath(catalogPath);

        if (!File.Exists(fullPath))
        {
            throw new TechTreeCatalogLoadException(
                fullPath,
                new[] { "Catalog file does not exist." });
        }

        TechTreeCatalogDocument? document;

        try
        {
            string json = File.ReadAllText(fullPath);

            document = JsonSerializer.Deserialize<TechTreeCatalogDocument>(
                json,
                SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new TechTreeCatalogLoadException(
                fullPath,
                new[]
                {
                    $"Catalog JSON is invalid: {exception.Message}"
                });
        }
        catch (IOException exception)
        {
            throw new TechTreeCatalogLoadException(
                fullPath,
                new[]
                {
                    $"Catalog file could not be read: {exception.Message}"
                });
        }

        if (document is null)
        {
            throw new TechTreeCatalogLoadException(
                fullPath,
                new[] { "Catalog JSON was empty." });
        }

        return BuildCatalog(
            fullPath,
            document,
            vehicleCatalog);
    }

    private static ITechTreeCatalog BuildCatalog(
        string catalogPath,
        TechTreeCatalogDocument document,
        IVehicleCatalog vehicleCatalog)
    {
        List<string> errors = new();

        string version = document.Version?.Trim() ?? string.Empty;
        string contentHash = document.ContentHash?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(version))
            errors.Add("Catalog version is empty.");

        if (!Sha256HashPattern.IsMatch(contentHash))
        {
            errors.Add(
                "Catalog contentHash must be a lowercase SHA-256 hash with 64 hexadecimal characters.");
        }

        if (document.TechTrees is null || document.TechTrees.Count == 0)
            errors.Add("Catalog does not contain any tech trees.");

        List<TechTreeCatalogEntry> techTrees = new();

        HashSet<string> techTreeIds =
            new(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, string> nodeOwnerById =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (TechTreeDocument sourceTree in document.TechTrees ?? [])
        {
            string techTreeId =
                sourceTree.TechTreeId?.Trim() ?? string.Empty;

            string nation =
                sourceTree.Nation?.Trim() ?? string.Empty;

            string branch =
                sourceTree.Branch?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(techTreeId))
            {
                errors.Add("A tech tree has an empty techTreeId.");
                continue;
            }

            if (!techTreeIds.Add(techTreeId))
            {
                errors.Add($"Duplicate techTreeId '{techTreeId}'.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(nation))
            {
                errors.Add(
                    $"Tech tree '{techTreeId}' has an empty nation.");
            }

            if (string.IsNullOrWhiteSpace(branch))
            {
                errors.Add(
                    $"Tech tree '{techTreeId}' has an empty branch.");
            }

            if (sourceTree.Nodes is null || sourceTree.Nodes.Count == 0)
            {
                errors.Add(
                    $"Tech tree '{techTreeId}' does not contain any nodes.");
            }

            List<TechTreeNodeCatalogEntry> nodes = new();

            HashSet<string> nodeIdsInTree =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (TechTreeNodeDocument sourceNode in sourceTree.Nodes ?? [])
            {
                string nodeId =
                    sourceNode.NodeId?.Trim() ?? string.Empty;

                string vehicleId =
                    sourceNode.VehicleId?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(nodeId))
                {
                    errors.Add(
                        $"Tech tree '{techTreeId}' contains a node with an empty nodeId.");

                    continue;
                }

                if (!nodeIdsInTree.Add(nodeId))
                {
                    errors.Add(
                        $"Tech tree '{techTreeId}' contains duplicate nodeId '{nodeId}'.");

                    continue;
                }

                if (nodeOwnerById.TryGetValue(
                        nodeId,
                        out string? ownerTreeId))
                {
                    errors.Add(
                        $"NodeId '{nodeId}' exists in both tech trees '{ownerTreeId}' and '{techTreeId}'.");
                }
                else
                {
                    nodeOwnerById.Add(nodeId, techTreeId);
                }

                if (string.IsNullOrWhiteSpace(vehicleId))
                {
                    errors.Add(
                        $"Node '{nodeId}' has an empty vehicleId.");
                }
                else if (!vehicleCatalog.TryGetVehicle(
                             vehicleId,
                             out VehicleCatalogEntry? vehicle))
                {
                    errors.Add(
                        $"Node '{nodeId}' references unknown vehicleId '{vehicleId}'.");
                }
                else if (!string.Equals(
                             vehicle.Nation,
                             nation,
                             StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(
                        $"Node '{nodeId}' belongs to nation '{nation}', but vehicle '{vehicleId}' belongs to nation '{vehicle.Nation}'.");
                }

                if (sourceNode.Rank < 1)
                {
                    errors.Add(
                        $"Node '{nodeId}' has invalid rank '{sourceNode.Rank}'.");
                }

                if (sourceNode.ResearchPointCost < 0)
                {
                    errors.Add(
                        $"Node '{nodeId}' has negative researchPointCost '{sourceNode.ResearchPointCost}'.");
                }

                if (sourceNode.PurchaseCost < 0)
                {
                    errors.Add(
                        $"Node '{nodeId}' has negative purchaseCost '{sourceNode.PurchaseCost}'.");
                }

                List<string> requiredNodeIds = new();

                HashSet<string> requiredNodeIdSet =
                    new(StringComparer.OrdinalIgnoreCase);

                foreach (string? rawRequiredNodeId
                         in sourceNode.RequiredNodeIds ?? [])
                {
                    string requiredNodeId =
                        rawRequiredNodeId?.Trim() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(requiredNodeId))
                    {
                        errors.Add(
                            $"Node '{nodeId}' contains an empty requiredNodeId.");

                        continue;
                    }

                    if (!requiredNodeIdSet.Add(requiredNodeId))
                    {
                        errors.Add(
                            $"Node '{nodeId}' lists requiredNodeId '{requiredNodeId}' more than once.");

                        continue;
                    }

                    requiredNodeIds.Add(requiredNodeId);
                }

                nodes.Add(new TechTreeNodeCatalogEntry(
                    nodeId,
                    vehicleId,
                    sourceNode.Rank,
                    sourceNode.ResearchPointCost,
                    sourceNode.PurchaseCost,
                    requiredNodeIds.AsReadOnly()));
            }

            techTrees.Add(new TechTreeCatalogEntry(
                techTreeId,
                nation,
                branch,
                nodes));
        }

        ValidatePrerequisites(techTrees, errors);

        if (errors.Count > 0)
        {
            throw new TechTreeCatalogLoadException(
                catalogPath,
                errors);
        }

        try
        {
            return new TechTreeCatalog(
                new TechTreeCatalogManifest(
                    version,
                    contentHash,
                    techTrees.Count,
                    techTrees.Sum(tree => tree.Nodes.Count)),
                techTrees);
        }
        catch (ArgumentException exception)
        {
            throw new TechTreeCatalogLoadException(
                catalogPath,
                new[] { exception.Message });
        }
    }

    private static void ValidatePrerequisites(
        IReadOnlyList<TechTreeCatalogEntry> techTrees,
        ICollection<string> errors)
    {
        foreach (TechTreeCatalogEntry techTree in techTrees)
        {
            Dictionary<string, TechTreeNodeCatalogEntry> nodesById =
                techTree.Nodes.ToDictionary(
                    node => node.NodeId,
                    StringComparer.OrdinalIgnoreCase);

            foreach (TechTreeNodeCatalogEntry node in techTree.Nodes)
            {
                foreach (string requiredNodeId in node.RequiredNodeIds)
                {
                    if (string.Equals(
                            node.NodeId,
                            requiredNodeId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(
                            $"Node '{node.NodeId}' cannot require itself.");

                        continue;
                    }

                    if (!nodesById.ContainsKey(requiredNodeId))
                    {
                        errors.Add(
                            $"Node '{node.NodeId}' in tech tree '{techTree.TechTreeId}' requires '{requiredNodeId}', but that node does not exist in the same tech tree.");
                    }
                }
            }

            ValidateAcyclicPrerequisites(
                techTree,
                nodesById,
                errors);
        }
    }

    private static void ValidateAcyclicPrerequisites(
        TechTreeCatalogEntry techTree,
        IReadOnlyDictionary<string, TechTreeNodeCatalogEntry> nodesById,
        ICollection<string> errors)
    {
        Dictionary<string, VisitState> states =
            new(StringComparer.OrdinalIgnoreCase);

        HashSet<string> reportedCycles =
            new(StringComparer.OrdinalIgnoreCase);

        List<string> path = new();

        foreach (TechTreeNodeCatalogEntry node in techTree.Nodes)
        {
            Visit(node);
        }

        void Visit(TechTreeNodeCatalogEntry node)
        {
            if (states.TryGetValue(node.NodeId, out VisitState state))
            {
                if (state == VisitState.Visited)
                    return;

                if (state == VisitState.Visiting)
                {
                    int cycleStartIndex = path.FindIndex(nodeId =>
                        string.Equals(
                            nodeId,
                            node.NodeId,
                            StringComparison.OrdinalIgnoreCase));

                    IEnumerable<string> cycleNodes =
                        cycleStartIndex >= 0
                            ? path.Skip(cycleStartIndex).Append(node.NodeId)
                            : new[] { node.NodeId };

                    string cycle = string.Join(
                        " -> ",
                        cycleNodes);

                    if (reportedCycles.Add(cycle))
                    {
                        errors.Add(
                            $"Tech tree '{techTree.TechTreeId}' contains a circular prerequisite: {cycle}.");
                    }
                }

                return;
            }

            states[node.NodeId] = VisitState.Visiting;
            path.Add(node.NodeId);

            foreach (string requiredNodeId in node.RequiredNodeIds)
            {
                if (nodesById.TryGetValue(
                        requiredNodeId,
                        out TechTreeNodeCatalogEntry? requiredNode))
                {
                    Visit(requiredNode);
                }
            }

            path.RemoveAt(path.Count - 1);
            states[node.NodeId] = VisitState.Visited;
        }
    }

    private enum VisitState
    {
        Visiting,
        Visited
    }

    private sealed class TechTreeCatalogDocument
    {
        [JsonPropertyName("version")]
        public string? Version { get; init; }

        [JsonPropertyName("contentHash")]
        public string? ContentHash { get; init; }

        [JsonPropertyName("techTrees")]
        public List<TechTreeDocument>? TechTrees { get; init; }
    }

    private sealed class TechTreeDocument
    {
        [JsonPropertyName("techTreeId")]
        public string? TechTreeId { get; init; }

        [JsonPropertyName("nation")]
        public string? Nation { get; init; }

        [JsonPropertyName("branch")]
        public string? Branch { get; init; }

        [JsonPropertyName("nodes")]
        public List<TechTreeNodeDocument>? Nodes { get; init; }
    }

    private sealed class TechTreeNodeDocument
    {
        [JsonPropertyName("nodeId")]
        public string? NodeId { get; init; }

        [JsonPropertyName("vehicleId")]
        public string? VehicleId { get; init; }

        [JsonPropertyName("rank")]
        public int Rank { get; init; }

        [JsonPropertyName("researchPointCost")]
        public long ResearchPointCost { get; init; }

        [JsonPropertyName("purchaseCost")]
        public long PurchaseCost { get; init; }

        [JsonPropertyName("requiredNodeIds")]
        public List<string?>? RequiredNodeIds { get; init; }
    }
}