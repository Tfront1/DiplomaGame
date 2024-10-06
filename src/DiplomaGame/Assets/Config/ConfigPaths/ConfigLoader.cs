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
		ConfigPaths.TerrainTexturesPath = configPathsDto.TexturesPath;
		ConfigPaths.InputActionConfigPath = configPathsDto.InputSystemPath;
		ConfigPaths.BiomesConfigPath = configPathsDto.BiomesConfigPath;
		ConfigPaths.BuildingsConfigPath = configPathsDto.BuildingsConfigPaths;

		Debug.Log("Config paths loaded");
	}
}
