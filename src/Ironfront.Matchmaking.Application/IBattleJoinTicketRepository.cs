using Ironfront.Matchmaking.Domain.Entities;

namespace Ironfront.Matchmaking.Application;

public interface IBattleJoinTicketRepository
{
    Task<BattleJoinTicket?> FindByIdAsync(
        Guid ticketId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BattleJoinTicket>>
        GetIssuedForMatchPlayerForUpdateAsync(
            Guid matchPlayerId,
            CancellationToken cancellationToken);

    Task<BattleJoinTicket?> FindByIdForUpdateAsync(
        Guid ticketId,
        CancellationToken cancellationToken);
    
    void Add(BattleJoinTicket ticket);
}