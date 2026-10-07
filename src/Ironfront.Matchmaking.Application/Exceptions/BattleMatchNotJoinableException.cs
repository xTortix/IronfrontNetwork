namespace Ironfront.Matchmaking.Application.Exceptions;

public sealed class BattleMatchNotJoinableException
    : Exception
{
    public BattleMatchNotJoinableException(
        Guid matchId,
        string status)
        : base(
            $"Battle match '{matchId}' is not joinable from status '{status}'.")
    {
        MatchId = matchId;
        Status = status;
    }

    public Guid MatchId { get; }

    public string Status { get; }
}