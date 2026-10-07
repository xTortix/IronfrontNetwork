namespace Ironfront.Matchmaking.Application.Exceptions;

public sealed class BattleMatchPlayerNotEligibleForTicketException
    : Exception
{
    public BattleMatchPlayerNotEligibleForTicketException(
        Guid matchPlayerId,
        string status)
        : base(
            $"Battle match player '{matchPlayerId}' cannot receive a join ticket from status '{status}'.")
    {
        MatchPlayerId = matchPlayerId;
        Status = status;
    }

    public Guid MatchPlayerId { get; }

    public string Status { get; }
}