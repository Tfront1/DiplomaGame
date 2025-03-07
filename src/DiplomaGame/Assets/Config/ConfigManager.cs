using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    private string _configPathsPath = "Assets/Config/ConfigPaths/ConfigPaths.json";

    void Awake()
    {
		ConfigLoader.LoadConfigPaths(_configPathsPath);

		ConfigLoader.LoadCameraConfig();
		ConfigLoader.LoadMapConfig();
        ConfigLoader.LoadTerrainTexturesConfig();
		ConfigLoader.LoadInputSystemConfig();

        ConfigLoader.LoadResourceItemsConfig();

        ConfigLoader.LoadSuppliesConfig();
        ConfigLoader.LoadBuildingsConfig();
        ConfigLoader.LoadBiomesConfig();
        ConfigLoader.LoadBiomeSuppliesConfig();
        ConfigLoader.LoadSupplyTexturesConfig();
        ConfigLoader.LoadBuildingTexturesConfig();

        ConfigLoader.LoadUnitsConfig();
        ConfigLoader.LoadUnitTexturesConfig();

    }
}