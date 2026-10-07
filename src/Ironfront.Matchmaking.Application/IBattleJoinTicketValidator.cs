using Ironfront.Matchmaking.Application.Models;

namespace Ironfront.Matchmaking.Application;

public interface IBattleJoinTicketValidator
{
    Task<BattleJoinTicketValidationSnapshot>
        ValidateAndConsumeAsync(
            string battleServerInstanceId,
            ValidateBattleJoinTicketRequest request,
            CancellationToken cancellationToken);
}