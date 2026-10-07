using Ironfront.Matchmaking.Application.Models;

namespace Ironfront.Matchmaking.Application;

public interface IBattleQueueLifecycle
{
    Task<BattleQueueStatusSnapshot> JoinAsync(
        JoinBattleQueueCommand command,
        CancellationToken cancellationToken);

    Task<BattleQueueStatusSnapshot> GetStatusAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<BattleQueueLeaveSnapshot> LeaveAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<BattleJoinTicketIssueSnapshot>
        IssueConnectionTicketAsync(
            Guid userId,
            CancellationToken cancellationToken);
}