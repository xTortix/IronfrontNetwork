namespace Ironfront.MatchmakingService.Hosting;

public sealed class BattleMatchmakingOptions
{
    public BattleMatchmakingOptions(
        TimeSpan interval,
        int maximumMatchesPerModePerCycle)
    {
        if (interval < TimeSpan.FromMilliseconds(250) ||
            interval > TimeSpan.FromSeconds(60))
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval),
                "Matchmaking interval must be between 250 milliseconds and 60 seconds.");
        }

        if (maximumMatchesPerModePerCycle is < 1 or > 128)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumMatchesPerModePerCycle),
                "Maximum matches per mode per cycle must be between 1 and 128.");
        }

        Interval = interval;
        MaximumMatchesPerModePerCycle =
            maximumMatchesPerModePerCycle;
    }

    public TimeSpan Interval { get; }

    public int MaximumMatchesPerModePerCycle { get; }
}