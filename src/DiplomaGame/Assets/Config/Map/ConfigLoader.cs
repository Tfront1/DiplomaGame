using System.IO;
using UnityEngine;

public static partial class ConfigLoader
{
	public static void LoadMapConfig()
	{
		var json = File.ReadAllText(ConfigPaths.MapConfigPath);
		var mapConfigDto = JsonUtility.FromJson<MapConfigDto>(json);

		if (mapConfigDto == null)
		{
			Debug.Log("Error map config");
			return;
		}

		MapConfig.MapWidth = mapConfigDto.MapWidth;
		MapConfig.MapHeight = mapConfigDto.MapHeight;
		MapConfig.MapStartPointX = mapConfigDto.MapStartPointX;
		MapConfig.MapStartPointY = mapConfigDto.MapStartPointY;
		MapConfig.CellSize = mapConfigDto.CellSize;

		Debug.Log("Map config loaded");
	}
}
