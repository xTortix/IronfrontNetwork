namespace Ironfront.Matchmaking.Application.Exceptions;

public sealed class NoBattleMatchPlayerSlotAvailableException
    : Exception
{
    public NoBattleMatchPlayerSlotAvailableException(
        Guid matchId)
        : base(
            $"No player slot is available for battle match '{matchId}'.")
    {
        MatchId = matchId;
    }

    public Guid MatchId { get; }
}