using System.IO;
using UnityEngine;

public static partial class ConfigLoader
{
	public static void LoadConfigPaths(
		string configPathsPath)
	{
		var json = File.ReadAllText(configPathsPath);
		var configPathsDto = JsonUtility.FromJson<ConfigPathsDto>(json);

		ConfigPaths.CameraConfigPath = configPathsDto.CameraConfigPath;
		ConfigPaths.MapConfigPath = configPathsDto.MapConfigPath;
		ConfigPaths.TerrainTexturesPath = configPathsDto.TerrainTexturesPath;
		ConfigPaths.SupplyTexturesPath = configPathsDto.SupplyTexturesPath;
        ConfigPaths.BuildingTexturesPath = configPathsDto.BuildingTexturesPath;
        ConfigPaths.InputActionConfigPath = configPathsDto.InputSystemPath;
		ConfigPaths.BiomesConfigPath = configPathsDto.BiomesConfigPath;
        ConfigPaths.SuppliesConfigPath = configPathsDto.SuppliesConfigPath;
        ConfigPaths.BiomeSuppliesConfigPath = configPathsDto.BiomeSuppliesConfigPath;
        ConfigPaths.BuildingsConfigPath = configPathsDto.BuildingsConfigPath;
        ConfigPaths.UnitsConfigPath = configPathsDto.UnitsConfigPath;
        ConfigPaths.UnitsTexturesPath = configPathsDto.UnitsTexturesPath;
        ConfigPaths.ResourceItemsConfigPath = configPathsDto.ResourceItemsConfigPath;
        ConfigPaths.CraftingRecipesConfigPath = configPathsDto.CraftingRecipesConfigPath;
        ConfigPaths.WeaponsConfigPath = configPathsDto.WeaponsConfigPath;
        ConfigPaths.ArmorsConfigPath = configPathsDto.ArmorsConfigPath;
        ConfigPaths.ArmorsConfigPath = configPathsDto.ArmorsConfigPath;
        ConfigPaths.UITexturesPath = configPathsDto.UITexturesPath;
        ConfigPaths.FogOfWarTexturesPath = configPathsDto.FogOfWarTexturesPath;

        Debug.Log("Config paths loaded");
	}
}
