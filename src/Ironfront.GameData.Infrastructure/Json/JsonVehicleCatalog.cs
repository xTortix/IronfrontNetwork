using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Ironfront.GameData.Application;
using Ironfront.GameData.Domain;

namespace Ironfront.GameData.Infrastructure.Json;

public sealed class JsonVehicleCatalogLoader : IVehicleCatalogLoader
{
    private static readonly Regex Sha256HashPattern = new(
        "^[a-f0-9]{64}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false
    };

    public IVehicleCatalog LoadFromFile(string catalogPath)
    {
        if (string.IsNullOrWhiteSpace(catalogPath))
        {
            throw new VehicleCatalogLoadException(
                catalogPath ?? string.Empty,
                new[] { "Catalog path is empty." });
        }

        string fullPath = Path.GetFullPath(catalogPath);

        if (!File.Exists(fullPath))
        {
            throw new VehicleCatalogLoadException(
                fullPath,
                new[] { "Catalog file does not exist." });
        }

        VehicleCatalogDocument? document;

        try
        {
            string json = File.ReadAllText(fullPath);

            document = JsonSerializer.Deserialize<VehicleCatalogDocument>(
                json,
                SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new VehicleCatalogLoadException(
                fullPath,
                new[] { $"Catalog JSON is invalid: {exception.Message}" });
        }
        catch (IOException exception)
        {
            throw new VehicleCatalogLoadException(
                fullPath,
                new[] { $"Catalog file could not be read: {exception.Message}" });
        }

        if (document is null)
        {
            throw new VehicleCatalogLoadException(
                fullPath,
                new[] { "Catalog JSON was empty." });
        }

        return BuildCatalog(fullPath, document);
    }

    private static IVehicleCatalog BuildCatalog(
        string catalogPath,
        VehicleCatalogDocument document)
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

        if (document.Vehicles is null || document.Vehicles.Count == 0)
            errors.Add("Catalog does not contain any vehicles.");

        List<VehicleCatalogEntry> vehicles = new();
        HashSet<string> vehicleIds = new(StringComparer.OrdinalIgnoreCase);

        foreach (VehicleCatalogVehicleDocument source in document.Vehicles ?? [])
        {
            string vehicleId = source.VehicleId?.Trim() ?? string.Empty;
            string displayName = source.DisplayName?.Trim() ?? string.Empty;
            string nation = source.Nation?.Trim() ?? string.Empty;
            string vehicleClass = source.VehicleClass?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(vehicleId))
            {
                errors.Add("A vehicle has an empty vehicleId.");
                continue;
            }

            if (!vehicleIds.Add(vehicleId))
            {
                errors.Add($"Duplicate vehicleId '{vehicleId}'.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(displayName))
                errors.Add($"Vehicle '{vehicleId}' has an empty displayName.");

            if (string.IsNullOrWhiteSpace(nation))
                errors.Add($"Vehicle '{vehicleId}' has an empty nation.");

            if (string.IsNullOrWhiteSpace(vehicleClass))
                errors.Add($"Vehicle '{vehicleId}' has an empty vehicleClass.");

            if (source.BattleRatingTenths == 0)
            {
                errors.Add(
                    $"Vehicle '{vehicleId}' has invalid battleRatingTenths '{source.BattleRatingTenths}'.");
            }

            vehicles.Add(new VehicleCatalogEntry(
                vehicleId,
                displayName,
                nation,
                vehicleClass,
                source.BattleRatingTenths));
        }

        if (errors.Count > 0)
            throw new VehicleCatalogLoadException(catalogPath, errors);

        try
        {
            return new VehicleCatalog(
                new GameDataManifest(
                    version,
                    contentHash,
                    vehicles.Count),
                vehicles);
        }
        catch (ArgumentException exception)
        {
            throw new VehicleCatalogLoadException(
                catalogPath,
                new[] { exception.Message });
        }
    }

    private sealed class VehicleCatalogDocument
    {
        [JsonPropertyName("version")]
        public string? Version { get; init; }

        [JsonPropertyName("contentHash")]
        public string? ContentHash { get; init; }

        [JsonPropertyName("vehicles")]
        public List<VehicleCatalogVehicleDocument>? Vehicles { get; init; }
    }

    private sealed class VehicleCatalogVehicleDocument
    {
        [JsonPropertyName("vehicleId")]
        public string? VehicleId { get; init; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; init; }

        [JsonPropertyName("nation")]
        public string? Nation { get; init; }

        [JsonPropertyName("vehicleClass")]
        public string? VehicleClass { get; init; }

        [JsonPropertyName("battleRatingTenths")]
        public byte BattleRatingTenths { get; init; }
    }
}