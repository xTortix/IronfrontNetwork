namespace Ironfront.Matchmaking.Application;

public interface IBattleMatchPlayerConnectionLifecycle
{
    Task MarkDisconnectedAsync(
        string battleServerInstanceId,
        Guid matchId,
        Guid matchPlayerId,
        CancellationToken cancellationToken);
}