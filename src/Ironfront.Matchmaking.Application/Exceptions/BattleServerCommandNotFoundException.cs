namespace Ironfront.Matchmaking.Application.Exceptions;

public sealed class BattleServerCommandNotFoundException
    : Exception
{
    public BattleServerCommandNotFoundException(
        Guid commandId)
        : base(
            $"Battle server command '{commandId}' was not found.")
    {
        CommandId = commandId;
    }

    public Guid CommandId { get; }
}