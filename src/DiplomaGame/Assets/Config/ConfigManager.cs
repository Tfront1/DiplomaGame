using Biomes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    private string _configPathsPath = "Assets/Config/ConfigPaths.json";

    void Awake()
    {
        LoadConfigPaths();

        LoadCameraConfig();
        LoadMapConfig();
        LoadTerrainTexturesConfig();
        LoadInputSystemConfig();
        LoadBiomesConfig();
    }

    void LoadConfigPaths()
    {
        var json = File.ReadAllText(_configPathsPath);
        var configPathsData = JsonUtility.FromJson<ConfigPathsData>(json);

        ConfigPaths.CameraConfigPath = configPathsData.CameraConfigPath;
        ConfigPaths.MapConfigPath = configPathsData.MapConfigPath;
        ConfigPaths.TerrainTexturesPath = configPathsData.TexturesPath;
        ConfigPaths.InputActionConfigPath = configPathsData.InputSystemPath;
        ConfigPaths.BiomesConfigPath = configPathsData.BiomesConfigPath;

        Debug.Log("Config paths loaded");
    }

    void LoadCameraConfig()
    {
        var json = File.ReadAllText(ConfigPaths.CameraConfigPath);
        var cameraConfigData = JsonUtility.FromJson<CameraConfigData>(json);

        if (cameraConfigData == null)
        {
            Debug.Log("Error camera config");
            return;
        }

        CameraConfig.EdgeMoveSpeed = cameraConfigData.EdgeMoveSpeed;
        CameraConfig.EdgeSize = cameraConfigData.EdgeSize;
        CameraConfig.MiddleMouseSpeed = cameraConfigData.MiddleMouseSpeed;
        CameraConfig.MinZoom = cameraConfigData.MinZoom;
        CameraConfig.MaxZoom = cameraConfigData.MaxZoom;
        CameraConfig.StepZoom = cameraConfigData.StepZoom;
        CameraConfig.CurrentZoom = cameraConfigData.CurrentZoom;
        CameraConfig.ZoomSpeed = cameraConfigData.ZoomSpeed;

        Debug.Log("Camera config loaded");
    }

    void LoadMapConfig()
    {
        var json = File.ReadAllText(ConfigPaths.MapConfigPath);
        var mapConfigData = JsonUtility.FromJson<MapConfigData>(json);

        if (mapConfigData == null)
        {
            Debug.Log("Error map config");
            return;
        }

        MapConfig.MapWidth = mapConfigData.MapWidth;
        MapConfig.MapHeight = mapConfigData.MapHeight;
        MapConfig.MapStartPointX = mapConfigData.MapStartPointX;
        MapConfig.MapStartPointY = mapConfigData.MapStartPointY;
        MapConfig.CellSize = mapConfigData.CellSize;

        Debug.Log("Map config loaded");
    }

    /// <summary>
    /// Loads and parses terrain textures configuration from a JSON file.
    /// </summary>
    /// <remarks>
    /// This method reads the JSON configuration file for terrain textures, parses it into a `TerrainTexturesData` object,
    /// and validates the texture data. It checks if the texture resolution matches the expected size and if there are any duplicate texture IDs.
    /// If there are any issues, exceptions are thrown to indicate the errors. If everything is valid, it updates the `TerrainTexturesConfig`
    /// with the loaded texture data and logs a success message.
    /// </remarks>
    /// <exception cref="System.Exception"></exception>
    void LoadTerrainTexturesConfig()
    {

        var json = File.ReadAllText(ConfigPaths.TerrainTexturesPath);
        var terrainTexturesData = JsonUtility.FromJson<TerrainTexturesData>(json);

        if (terrainTexturesData == null)
        {
            Debug.Log("Error terrain textures config");
            return;
        }

        TerrainTexturesConfig.TexturesPath = terrainTexturesData.TexturesPath;
        TerrainTexturesConfig.DefaultTextureSize = terrainTexturesData.DefaultTextureSize;

        if (terrainTexturesData.TilemapSprites.Any(x => x.TextureResolution != TerrainTexturesConfig.DefaultTextureSize))
        {
            throw new System.Exception($"Wrong texture resolution. Expected {TerrainTexturesConfig.DefaultTextureSize}X{TerrainTexturesConfig.DefaultTextureSize}");
        }

        var hasDuplicates = terrainTexturesData.TilemapSprites
            .GroupBy(x => x.Id)
            .Any(group => group.Count() > 1);

        var repeatedIds = terrainTexturesData.TilemapSprites
            .GroupBy(x => x.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (hasDuplicates)
        {
            throw new System.Exception($"Textures Id repeats: {repeatedIds}");
        }


        TerrainTexturesConfig.TerrainTextures = terrainTexturesData.TilemapSprites;

        Debug.Log("Terrain textures config loaded and mesh created");
    }

    void LoadInputSystemConfig()
    {
        var json = File.ReadAllText(ConfigPaths.InputActionConfigPath);
        var inputSystemData = JsonUtility.FromJson<InputSystemData>(json);

        if (inputSystemData == null)
        {
            Debug.Log("Error input system config");
            return;
        }

        InputSystemConfig.ActionMap = new List<InputSystemConfig.ActionMapConfig>();

        foreach (var map in inputSystemData.Maps)
        {
            var actionMapConfig = new InputSystemConfig.ActionMapConfig
            {
                MapName = map.name,
                Actions = new List<InputSystemConfig.ActionsConfig>()
            };

            foreach (var action in map.actions)
            {
                var actionsConfig = new InputSystemConfig.ActionsConfig
                {
                    ActionName = action.name,
                    ActionType = action.type,
                    ExpectedControlType = action.expectedControlType,
                    ActionBindings = new List<InputSystemConfig.ActionBindingsConfig>()
                };

                foreach (var binding in action.bindings)
                {
                    var actionBindingsConfig = new InputSystemConfig.ActionBindingsConfig
                    {
                        BindingPath = binding.path,
                        BindingGroup = binding.group
                    };

                    actionsConfig.ActionBindings.Add(actionBindingsConfig);
                }

                actionMapConfig.Actions.Add(actionsConfig);
            }

            InputSystemConfig.ActionMap.Add(actionMapConfig);
        }

        Debug.Log("Input System config loaded");

    }

    void LoadBiomesConfig()
    {
        var json = File.ReadAllText(ConfigPaths.BiomesConfigPath);
        var biomesData = JsonUtility.FromJson<BiomesData>(json);

        if (biomesData == null)
        {
            Debug.Log("Error biomes config");
            return;
        }

        if (biomesData.ValidationWeightPercentageDifference < 0 || 
            biomesData.ValidationWeightPercentageDifference > 100) 
        {
			throw new System.Exception($"Invalid ValidationWeightPercentageDifference: " +
                $"{biomesData.ValidationWeightPercentageDifference}");
		}

        var hasDuplicates = biomesData.Biomes
            .GroupBy(x => x.Id)
            .Any(group => group.Count() > 1);

        var repeatedIds = biomesData.Biomes
            .GroupBy(x => x.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (hasDuplicates)
        {
            throw new System.Exception($"Biomes Id repeats: {repeatedIds}");
        }


        BiomesConfig.NoiseMult = biomesData.NoiseMult;
        BiomesConfig.NoiseDist = biomesData.NoiseDist;
        BiomesConfig.BiomeRange = biomesData.BiomeRange;
		BiomesConfig.ValidationWeightPercentageDifference = biomesData.ValidationWeightPercentageDifference;
		
        biomesData.Biomes.ForEach(x => BiomesConfig.Biomes.Add(new Biome
        {
            Id = x.Id,
            Name = x.Name,
            Weight = x.Weight
        }));
    }
}

//Classes for json reading
#region

[System.Serializable]
public class ConfigPathsData
{
    public string CameraConfigPath;
    public string MapConfigPath;
    public string TexturesPath;
    public string InputSystemPath;
    public string BiomesConfigPath;
}

[System.Serializable]
public class CameraConfigData
{
    public float EdgeMoveSpeed;
    public float EdgeSize;
    public float MiddleMouseSpeed;
    public float MinZoom;
    public float MaxZoom;
    public float StepZoom;
    public float CurrentZoom;
    public float ZoomSpeed;
}

[System.Serializable]
public class MapConfigData
{
    public int MapWidth;
    public int MapHeight;
    public float MapStartPointX;
    public float MapStartPointY;
    public float CellSize;
}

[System.Serializable]
public class TerrainTexturesData
{
    public string TexturesPath;
    public int DefaultTextureSize;
    public List<TerrainTexturesSprite> TilemapSprites;
}

[System.Serializable]
public class TerrainTexturesSprite
{
    public int Id;
    public string TextureFileName;
    public int TextureResolution;
}


[System.Serializable]
public class InputSystemData
{
    public List<ActionMapData> Maps;
}

[System.Serializable]
public class ActionMapData
{
    public string name;
    public List<ActionsData> actions;
}

[System.Serializable]
public class ActionsData
{
    public string name;
    public string type;
    public string expectedControlType;

    public List<ActionBindingsData> bindings;

}

[System.Serializable]
public class ActionBindingsData
{
    public string path;
    public string group;
}

[System.Serializable]
public class BiomesData
{
    public float NoiseMult;
    public float NoiseDist;
    public int BiomeRange;
    public int ValidationWeightPercentageDifference;
    public List<BiomeData> Biomes;
}

[System.Serializable]
public class BiomeData
{
    public int Id;
    public string Name;
    public float Weight;
}

#endregion
