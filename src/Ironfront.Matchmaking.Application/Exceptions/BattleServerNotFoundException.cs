namespace Ironfront.Matchmaking.Application.Exceptions;

public sealed class BattleServerSlotNotFoundException
    : Exception
{
    public BattleServerSlotNotFoundException(
        string serverInstanceId)
        : base(
            $"Battle server slot '{serverInstanceId}' is not registered.")
    {
        ServerInstanceId = serverInstanceId;
    }

    public string ServerInstanceId { get; }
}