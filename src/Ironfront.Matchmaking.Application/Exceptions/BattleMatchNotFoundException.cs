namespace Ironfront.Matchmaking.Application.Exceptions;

public sealed class BattleMatchNotFoundException
    : Exception
{
    public BattleMatchNotFoundException(
        Guid matchId)
        : base(
            $"Battle match '{matchId}' was not found.")
    {
        MatchId = matchId;
    }

    public Guid MatchId { get; }
}