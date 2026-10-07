namespace Ironfront.Matchmaking.Application.Exceptions;

public sealed class BattleMatchOwnershipException
    : Exception
{
    public BattleMatchOwnershipException(
        Guid matchId,
        string battleServerInstanceId)
        : base(
            $"Battle match '{matchId}' does not belong to " +
            $"battle server slot '{battleServerInstanceId}'.")
    {
        MatchId = matchId;
        BattleServerInstanceId = battleServerInstanceId;
    }

    public Guid MatchId { get; }

    public string BattleServerInstanceId { get; }
}