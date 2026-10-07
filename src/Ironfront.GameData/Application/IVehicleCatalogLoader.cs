namespace Ironfront.GameData.Application;

public interface IVehicleCatalogLoader
{
    IVehicleCatalog LoadFromFile(string catalogPath);
}