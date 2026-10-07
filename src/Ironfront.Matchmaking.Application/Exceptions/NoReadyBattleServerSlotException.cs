namespace Ironfront.Matchmaking.Application.Exceptions;

public sealed class NoReadyBattleServerSlotException
    : Exception
{
    public NoReadyBattleServerSlotException()
        : base(
            "No ready battle server slot is currently available.")
    {
    }
}