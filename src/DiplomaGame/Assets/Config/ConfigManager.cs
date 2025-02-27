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

        ConfigLoader.LoadSuppliesConfig();
        ConfigLoader.LoadBiomesConfig();
        ConfigLoader.LoadBiomeSuppliesConfig();
        ConfigLoader.LoadSupplyTexturesConfig();

        ConfigLoader.LoadBuildingsConfig();

        ConfigLoader.LoadUnitsConfig();
        ConfigLoader.LoadUnitTexturesConfig();
    }
}