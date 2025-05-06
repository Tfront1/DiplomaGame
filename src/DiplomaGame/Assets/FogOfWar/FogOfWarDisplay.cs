using GameUtilities.MonoBehaviours;
using System.Collections.Generic;
using System.IO;
using Town;
using UnityEngine;

namespace FogOfWar
{
    public class FogOfWarDisplay : MonoBehaviour
    {
        private int chunkSize = 50;

        private int mapWidth;
        private int mapHeight;
        private Vector3 mapOrigin;

        private Dictionary<Vector2Int, FogOfWarChunk> chunks = new();
        private Dictionary<Vector2Int, FogOfWarChunk> activeChunks = new();
        private Dictionary<UnitItem, Vector3> lastUnitPositions = new();
        private Dictionary<BuildingItem, Vector3> lastBuildingPositions = new();

        private static FogOfWarDisplay _instance;
        private static readonly object _lock = new();

        public Shader FogShader { get; set; }
        public Material FogMaterial { get; set; }
        public Material FogCloudMaterial { get; set; }
        public Sprite WhiteSprite { get; set; }

        private const string FOG_SHADER_PATH = "FogOfWar/Shaders/FogOfWarShader";
        private const string FOG_MATERIAL_PATH = "FogOfWar/Materials/FogOfWarMaterial";
        private const string FOG_SPRITE_PATH = "FogOfWar/Sprites/WhiteSquare";
        private const string FOG_CLOUD_PATH = "FogOfWar/Materials/FogCloudOverlay";

        private Dictionary<(Vector2Int, float), Texture2D> _noiseTextureCache = new();
        private Dictionary<Vector2Int, float> neighborChunks = new();
        private float neighborChunkTimeout = 10.0f;

        public static FogOfWarDisplay Instance
        {
            get
            {
                lock (_lock)
                {
                    var instances = FindObjectsOfType<FogOfWarDisplay>();
                    if (instances.Length > 0)
                    {
                        _instance = instances[0];
                        if (instances.Length > 1)
                        {
                            Debug.LogWarning("Found multiple FogOfWarDisplay instances in scene. Using the first one.");
                            for (var i = 1; i < instances.Length; i++)
                            {
                                Destroy(instances[i].gameObject);
                            }
                        }
                    }
                    else
                    {
                        var go = new GameObject("FogOfWarDisplay");
                        _instance = go.AddComponent<FogOfWarDisplay>();
                    }
                    return _instance;
                }
            }
        }

        public void StartFog(int width, int height, Vector3 origin)
        {
            InitializeResources();

            mapWidth = width;
            mapHeight = height;
            mapOrigin = origin;

            CreateChunks();
            CameraManager.SubscribeToCameraMove(OnCameraMove);
        }

        private void CreateChunks()
        {
            var chunksX = Mathf.CeilToInt((float)mapWidth / chunkSize);
            var chunksY = Mathf.CeilToInt((float)mapHeight / chunkSize);

            var direction = GetRandomDirection();

            for (var y = 0; y < chunksY; y++)
            {
                for (var x = 0; x < chunksX; x++)
                {
                    var chunkCoord = new Vector2Int(x, y);

                    if (chunks.ContainsKey(chunkCoord))
                        continue;

                    var chunkWidth = Mathf.Min(chunkSize, mapWidth - x * chunkSize);
                    var chunkHeight = Mathf.Min(chunkSize, mapHeight - y * chunkSize);

                    var chunkPosition = new Vector3(
                        mapOrigin.x + x * chunkSize * MapConfig.CellSize + chunkSize * MapConfig.CellSize / 2.0f,
                        mapOrigin.y + y * chunkSize * MapConfig.CellSize + chunkSize * MapConfig.CellSize / 2.0f,
                        mapOrigin.z
                    );

                    var chunkScale = new Vector3(
                        chunkWidth * MapConfig.CellSize,
                        chunkHeight * MapConfig.CellSize,
                        1f
                    );

                    CreateChunk(chunkCoord, chunkPosition, chunkScale, chunkWidth, chunkHeight, direction);
                }
            }
        }

        private void CreateChunk(Vector2Int chunkCoords, Vector3 position, Vector3 scale, int width, int height, Vector2 direction)
        {
            var chunkObject = new GameObject($"FogChunk_{chunkCoords.x}_{chunkCoords.y}");
            chunkObject.transform.parent = transform;
            chunkObject.transform.position = position;

            var chunkComponent = chunkObject.AddComponent<FogOfWarChunk>();

            chunkComponent.SetupFog(position, scale, width, height, direction);

            chunks.Add(chunkCoords, chunkComponent);
        }

        private void ActivateChunk(FogOfWarChunk chunk)
        {
            var perlinTextureWidth = chunk.chunkWidth * (int)MapConfig.CellSize;
            var perlinTextureHeight = chunk.chunkHeight * (int)MapConfig.CellSize;

            var noiseTexture1 = GetOrCreatePerlinNoiseTexture(perlinTextureWidth, perlinTextureHeight, 1f);
            var noiseTexture2 = GetOrCreatePerlinNoiseTexture(perlinTextureWidth, perlinTextureWidth, 2f);
            var noiseTexture3 = GetOrCreatePerlinNoiseTexture(perlinTextureWidth, perlinTextureWidth, 3f);

            chunk.StartFog(noiseTexture1, noiseTexture2, noiseTexture3);
            var units = TownRegistry.UserTown.Units;
            foreach (var unit in units)
            {
                chunk.UpdateUnitData(unit);
            }

            var buildings = TownRegistry.UserTown.Buildings;
            foreach (var building in buildings)
            {
                chunk.UpdateBuildingData(building);
            }
        }

        private void DeactivateChunk(FogOfWarChunk chunk)
        {
            chunk.StopFog();
        }

        private void Update()
        {
            var units = TownRegistry.UserTown.Units;
            var toClearCurrentVisibleFog = false;
            foreach (var unit in units)
            {
                if (unit.IsDestroyed && lastUnitPositions.ContainsKey(unit))
                {
                    lastUnitPositions.Remove(unit);
                    toClearCurrentVisibleFog = true;
                }
                else
                {
                    if (!lastUnitPositions.ContainsKey(unit) ||
                        Vector3.Distance(lastUnitPositions[unit], unit.UnitGameObject.transform.position) > 1.0f)
                    {
                        foreach (var chunk in activeChunks)
                        {
                            if (chunk.Value.isActiveAndEnabled)
                            {
                                chunk.Value.UpdateUnitData(unit);
                            }
                        }

                        lastUnitPositions[unit] = unit.UnitGameObject.transform.position;
                    }
                }
            }

            var buildings = TownRegistry.UserTown.Buildings;
            foreach (var building in buildings)
            {
                if (building.IsDestroyed && lastBuildingPositions.ContainsKey(building))
                {
                    lastBuildingPositions.Remove(building);
                    toClearCurrentVisibleFog = true;
                }
                else
                {
                    if (!lastBuildingPositions.ContainsKey(building) ||
                        Vector3.Distance(lastBuildingPositions[building], building.BuildingGameObject.transform.position) > 1.0f)
                    {
                        foreach (var chunk in activeChunks)
                        {
                            if (chunk.Value.isActiveAndEnabled)
                            {
                                chunk.Value.UpdateBuildingData(building);
                            }
                        }

                        lastBuildingPositions[building] = building.BuildingGameObject.transform.position;
                    }
                }
            }

            foreach (var chunk in activeChunks)
            {
                chunk.Value.UpdateFadeFog(toClearCurrentVisibleFog);
            }
        }

        private void InitializeResources()
        {
            FogShader = Resources.Load<Shader>(FOG_SHADER_PATH);
            if (FogShader == null)
            {
                Debug.LogError("No fog of war shader at: Resources/" + FOG_SHADER_PATH);
                return;
            }

            FogMaterial = Resources.Load<Material>(FOG_MATERIAL_PATH);
            if (FogMaterial == null)
            {
                Debug.LogWarning("No fog of war material at: Resources/" + FOG_MATERIAL_PATH + "");
                return;
            }

            FogCloudMaterial = Resources.Load<Material>(FOG_CLOUD_PATH);
            if (FogCloudMaterial == null)
            {
                Debug.LogWarning("No fog cloud material at: Resources/" + FOG_CLOUD_PATH + "");
                return;
            }

            WhiteSprite = Resources.Load<Sprite>(FOG_SPRITE_PATH);
            if (WhiteSprite == null)
            {
                Debug.LogWarning("No fog of war sprite at: Resources/" + FOG_SPRITE_PATH);
                return;
            }
        }

        private bool IsChunkVisible(Vector2Int chunkCoord, Vector3 bottomLeft, Vector3 topRight)
        {
            var chunk = chunks[chunkCoord];
            var chunkPosition = chunk.transform.position;

            var chunkLeft = chunkPosition.x - chunk.chunkWidth * MapConfig.CellSize / 2f;
            var chunkRight = chunkPosition.x + chunk.chunkWidth * MapConfig.CellSize / 2f;
            var chunkBottom = chunkPosition.y - chunk.chunkHeight * MapConfig.CellSize / 2f;
            var chunkTop = chunkPosition.y + chunk.chunkHeight * MapConfig.CellSize / 2f;

            if (chunkRight < bottomLeft.x || chunkLeft > topRight.x ||
                chunkTop < bottomLeft.y || chunkBottom > topRight.y)
            {
                return false;
            }

            return true;
        }

        private bool HasUnitsOrBuildingsInChunk(Vector2Int chunkCoords, List<UnitItem> units, List<BuildingItem> buildings)
        {
            var chunk = chunks[chunkCoords];
            var chunkPosition = chunk.transform.position;
            var chunkLeft = chunkPosition.x - chunk.chunkWidth * MapConfig.CellSize / 2f;
            var chunkRight = chunkPosition.x + chunk.chunkWidth * MapConfig.CellSize / 2f;
            var chunkBottom = chunkPosition.y - chunk.chunkHeight * MapConfig.CellSize / 2f;
            var chunkTop = chunkPosition.y + chunk.chunkHeight * MapConfig.CellSize / 2f;

            foreach (var unit in units)
            {
                var unitVisionRadius = unit.Unit.VisionRadius * MapConfig.CellSize;

                var closestX = Mathf.Max(chunkLeft, Mathf.Min(unit.Coords.x, chunkRight));
                var closestY = Mathf.Max(chunkBottom, Mathf.Min(unit.Coords.y, chunkTop));

                var distance = Vector2.Distance(new Vector2(closestX, closestY), unit.Coords);

                if (distance <= unitVisionRadius)
                {
                    return true;
                }
            }

            foreach (var building in buildings)
            {
                var buildingVisionRadius = building.Building.VisionRadius * MapConfig.CellSize;

                var closestX = Mathf.Max(chunkLeft, Mathf.Min(building.BuildingGameObject.transform.position.x, chunkRight));
                var closestY = Mathf.Max(chunkBottom, Mathf.Min(building.BuildingGameObject.transform.position.y, chunkTop));

                var distance = Vector2.Distance(
                    new Vector2(closestX, closestY),
                    new Vector2(building.BuildingGameObject.transform.position.x, building.BuildingGameObject.transform.position.y));

                if (distance <= buildingVisionRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateChunksVisibility(Vector3 bottomLeft, Vector3 topRight)
        {
            var activeUnits = TownRegistry.UserTown.Units;
            var activeBuildings = TownRegistry.UserTown.Buildings;

            var currentTime = Time.time;
            var expiredNeighbors = new List<Vector2Int>();

            foreach (var kvp in neighborChunks)
            {
                if (activeChunks.ContainsKey(kvp.Key))
                {
                    if (kvp.Value + neighborChunkTimeout < currentTime)
                    {
                        var isVisible = IsChunkVisible(kvp.Key, bottomLeft, topRight);
                        if (!isVisible)
                        {
                            var hasUnitsOrBuildings = HasUnitsOrBuildingsInChunk(kvp.Key, activeUnits, activeBuildings);
                            if (!hasUnitsOrBuildings)
                            {
                                expiredNeighbors.Add(kvp.Key);
                            }
                        }
                    }
                }
            }

            foreach (var chunkCoords in expiredNeighbors)
            {
                chunks.TryGetValue(chunkCoords, out var chunkComponent);
                DeactivateChunk(chunkComponent);
                activeChunks.Remove(chunkCoords);
                neighborChunks.Remove(chunkCoords);
            }

            var minChunkX = Mathf.FloorToInt((bottomLeft.x - mapOrigin.x) / (chunkSize * MapConfig.CellSize));
            var maxChunkX = Mathf.CeilToInt((topRight.x - mapOrigin.x) / (chunkSize * MapConfig.CellSize));
            var minChunkY = Mathf.FloorToInt((bottomLeft.y - mapOrigin.y) / (chunkSize * MapConfig.CellSize));
            var maxChunkY = Mathf.CeilToInt((topRight.y - mapOrigin.y) / (chunkSize * MapConfig.CellSize));

            minChunkX = Mathf.Max(0, minChunkX);
            maxChunkX = Mathf.Min(Mathf.CeilToInt((float)mapWidth / chunkSize) - 1, maxChunkX);
            minChunkY = Mathf.Max(0, minChunkY);
            maxChunkY = Mathf.Min(Mathf.CeilToInt((float)mapHeight / chunkSize) - 1, maxChunkY);

            var chunksToActivate = new HashSet<Vector2Int>();

            for (var y = minChunkY; y <= maxChunkY; y++)
            {
                for (var x = minChunkX; x <= maxChunkX; x++)
                {
                    var chunkCoords = new Vector2Int(x, y);

                    var isVisible = IsChunkVisible(chunkCoords, bottomLeft, topRight);
                    if (isVisible)
                    {
                        chunksToActivate.Add(chunkCoords);
                        AddNeighborChunks(chunkCoords, chunksToActivate);
                    }
                    else
                    {
                        var hasUnitsOrBuildings = HasUnitsOrBuildingsInChunk(chunkCoords, activeUnits, activeBuildings);
                        if (hasUnitsOrBuildings)
                        {
                            chunksToActivate.Add(chunkCoords);
                            AddNeighborChunks(chunkCoords, chunksToActivate);
                        }
                    }
                }
            }

            foreach (var chunkCoords in chunksToActivate)
            {
                if (chunks.TryGetValue(chunkCoords, out var chunkComponent) && !activeChunks.ContainsKey(chunkCoords))
                {
                    if (activeChunks.TryAdd(chunkCoords, chunkComponent))
                    {
                        ActivateChunk(chunkComponent);
                        neighborChunks.Add(chunkCoords, currentTime);
                    }
                }
            }
        }

        private void AddNeighborChunks(Vector2Int chunkCoords, HashSet<Vector2Int> chunksToActivate)
        {
            var firstLevelOffsets = new[]
            {
                new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
                new Vector2Int(-1, 0),                         new Vector2Int(1, 0),
                new Vector2Int(-1, 1),  new Vector2Int(0, 1),  new Vector2Int(1, 1)
            };

            var secondLevelOffsets = new[]
            {
                new Vector2Int(-2, -2), new Vector2Int(-1, -2), new Vector2Int(0, -2), new Vector2Int(1, -2), new Vector2Int(2, -2),
                new Vector2Int(-2, -1),                                                                        new Vector2Int(2, -1),
                new Vector2Int(-2, 0),                                                                         new Vector2Int(2, 0),
                new Vector2Int(-2, 1),                                                                         new Vector2Int(2, 1),
                new Vector2Int(-2, 2),  new Vector2Int(-1, 2),  new Vector2Int(0, 2),  new Vector2Int(1, 2),  new Vector2Int(2, 2)
            };

            foreach (var offset in firstLevelOffsets)
            {
                var neighborCoords = new Vector2Int(chunkCoords.x + offset.x, chunkCoords.y + offset.y);

                if (IsValidChunkCoords(neighborCoords))
                {
                    if (chunks.ContainsKey(neighborCoords))
                    {
                        chunksToActivate.Add(neighborCoords);
                    }
                }
            }

            foreach (var offset in secondLevelOffsets)
            {
                var neighborCoords = new Vector2Int(chunkCoords.x + offset.x, chunkCoords.y + offset.y);

                if (IsValidChunkCoords(neighborCoords))
                {
                    if (chunks.ContainsKey(neighborCoords))
                    {
                        chunksToActivate.Add(neighborCoords);
                    }
                }
            }
        }

        private bool IsValidChunkCoords(Vector2Int coords)
        {
            return coords.x >= 0 && coords.x < mapWidth &&
                   coords.y >= 0 && coords.y < mapHeight;
        }

        private Texture2D GetOrCreatePerlinNoiseTexture(int width, int height, float scale)
        {
            var key = (new Vector2Int(width, height), scale);

            if (!_noiseTextureCache.TryGetValue(key, out var cachedTexture))
            {
                var offsetX = Random.Range(0f, 1000f);
                var offsetY = Random.Range(0f, 1000f);
                cachedTexture = new Texture2D(width, height, TextureFormat.R16, false);
                cachedTexture.wrapMode = TextureWrapMode.Repeat;
                var pixels = new Color[width * height];

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var sample = TileablePerlin(x, y, width, height, scale, offsetX, offsetY);
                        pixels[y * width + x] = new Color(sample, 0, 0, 1);
                    }
                }

                cachedTexture.SetPixels(pixels);
                cachedTexture.Apply();

                _noiseTextureCache.Add(key, cachedTexture);
            }

            return cachedTexture;
        }

        private float TileablePerlin(float x, float y, float width, float height, float scale, float offsetX, float offsetY)
        {
            var u = x / width;
            var v = y / height;

            var x0 = x * scale + offsetX;
            var x1 = (x - width) * scale + offsetX;
            var y0 = y * scale + offsetY;
            var y1 = (y - height) * scale + offsetY;

            var a = Mathf.PerlinNoise(x0, y0);
            var b = Mathf.PerlinNoise(x1, y0);
            var c = Mathf.PerlinNoise(x0, y1);
            var d = Mathf.PerlinNoise(x1, y1);

            var ab = Mathf.Lerp(a, b, u);
            var cd = Mathf.Lerp(c, d, u);

            return Mathf.Lerp(ab, cd, v);
        }

        private Vector2 GetRandomDirection(float min = -0.2f, float max = 0.2f)
        {
            return new Vector2(Random.Range(min, max), Random.Range(min, max));
        }

        private void OnCameraMove(CameraMoveEventArgs args)
        {
            UpdateChunksVisibility(args.BottomLeft, args.TopRight);
        }

        private void OnDestroy()
        {
            CameraManager.UnsubscribeFromCameraMove(OnCameraMove);
        }

        private void SaveTextureToPNG(Texture2D texture, string fileName)
        {
            try
            {
                var bytes = texture.EncodeToPNG();

                var directoryPath = Path.Combine(Application.persistentDataPath, "PerlinTextures");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var filePath = Path.Combine(directoryPath, fileName + ".png");

                File.WriteAllBytes(filePath, bytes);

                Debug.Log("Текстуру збережено у: " + filePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Помилка при збереженні текстури: " + e.Message);
            }
        }
    }
}
