using Ironfront.Matchmaking.Application.Models;

namespace Ironfront.Matchmaking.Application;

public interface IBattleJoinTicketIssuer
{
    Task<BattleJoinTicketIssueSnapshot> IssueAsync(
        IssueBattleJoinTicketRequest request,
        CancellationToken cancellationToken);
}