namespace Ironfront.GameData.Application;

public interface IBattleModeCatalogLoader
{
    IBattleModeCatalog LoadFromFile(
        string catalogPath);
}