namespace Ironfront.Matchmaking.Domain.Enums;

public enum BattleMatchStatus
{
    Provisioning,
    WaitingForPlayers,
    Running,
    Finishing,
    Completed,
    Failed,
    Cancelled
}