using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Ironfront.GameData.Application;
using Ironfront.GameData.Domain;

namespace Ironfront.GameData.Infrastructure.Json;

public sealed class JsonBattleModeCatalogLoader
    : IBattleModeCatalogLoader
{
    private static readonly Regex Sha256HashPattern =
        new(
            "^[a-f0-9]{64}$",
            RegexOptions.Compiled |
            RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions
        SerializerOptions =
            new()
            {
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };

    public IBattleModeCatalog LoadFromFile(
        string catalogPath)
    {
        if (string.IsNullOrWhiteSpace(catalogPath))
        {
            throw new BattleModeCatalogLoadException(
                catalogPath ?? string.Empty,
                new[]
                {
                    "Catalog path is empty."
                });
        }

        string fullPath =
            Path.GetFullPath(catalogPath);

        if (!File.Exists(fullPath))
        {
            throw new BattleModeCatalogLoadException(
                fullPath,
                new[]
                {
                    "Catalog file does not exist."
                });
        }

        BattleModeCatalogDocument? document;

        try
        {
            document =
                JsonSerializer.Deserialize<
                    BattleModeCatalogDocument>(
                    File.ReadAllText(fullPath),
                    SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new BattleModeCatalogLoadException(
                fullPath,
                new[]
                {
                    $"Catalog JSON is invalid: {exception.Message}"
                });
        }
        catch (IOException exception)
        {
            throw new BattleModeCatalogLoadException(
                fullPath,
                new[]
                {
                    $"Catalog file could not be read: {exception.Message}"
                });
        }

        if (document is null)
        {
            throw new BattleModeCatalogLoadException(
                fullPath,
                new[]
                {
                    "Catalog JSON was empty."
                });
        }

        return BuildCatalog(fullPath, document);
    }

    private static IBattleModeCatalog BuildCatalog(
        string catalogPath,
        BattleModeCatalogDocument document)
    {
        List<string> errors = new();

        string version =
            document.Version?.Trim() ?? string.Empty;

        string contentHash =
            document.ContentHash?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(version))
        {
            errors.Add("Catalog version is empty.");
        }

        if (!Sha256HashPattern.IsMatch(contentHash))
        {
            errors.Add(
                "Catalog contentHash must be a lowercase SHA-256 hash with 64 hexadecimal characters.");
        }

        if (document.Modes is null ||
            document.Modes.Count == 0)
        {
            errors.Add(
                "Catalog does not contain any battle modes.");
        }

        var modes = new List<BattleModeDefinition>();

        foreach (BattleModeDocument? source
                 in document.Modes ?? [])
        {
            if (source is null)
            {
                errors.Add(
                    "Catalog contains an empty battle mode entry.");

                continue;
            }

            if (source.Kind is null)
            {
                errors.Add(
                    $"Battle mode '{source.ModeId ?? "<unknown>"}' has no valid kind.");

                continue;
            }

            if (source.Availability is null)
            {
                errors.Add(
                    $"Battle mode '{source.ModeId ?? "<unknown>"}' has no availability.");

                continue;
            }

            if (source.VehicleRules is null)
            {
                errors.Add(
                    $"Battle mode '{source.ModeId ?? "<unknown>"}' has no vehicleRules.");

                continue;
            }

            if (source.MatchmakingRules is null)
            {
                errors.Add(
                    $"Battle mode '{source.ModeId ?? "<unknown>"}' has no matchmakingRules.");

                continue;
            }

            if (source.RewardRules is null ||
                source.RewardRules.ProgressionPolicy is null)
            {
                errors.Add(
                    $"Battle mode '{source.ModeId ?? "<unknown>"}' has no valid rewardRules.");

                continue;
            }

            BattleModeAvailability availability =
                new(
                    source.Availability.IsEnabled,
                    source.Availability.StartsAtUtc,
                    source.Availability.EndsAtUtc);

            BattleModeVehicleRules vehicleRules =
                new(
                    source.VehicleRules
                        .MinimumBattleRatingTenths,
                    source.VehicleRules
                        .MaximumBattleRatingTenths,
                    (source.VehicleRules
                        .AllowedVehicleClasses ?? [])
                        .Select(vehicleClass =>
                            vehicleClass?.Trim() ??
                            string.Empty)
                        .ToArray());

            BattleModeMatchmakingRules matchmakingRules =
                new(
                    source.MatchmakingRules
                        .InitialMaximumBattleRatingDifferenceTenths,
                    source.MatchmakingRules
                        .BattleRatingDifferenceExpansionTenths,
                    source.MatchmakingRules
                        .ExpansionIntervalSeconds,
                    source.MatchmakingRules
                        .MaximumBattleRatingDifferenceTenths);

            BattleModeRewardRules rewardRules =
                new(
                    source.RewardRules.ProgressionPolicy.Value,
                    source.RewardRules.CreditsMultiplier,
                    source.RewardRules.ExperienceMultiplier,
                    source.RewardRules.KillScoreMultiplier,
                    source.RewardRules.SpotScoreMultiplier,
                    source.RewardRules.CaptureScoreMultiplier);

            modes.Add(
                new BattleModeDefinition(
                    source.ModeId?.Trim() ??
                    string.Empty,

                    source.Revision,

                    source.Kind.Value,

                    source.DisplayName?.Trim() ??
                    string.Empty,

                    source.Description?.Trim() ??
                    string.Empty,

                    availability,

                    (source.MapPool ?? [])
                        .Select(map =>
                            new BattleModeMapPoolEntry(
                                map?.MapId?.Trim() ??
                                string.Empty,

                                map?.Weight ?? 0))
                        .ToArray(),

                        (source.Teams ?? [])
                        .Select((team, index) =>
                            new BattleModeTeamDefinition(
                                (byte)index,

                                team?.DisplayName?.Trim() ??
                                string.Empty,

                                team?.RequiredPlayerCount ?? 0))
                        .ToArray(),

                    vehicleRules,
                    matchmakingRules,
                    rewardRules));
        }

        if (errors.Count > 0)
        {
            throw new BattleModeCatalogLoadException(
                catalogPath,
                errors);
        }

        try
        {
            return new BattleModeCatalog(
                new BattleModeCatalogManifest(
                    version,
                    contentHash,
                    modes.Count),
                modes);
        }
        catch (ArgumentException exception)
        {
            throw new BattleModeCatalogLoadException(
                catalogPath,
                new[]
                {
                    exception.Message
                });
        }
    }

    private sealed class BattleModeCatalogDocument
    {
        public string? Version { get; init; }

        public string? ContentHash { get; init; }

        public List<BattleModeDocument?>? Modes { get; init; }
    }

    private sealed class BattleModeDocument
    {
        public string? ModeId { get; init; }

        public int Revision { get; init; }

        public BattleModeKind? Kind { get; init; }

        public string? DisplayName { get; init; }

        public string? Description { get; init; }

        public BattleModeAvailabilityDocument? Availability { get; init; }

        public List<BattleModeMapPoolEntryDocument?>? MapPool { get; init; }

        public List<BattleModeTeamDocument?>? Teams { get; init; }

        public BattleModeVehicleRulesDocument? VehicleRules { get; init; }

        public BattleModeMatchmakingRulesDocument? MatchmakingRules { get; init; }

        public BattleModeRewardRulesDocument? RewardRules { get; init; }
    }

    private sealed class BattleModeAvailabilityDocument
    {
        public bool IsEnabled { get; init; }

        public DateTime? StartsAtUtc { get; init; }

        public DateTime? EndsAtUtc { get; init; }
    }

    private sealed class BattleModeMapPoolEntryDocument
    {
        public string? MapId { get; init; }

        public int Weight { get; init; }
    }

    private sealed class BattleModeTeamDocument
    {
        public string? DisplayName { get; init; }

        public byte? RequiredPlayerCount { get; init; }
    }

    private sealed class BattleModeVehicleRulesDocument
    {
        public byte MinimumBattleRatingTenths { get; init; }

        public byte MaximumBattleRatingTenths { get; init; }

        public List<string?>? AllowedVehicleClasses { get; init; }
    }

    private sealed class BattleModeMatchmakingRulesDocument
    {
        public byte InitialMaximumBattleRatingDifferenceTenths { get; init; }

        public byte BattleRatingDifferenceExpansionTenths { get; init; }

        public int ExpansionIntervalSeconds { get; init; }

        public byte MaximumBattleRatingDifferenceTenths { get; init; }
    }

    private sealed class BattleModeRewardRulesDocument
    {
        public BattleModeProgressionPolicy? ProgressionPolicy { get; init; }

        public decimal CreditsMultiplier { get; init; }

        public decimal ExperienceMultiplier { get; init; }

        public decimal KillScoreMultiplier { get; init; }

        public decimal SpotScoreMultiplier { get; init; }

        public decimal CaptureScoreMultiplier { get; init; }
    }
}