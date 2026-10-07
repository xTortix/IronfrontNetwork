using System.Diagnostics.CodeAnalysis;
using Ironfront.GameData.Domain;

namespace Ironfront.GameData.Application;

public interface IBattleModeCatalog
{
    BattleModeCatalogManifest Manifest { get; }

    IReadOnlyList<BattleModeDefinition> Modes { get; }

    bool TryGetMode(
        string modeId,
        [NotNullWhen(true)] out BattleModeDefinition? mode);

    IReadOnlyList<BattleModeDefinition> GetAvailableModes(
        DateTime utcNow);
}