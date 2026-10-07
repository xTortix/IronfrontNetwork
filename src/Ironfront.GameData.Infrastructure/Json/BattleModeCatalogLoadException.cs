namespace Ironfront.GameData.Infrastructure.Json;

public sealed class BattleModeCatalogLoadException
    : InvalidOperationException
{
    public BattleModeCatalogLoadException(
        string catalogPath,
        IEnumerable<string> errors)
        : base(
            $"Battle mode catalog '{catalogPath}' is invalid:" +
            Environment.NewLine +
            string.Join(
                Environment.NewLine,
                errors.Select(error => $"- {error}")))
    {
    }
}