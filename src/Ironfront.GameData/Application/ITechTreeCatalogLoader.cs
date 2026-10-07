namespace Ironfront.GameData.Application;

public interface ITechTreeCatalogLoader
{
    ITechTreeCatalog LoadFromFile(
        string catalogPath,
        IVehicleCatalog vehicleCatalog);
}