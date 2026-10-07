namespace Ironfront.GameData.Infrastructure.Json;

public sealed class VehicleCatalogLoadException : InvalidOperationException
{
    public string CatalogPath { get; }

    public IReadOnlyList<string> Errors { get; }

    public VehicleCatalogLoadException(
        string catalogPath,
        IEnumerable<string> errors)
        : base(BuildMessage(catalogPath, errors))
    {
        CatalogPath = catalogPath;
        Errors = errors.ToArray();
    }

    private static string BuildMessage(
        string catalogPath,
        IEnumerable<string> errors)
    {
        string errorText = string.Join(
            Environment.NewLine,
            errors.Select(error => $"- {error}"));

        return
            $"Failed to load vehicle catalog '{catalogPath}'."
            + Environment.NewLine
            + errorText;
    }
}