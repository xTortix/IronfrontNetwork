using Ironfront.Matchmaking.Application.Models;

namespace Ironfront.Matchmaking.Application;

public interface IBattleServerCommandInbox
{
    Task<BattleServerCommandSnapshot?> PollNextAsync(
        string battleServerInstanceId,
        CancellationToken cancellationToken);

    Task<BattleServerCommandSnapshot> AcknowledgeAsync(
        string battleServerInstanceId,
        BattleServerCommandAcknowledgement acknowledgement,
        CancellationToken cancellationToken);
}