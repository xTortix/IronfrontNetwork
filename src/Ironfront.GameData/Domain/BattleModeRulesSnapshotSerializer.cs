using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ironfront.GameData.Domain;

public static class BattleModeRulesSnapshotSerializer
{
    private static readonly JsonSerializerOptions
        SerializerOptions =
            new(JsonSerializerDefaults.Web)
            {
                WriteIndented = false,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };

    public static string Serialize(
        BattleModeDefinition mode)
    {
        ArgumentNullException.ThrowIfNull(mode);

        return JsonSerializer.Serialize(
            mode,
            SerializerOptions);
    }
}