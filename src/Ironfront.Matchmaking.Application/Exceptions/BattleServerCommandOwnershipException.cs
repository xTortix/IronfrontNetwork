namespace Ironfront.Matchmaking.Application.Exceptions;

public sealed class BattleServerCommandOwnershipException
    : Exception
{
    public BattleServerCommandOwnershipException(
        Guid commandId,
        string battleServerInstanceId)
        : base(
            $"Battle server command '{commandId}' does not belong to slot '{battleServerInstanceId}'.")
    {
        CommandId = commandId;
        BattleServerInstanceId = battleServerInstanceId;
    }

    public Guid CommandId { get; }

    public string BattleServerInstanceId { get; }
}