namespace Ironfront.Matchmaking.Application.Configuration;

public sealed class BattleJoinTicketPolicy
{
    public BattleJoinTicketPolicy(
        TimeSpan lifetime)
    {
        if (lifetime < TimeSpan.FromSeconds(10) ||
            lifetime > TimeSpan.FromMinutes(10))
        {
            throw new ArgumentOutOfRangeException(
                nameof(lifetime),
                "Battle join ticket lifetime must be between 10 seconds and 10 minutes.");
        }

        Lifetime = lifetime;
    }

    public TimeSpan Lifetime { get; }
}