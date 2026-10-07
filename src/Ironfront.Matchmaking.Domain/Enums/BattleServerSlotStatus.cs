namespace Ironfront.Matchmaking.Domain.Enums;

public enum BattleServerSlotStatus
{
    Ready,
    Provisioning,
    WaitingForPlayers,
    Warmup,
    Running,
    Finishing,
    Resetting,
    Offline,
    Unhealthy
}