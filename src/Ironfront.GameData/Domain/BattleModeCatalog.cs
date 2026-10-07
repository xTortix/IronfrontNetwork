using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Ironfront.GameData.Application;

namespace Ironfront.GameData.Domain;

public sealed class BattleModeCatalog : IBattleModeCatalog
{
    private const int MaximumModeIdLength = 64;
    private const int MaximumDisplayNameLength = 128;
    private const int MaximumDescriptionLength = 512;
    private const int MaximumMapIdLength = 64;
    private const int MaximumTeamDisplayNameLength = 64;
    private const int MaximumPlayersPerMatch = 128;

    private readonly IReadOnlyList<BattleModeDefinition> modes;
    private readonly IReadOnlyDictionary<string, BattleModeDefinition>
        modesById;

    public BattleModeCatalogManifest Manifest { get; }

    public IReadOnlyList<BattleModeDefinition> Modes =>
        modes;

    public BattleModeCatalog(
        BattleModeCatalogManifest manifest,
        IEnumerable<BattleModeDefinition> modeDefinitions)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(modeDefinitions);

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new ArgumentException(
                "Battle mode catalog version cannot be empty.",
                nameof(manifest));
        }

        if (string.IsNullOrWhiteSpace(manifest.ContentHash))
        {
            throw new ArgumentException(
                "Battle mode catalog content hash cannot be empty.",
                nameof(manifest));
        }

        List<BattleModeDefinition> sortedModes =
            modeDefinitions
                .OrderBy(mode => mode.ModeId, StringComparer.Ordinal)
                .ToList();

        if (sortedModes.Count == 0)
        {
            throw new ArgumentException(
                "Battle mode catalog cannot be empty.",
                nameof(modeDefinitions));
        }

        var lookup =
            new Dictionary<string, BattleModeDefinition>(
                StringComparer.OrdinalIgnoreCase);

        foreach (BattleModeDefinition mode in sortedModes)
        {
            ValidateMode(mode);

            if (!lookup.TryAdd(mode.ModeId, mode))
            {
                throw new ArgumentException(
                    $"Duplicate battle mode id '{mode.ModeId}'.",
                    nameof(modeDefinitions));
            }
        }

        Manifest = manifest with
        {
            ModeCount = sortedModes.Count
        };

        modes =
            new ReadOnlyCollection<BattleModeDefinition>(
                sortedModes);

        modesById =
            new ReadOnlyDictionary<
                string,
                BattleModeDefinition>(
                lookup);
    }

    public bool TryGetMode(
        string modeId,
        [NotNullWhen(true)] out BattleModeDefinition? mode)
    {
        mode = null;

        if (string.IsNullOrWhiteSpace(modeId))
        {
            return false;
        }

        return modesById.TryGetValue(
            modeId.Trim(),
            out mode);
    }

    public IReadOnlyList<BattleModeDefinition>
        GetAvailableModes(
            DateTime utcNow)
    {
        return modes
            .Where(mode => mode.IsAvailableAt(utcNow))
            .ToArray();
    }

    private static void ValidateMode(
        BattleModeDefinition mode)
    {
        ArgumentNullException.ThrowIfNull(mode);

        ValidateRequiredValue(
            mode.ModeId,
            nameof(mode.ModeId),
            MaximumModeIdLength);

        if (mode.Revision < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(mode.Revision),
                "Battle mode revision must be at least 1.");
        }

        if (!Enum.IsDefined(mode.Kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(mode.Kind));
        }

        ValidateRequiredValue(
            mode.DisplayName,
            nameof(mode.DisplayName),
            MaximumDisplayNameLength);

        ValidateRequiredValue(
            mode.Description,
            nameof(mode.Description),
            MaximumDescriptionLength);

        ArgumentNullException.ThrowIfNull(
            mode.Availability);

        ValidateAvailability(
            mode.Availability);

        ValidateMapPool(mode.MapPool);
        ValidateTeams(mode.Teams);
        ValidateVehicleRules(mode.VehicleRules);
        ValidateMatchmakingRules(mode.MatchmakingRules);
        ValidateRewardRules(mode.Kind, mode.RewardRules);

        if (mode.ExpectedPlayerCount is < 2 or
            > MaximumPlayersPerMatch)
        {
            throw new ArgumentOutOfRangeException(
                nameof(mode.Teams),
                $"Battle mode '{mode.ModeId}' must require between " +
                $"2 and {MaximumPlayersPerMatch} players.");
        }
    }

    private static void ValidateAvailability(
        BattleModeAvailability availability)
    {
        if (availability.StartsAtUtc is DateTime startsAtUtc &&
            startsAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Battle mode availability startsAtUtc must be UTC.");
        }

        if (availability.EndsAtUtc is DateTime endsAtUtc &&
            endsAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Battle mode availability endsAtUtc must be UTC.");
        }

        if (availability.StartsAtUtc is DateTime starts &&
            availability.EndsAtUtc is DateTime ends &&
            ends <= starts)
        {
            throw new ArgumentException(
                "Battle mode availability end must be after its start.");
        }
    }

    private static void ValidateMapPool(
        IReadOnlyList<BattleModeMapPoolEntry> mapPool)
    {
        ArgumentNullException.ThrowIfNull(mapPool);

        if (mapPool.Count == 0)
        {
            throw new ArgumentException(
                "Battle mode map pool cannot be empty.");
        }

        var knownMapIds =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (BattleModeMapPoolEntry map in mapPool)
        {
            ArgumentNullException.ThrowIfNull(map);

            ValidateRequiredValue(
                map.MapId,
                nameof(map.MapId),
                MaximumMapIdLength);

            if (!knownMapIds.Add(map.MapId.Trim()))
            {
                throw new ArgumentException(
                    $"Battle mode map pool contains duplicate map id " +
                    $"'{map.MapId}'.");
            }

            if (map.Weight is < 1 or > 1000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(map.Weight),
                    "Battle mode map weight must be between 1 and 1000.");
            }
        }
    }

    private static void ValidateTeams(
        IReadOnlyList<BattleModeTeamDefinition> teams)
    {
        ArgumentNullException.ThrowIfNull(teams);

        if (teams.Count < 2)
        {
            throw new ArgumentException(
                "Battle mode must define at least two teams.");
        }

        if (teams.Count > MaximumPlayersPerMatch)
        {
            throw new ArgumentOutOfRangeException(
                nameof(teams),
                $"Battle mode cannot define more than " +
                $"{MaximumPlayersPerMatch} teams.");
        }

        for (int index = 0; index < teams.Count; index++)
        {
            BattleModeTeamDefinition team = teams[index];

            ArgumentNullException.ThrowIfNull(team);

            byte expectedTeamId =
                checked((byte)index);

            if (team.TeamId != expectedTeamId)
            {
                throw new ArgumentException(
                    $"Battle mode team ids must be sequential and " +
                    $"zero-based. Expected '{expectedTeamId}' at " +
                    $"team index '{index}', but got '{team.TeamId}'.");
            }

            ValidateRequiredValue(
                team.DisplayName,
                nameof(team.DisplayName),
                MaximumTeamDisplayNameLength);

            if (team.RequiredPlayerCount is < 1 or > 64)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(team.RequiredPlayerCount),
                    "Battle mode team size must be between 1 and 64.");
            }
        }
    }

    private static void ValidateVehicleRules(
        BattleModeVehicleRules vehicleRules)
    {
        ArgumentNullException.ThrowIfNull(vehicleRules);

        if (vehicleRules.MinimumBattleRatingTenths == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vehicleRules.MinimumBattleRatingTenths),
                "Minimum battle rating must be greater than zero.");
        }

        if (vehicleRules.MaximumBattleRatingTenths <
            vehicleRules.MinimumBattleRatingTenths)
        {
            throw new ArgumentException(
                "Maximum battle rating must not be lower than minimum battle rating.");
        }

        ArgumentNullException.ThrowIfNull(
            vehicleRules.AllowedVehicleClasses);

        if (vehicleRules.AllowedVehicleClasses.Count == 0)
        {
            throw new ArgumentException(
                "Battle mode must allow at least one vehicle class.");
        }

        var knownVehicleClasses =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (string vehicleClass
                 in vehicleRules.AllowedVehicleClasses)
        {
            ValidateRequiredValue(
                vehicleClass,
                nameof(vehicleRules.AllowedVehicleClasses),
                64);

            if (!knownVehicleClasses.Add(vehicleClass.Trim()))
            {
                throw new ArgumentException(
                    $"Battle mode contains duplicate vehicle class " +
                    $"'{vehicleClass}'.");
            }
        }
    }

    private static void ValidateMatchmakingRules(
        BattleModeMatchmakingRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        if (rules.InitialMaximumBattleRatingDifferenceTenths >
            rules.MaximumBattleRatingDifferenceTenths)
        {
            throw new ArgumentException(
                "Initial battle rating difference must not exceed the maximum.");
        }

        if (rules.ExpansionIntervalSeconds is < 1 or > 300)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rules.ExpansionIntervalSeconds),
                "Battle rating expansion interval must be between 1 and 300 seconds.");
        }
    }

    private static void ValidateRewardRules(
        BattleModeKind kind,
        BattleModeRewardRules rewardRules)
    {
        ArgumentNullException.ThrowIfNull(rewardRules);

        if (!Enum.IsDefined(rewardRules.ProgressionPolicy))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rewardRules.ProgressionPolicy));
        }

        if (kind == BattleModeKind.Community &&
            rewardRules.ProgressionPolicy !=
            BattleModeProgressionPolicy.Disabled)
        {
            throw new ArgumentException(
                "Community battle modes may not define Warlabs progression.");
        }

        if (kind is BattleModeKind.WarlabsStandard or
            BattleModeKind.WarlabsEvent &&
            rewardRules.ProgressionPolicy !=
            BattleModeProgressionPolicy.Warlabs)
        {
            throw new ArgumentException(
                "Warlabs battle modes must use the Warlabs progression policy.");
        }

        if (rewardRules.ProgressionPolicy ==
            BattleModeProgressionPolicy.Disabled)
        {
            if (rewardRules.CreditsMultiplier != 0m ||
                rewardRules.ExperienceMultiplier != 0m ||
                rewardRules.KillScoreMultiplier != 0m ||
                rewardRules.SpotScoreMultiplier != 0m ||
                rewardRules.CaptureScoreMultiplier != 0m)
            {
                throw new ArgumentException(
                    "Disabled progression requires all reward multipliers to be zero.");
            }

            return;
        }

        ValidateRewardMultiplier(
            rewardRules.CreditsMultiplier,
            nameof(rewardRules.CreditsMultiplier));

        ValidateRewardMultiplier(
            rewardRules.ExperienceMultiplier,
            nameof(rewardRules.ExperienceMultiplier));

        ValidateRewardMultiplier(
            rewardRules.KillScoreMultiplier,
            nameof(rewardRules.KillScoreMultiplier));

        ValidateRewardMultiplier(
            rewardRules.SpotScoreMultiplier,
            nameof(rewardRules.SpotScoreMultiplier));

        ValidateRewardMultiplier(
            rewardRules.CaptureScoreMultiplier,
            nameof(rewardRules.CaptureScoreMultiplier));
    }

    private static void ValidateRewardMultiplier(
        decimal value,
        string name)
    {
        if (value is < 0.01m or > 10m)
        {
            throw new ArgumentOutOfRangeException(
                name,
                "Reward multiplier must be between 0.01 and 10.00.");
        }
    }

    private static void ValidateRequiredValue(
        string value,
        string name,
        int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value,
            name);

        if (value.Trim().Length > maximumLength)
        {
            throw new ArgumentOutOfRangeException(
                name,
                $"Value must not exceed {maximumLength} characters.");
        }
    }
}