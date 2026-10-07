using Ironfront.Matchmaking.Application.Models;

namespace Ironfront.Matchmaking.Application;

public interface IBattleMatchProvisioner
{
    Task<BattleMatchSnapshot> ProvisionAsync(
        CreateBattleMatchRequest request,
        CancellationToken cancellationToken);
}