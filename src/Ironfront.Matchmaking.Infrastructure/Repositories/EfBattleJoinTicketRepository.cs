using Ironfront.Matchmaking.Application;
using Ironfront.Matchmaking.Domain.Entities;
using Ironfront.Matchmaking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Infrastructure.Repositories;

public sealed class EfBattleJoinTicketRepository
    : IBattleJoinTicketRepository
{
    private readonly MatchmakingDbContext dbContext;

    public EfBattleJoinTicketRepository(
        MatchmakingDbContext dbContext)
    {
        this.dbContext = dbContext
                         ?? throw new ArgumentNullException(
                             nameof(dbContext));
    }

    public Task<BattleJoinTicket?> FindByIdAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        return dbContext.BattleJoinTickets
            .SingleOrDefaultAsync(
                ticket =>
                    ticket.TicketId == ticketId,
                cancellationToken);
    }
    
    public Task<BattleJoinTicket?> FindByIdForUpdateAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        return dbContext.BattleJoinTickets
            .FromSqlInterpolated($"""
                                  SELECT *
                                  FROM battle_join_tickets
                                  WHERE "TicketId" = {ticketId}
                                  FOR UPDATE
                                  """)
            .SingleOrDefaultAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<BattleJoinTicket>>
        GetIssuedForMatchPlayerForUpdateAsync(
            Guid matchPlayerId,
            CancellationToken cancellationToken)
    {
        return await dbContext.BattleJoinTickets
            .FromSqlInterpolated($"""
                                  SELECT *
                                  FROM battle_join_tickets
                                  WHERE "MatchPlayerId" = {matchPlayerId}
                                    AND "Status" = {BattleJoinTicketStatus.Issued.ToString()}
                                  FOR UPDATE
                                  """)
            .OrderBy(ticket => ticket.IssuedAtUtc)
            .ToListAsync(cancellationToken);
    }
    
    public void Add(
        BattleJoinTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        dbContext.BattleJoinTickets.Add(ticket);
    }
}