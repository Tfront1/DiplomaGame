using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    public string _configPathsPath = "Assets/Config/configPaths.json";

    void Awake()
    {
        LoadConfigPaths();

        LoadCameraConfig();
        LoadMapConfig();
        LoadTerrainTexturesConfig();
    }

    void LoadConfigPaths()
    {
        var json = File.ReadAllText(_configPathsPath);
        var configPathsData = JsonUtility.FromJson<ConfigPathsData>(json);

        ConfigPaths.CameraConfigPath = configPathsData.cameraConfigPath;
        ConfigPaths.MapConfigPath = configPathsData.mapConfigPath;
        ConfigPaths.TerrainTexturesPath = configPathsData.texturesPath;

        Debug.Log("Config paths loaded");
    }

    void LoadCameraConfig()
    {
        var json = File.ReadAllText(ConfigPaths.CameraConfigPath);
        var cameraConfigData = JsonUtility.FromJson<CameraConfigData>(json);

        CameraConfig.EdgeMoveSpeed = cameraConfigData.edgeMoveSpeed;
        CameraConfig.EdgeSize = cameraConfigData.edgeSize;
        CameraConfig.MiddleMouseSpeed = cameraConfigData.middleMouseSpeed;
        CameraConfig.MinZoom = cameraConfigData.minZoom;
        CameraConfig.MaxZoom = cameraConfigData.maxZoom;
        CameraConfig.StepZoom = cameraConfigData.stepZoom;
        CameraConfig.CurrentZoom = cameraConfigData.currentZoom;
        CameraConfig.ZoomSpeed = cameraConfigData.zoomSpeed;

        Debug.Log("Camera config loaded");
    }

    void LoadMapConfig()
    {
        var json = File.ReadAllText(ConfigPaths.MapConfigPath);
        var mapConfigData = JsonUtility.FromJson<MapConfigData>(json);

        MapConfig.MapWidth = mapConfigData.mapWidth;
        MapConfig.MapHeight = mapConfigData.mapHeight;
        MapConfig.MapStartPointX = mapConfigData.mapStartPointX;
        MapConfig.MapStartPointY = mapConfigData.mapStartPointY;
        MapConfig.SellSize = mapConfigData.sellSize;

        Debug.Log("Map config loaded");
    }

    void LoadTerrainTexturesConfig()
    {

        var json = File.ReadAllText(ConfigPaths.TerrainTexturesPath);
        var terrainTexturesData = JsonUtility.FromJson<TerrainTexturesData>(json);

        TerrainTexturesConfig._texturesPath = terrainTexturesData.texturesPath;
        TerrainTexturesConfig._defaultTextureSize = terrainTexturesData.defaultTextureSize;

        if (terrainTexturesData.tilemapSprites.Any(x => x.textureResolution != TerrainTexturesConfig._defaultTextureSize))
        {
            throw new System.Exception("Wrong texture resolution. Expected 64x64");
        }

        var hasDuplicates = terrainTexturesData.tilemapSprites
            .GroupBy(x => x.id)
            .Any(group => group.Count() > 1);

        var repeatedIds = terrainTexturesData.tilemapSprites
            .GroupBy(x => x.id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (hasDuplicates)
        {
            throw new System.Exception($"Textures id repeats: {repeatedIds}");
        }


        TerrainTexturesConfig._terrainTextures = terrainTexturesData.tilemapSprites;

        Debug.Log("Terrain textures config loaded and mesh created");
    }
}

[System.Serializable]
public class ConfigPathsData
{
    public string cameraConfigPath;
    public string mapConfigPath;
    public string texturesPath;
}

[System.Serializable]
public class CameraConfigData
{
    public float edgeMoveSpeed;
    public float edgeSize;
    public float middleMouseSpeed;
    public float minZoom;
    public float maxZoom;
    public float stepZoom;
    public float currentZoom;
    public float zoomSpeed;
}

[System.Serializable]
public class MapConfigData
{
    public int mapWidth;
    public int mapHeight;
    public float mapStartPointX;
    public float mapStartPointY;
    public float sellSize;
}

[System.Serializable]
public class TerrainTexturesData
{
    public string texturesPath;
    public int defaultTextureSize;
    public List<TerrainTexturesSprite> tilemapSprites;
}

[System.Serializable]
public class TerrainTexturesSprite
{
    public int id;
    public string name;
    public string textureFileName;
    public int textureResolution;
}