namespace Ironfront.GameData.Domain;

public enum BattleModeKind
{
    WarlabsStandard,
    WarlabsEvent,
    Community
}

public enum BattleModeProgressionPolicy
{
    Warlabs,
    Disabled
}

public sealed record BattleModeDefinition(
    string ModeId,
    int Revision,
    BattleModeKind Kind,
    string DisplayName,
    string Description,
    BattleModeAvailability Availability,
    IReadOnlyList<BattleModeMapPoolEntry> MapPool,
    IReadOnlyList<BattleModeTeamDefinition> Teams,
    BattleModeVehicleRules VehicleRules,
    BattleModeMatchmakingRules MatchmakingRules,
    BattleModeRewardRules RewardRules)
{
    public int ExpectedPlayerCount =>
        Teams.Sum(team => team.RequiredPlayerCount);

    public bool IsAvailableAt(
        DateTime utcNow)
    {
        return Availability.IsAvailableAt(utcNow);
    }
}

public sealed record BattleModeAvailability(
    bool IsEnabled,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc)
{
    public bool IsAvailableAt(
        DateTime utcNow)
    {
        if (!IsEnabled)
        {
            return false;
        }

        DateTime normalizedUtcNow =
            utcNow.Kind == DateTimeKind.Utc
                ? utcNow
                : utcNow.ToUniversalTime();

        if (StartsAtUtc is DateTime startsAtUtc &&
            normalizedUtcNow < startsAtUtc)
        {
            return false;
        }

        if (EndsAtUtc is DateTime endsAtUtc &&
            normalizedUtcNow >= endsAtUtc)
        {
            return false;
        }

        return true;
    }
}

public sealed record BattleModeMapPoolEntry(
    string MapId,
    int Weight);

public sealed record BattleModeTeamDefinition(
    byte TeamId,
    string DisplayName,
    byte RequiredPlayerCount);

public sealed record BattleModeVehicleRules(
    byte MinimumBattleRatingTenths,
    byte MaximumBattleRatingTenths,
    IReadOnlyList<string> AllowedVehicleClasses);

public sealed record BattleModeMatchmakingRules(
    byte InitialMaximumBattleRatingDifferenceTenths,
    byte BattleRatingDifferenceExpansionTenths,
    int ExpansionIntervalSeconds,
    byte MaximumBattleRatingDifferenceTenths);

public sealed record BattleModeRewardRules(
    BattleModeProgressionPolicy ProgressionPolicy,
    decimal CreditsMultiplier,
    decimal ExperienceMultiplier,
    decimal KillScoreMultiplier,
    decimal SpotScoreMultiplier,
    decimal CaptureScoreMultiplier);