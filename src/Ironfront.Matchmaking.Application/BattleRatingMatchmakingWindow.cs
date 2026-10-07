using Ironfront.GameData.Domain;
using Ironfront.Matchmaking.Domain.Entities;

namespace Ironfront.Matchmaking.Application.Matchmaking;

public static class BattleRatingMatchmakingWindow
{
    public static bool IsEligibleForMode(
        BattleQueueEntry queueEntry,
        BattleModeDefinition mode)
    {
        ArgumentNullException.ThrowIfNull(queueEntry);
        ArgumentNullException.ThrowIfNull(mode);

        if (!string.Equals(
                queueEntry.ModeId,
                mode.ModeId,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (queueEntry.ModeRevision != mode.Revision)
        {
            return false;
        }

        if (queueEntry.VehicleBattleRatingTenths <
                mode.VehicleRules.MinimumBattleRatingTenths ||
            queueEntry.VehicleBattleRatingTenths >
                mode.VehicleRules.MaximumBattleRatingTenths)
        {
            return false;
        }

        return mode.VehicleRules.AllowedVehicleClasses.Any(
            allowedVehicleClass =>
                string.Equals(
                    allowedVehicleClass,
                    queueEntry.VehicleClass,
                    StringComparison.OrdinalIgnoreCase));
    }

    public static byte GetMaximumDifferenceTenths(
        BattleModeMatchmakingRules rules,
        DateTime queuedAtUtc,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(rules);

        if (rules.ExpansionIntervalSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rules),
                "Battle rating expansion interval must be greater than zero.");
        }

        if (rules.InitialMaximumBattleRatingDifferenceTenths >
            rules.MaximumBattleRatingDifferenceTenths)
        {
            throw new ArgumentException(
                "Initial battle rating difference must not exceed the maximum.",
                nameof(rules));
        }

        DateTime normalizedQueuedAtUtc =
            NormalizeUtc(queuedAtUtc);

        DateTime normalizedUtcNow =
            NormalizeUtc(utcNow);

        int initialDifference =
            rules.InitialMaximumBattleRatingDifferenceTenths;

        int maximumDifference =
            rules.MaximumBattleRatingDifferenceTenths;

        if (normalizedUtcNow <= normalizedQueuedAtUtc)
        {
            return (byte)initialDifference;
        }

        TimeSpan queueDuration =
            normalizedUtcNow - normalizedQueuedAtUtc;

        long expansionSteps =
            (long)Math.Floor(
                queueDuration.TotalSeconds /
                rules.ExpansionIntervalSeconds);

        long expandedDifference =
            initialDifference +
            expansionSteps *
            rules.BattleRatingDifferenceExpansionTenths;

        return (byte)Math.Min(
            expandedDifference,
            maximumDifference);
    }

    public static bool AreCompatible(
        BattleQueueEntry firstEntry,
        BattleQueueEntry secondEntry,
        BattleModeDefinition mode,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(firstEntry);
        ArgumentNullException.ThrowIfNull(secondEntry);
        ArgumentNullException.ThrowIfNull(mode);

        if (firstEntry.QueueEntryId ==
            secondEntry.QueueEntryId)
        {
            return false;
        }

        if (firstEntry.UserId == secondEntry.UserId)
        {
            return false;
        }

        if (!IsEligibleForMode(firstEntry, mode) ||
            !IsEligibleForMode(secondEntry, mode))
        {
            return false;
        }

        int ratingDifferenceTenths =
            Math.Abs(
                firstEntry.VehicleBattleRatingTenths -
                secondEntry.VehicleBattleRatingTenths);

        byte firstMaximumDifference =
            GetMaximumDifferenceTenths(
                mode.MatchmakingRules,
                firstEntry.QueuedAtUtc,
                utcNow);

        byte secondMaximumDifference =
            GetMaximumDifferenceTenths(
                mode.MatchmakingRules,
                secondEntry.QueuedAtUtc,
                utcNow);

        /*
         * Beide Spieler müssen den Abstand akzeptieren.
         *
         * Ein lange wartender Spieler mit großem Suchfenster kann
         * deshalb keinen gerade erst gequeueten Spieler gegen dessen
         * engeres BR-Fenster ziehen.
         */
        return ratingDifferenceTenths <=
                   firstMaximumDifference &&
               ratingDifferenceTenths <=
                   secondMaximumDifference;
    }

    public static byte GetBroadCandidateSearchRangeTenths(
        BattleModeDefinition mode,
        BattleQueueEntry queueEntry,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(mode);
        ArgumentNullException.ThrowIfNull(queueEntry);

        return GetMaximumDifferenceTenths(
            mode.MatchmakingRules,
            queueEntry.QueuedAtUtc,
            utcNow);
    }

    private static DateTime NormalizeUtc(
        DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }
}