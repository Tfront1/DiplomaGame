using UnityEngine;
using System.IO;

public class ConfigManager : MonoBehaviour
{
    public string configPathsPath = "Assets/Config/configPaths.json";

    void Awake()
    {
        LoadConfigPaths();
        LoadCameraConfig();
        LoadMapConfig();
    }

    void LoadConfigPaths()
    {
        var json = File.ReadAllText(configPathsPath);
        var configPathsData = JsonUtility.FromJson<ConfigPathsData>(json);

        ConfigPaths.CameraConfigPath = configPathsData.cameraConfigPath;
        ConfigPaths.MapConfigPath = configPathsData.mapConfigPath;

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
}

[System.Serializable]
public class ConfigPathsData
{
    public string cameraConfigPath;
    public string mapConfigPath;
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
