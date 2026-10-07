namespace Ironfront.MatchmakingService.Hosting;

public sealed class BattleServerHealthReconciliationOptions
{
    public BattleServerHealthReconciliationOptions(
        TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval),
                "Reconciliation interval must be greater than zero.");
        }

        Interval = interval;
    }

    public TimeSpan Interval { get; }
}