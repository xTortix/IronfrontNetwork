using Ironfront.GameData.Domain;

namespace Ironfront.Matchmaking.Application.Matchmaking;

public static class BattleModeMapSelector
{
    public static string SelectMapId(
        BattleModeDefinition mode)
    {
        ArgumentNullException.ThrowIfNull(mode);

        if (mode.MapPool.Count == 0)
        {
            throw new InvalidOperationException(
                $"Battle mode '{mode.ModeId}' has no maps.");
        }

        int totalWeight = 0;

        foreach (BattleModeMapPoolEntry map in mode.MapPool)
        {
            totalWeight = checked(
                totalWeight + map.Weight);
        }

        int roll = Random.Shared.Next(totalWeight);

        foreach (BattleModeMapPoolEntry map in mode.MapPool)
        {
            if (roll < map.Weight)
            {
                return map.MapId;
            }

            roll -= map.Weight;
        }

        throw new InvalidOperationException(
            $"Could not select a map for mode '{mode.ModeId}'.");
    }
}