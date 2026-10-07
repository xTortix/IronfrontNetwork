namespace Ironfront.GameData.Domain;

public sealed record VehicleCatalogEntry(
    string VehicleId,
    string DisplayName,
    string Nation,
    string VehicleClass,
    byte BattleRatingTenths);