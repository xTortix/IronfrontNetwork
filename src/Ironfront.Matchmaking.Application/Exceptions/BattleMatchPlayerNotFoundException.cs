namespace Ironfront.Matchmaking.Application.Exceptions;

public sealed class BattleMatchPlayerNotFoundException
    : Exception
{
    public BattleMatchPlayerNotFoundException(
        Guid matchPlayerId)
        : base(
            $"Battle match player '{matchPlayerId}' was not found.")
    {
        MatchPlayerId = matchPlayerId;
    }

    public Guid MatchPlayerId { get; }
}