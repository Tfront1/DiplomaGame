using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    private string _configPathsPath = "Assets/Config/ConfigPaths/ConfigPaths.json";
    private static bool _configsLoaded = false;

    void Awake()
    {
        if (!_configsLoaded)
        {
            ConfigLoader.LoadConfigPaths(_configPathsPath);

            ConfigLoader.LoadCameraConfig();
            ConfigLoader.LoadMapConfig();
            ConfigLoader.LoadTerrainTexturesConfig();
            ConfigLoader.LoadInputSystemConfig();

            ConfigLoader.LoadResourceItemsConfig();
            ConfigLoader.LoadWeaponsConfig();
            ConfigLoader.LoadArmorsConfig();

            ConfigLoader.LoadSuppliesConfig();
            ConfigLoader.LoadBuildingsConfig();
            ConfigLoader.LoadBiomesConfig();
            ConfigLoader.LoadBiomeSuppliesConfig();
            ConfigLoader.LoadSupplyTexturesConfig();
            ConfigLoader.LoadBuildingTexturesConfig();

            ConfigLoader.LoadUnitsConfig();
            ConfigLoader.LoadUnitTexturesConfig();

            ConfigLoader.LoadCraftingRecipesConfig();

            ConfigLoader.LoadUITexturesConfig();
            //ConfigLoader.LoadForOfWarTexturesConfig();

            _configsLoaded = true;
        }
    }
}