namespace Ironfront.Matchmaking.Application.Configuration;

public sealed class BattleServerSelectionPolicy
{
    public BattleServerSelectionPolicy(
        TimeSpan maximumHeartbeatAge)
    {
        if (maximumHeartbeatAge <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumHeartbeatAge),
                "Maximum heartbeat age must be greater than zero.");
        }

        MaximumHeartbeatAge = maximumHeartbeatAge;
    }

    public TimeSpan MaximumHeartbeatAge { get; }
}