using System.Collections.Generic;
using System.IO;
using Town;
using UnityEngine;

namespace FogOfWar
{
    public class FogOfWarDisplay : MonoBehaviour
    {
        private int chunkSize = 100;

        private int mapWidth;
        private int mapHeight;
        private Vector3 mapOrigin;

        private Dictionary<Vector2Int, FogOfWarChunk> chunks = new();
        private Dictionary<UnitItem, Vector3> lastUnitPositions = new();

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

            var perlinTextureWidth = width * (int)MapConfig.CellSize;
            var perlinTextureHeight = width * (int)MapConfig.CellSize;

            var noiseTexture1 = GetOrCreatePerlinNoiseTexture(perlinTextureWidth, perlinTextureHeight, 1f);
            var noiseTexture2 = GetOrCreatePerlinNoiseTexture(perlinTextureWidth, perlinTextureWidth, 2f);
            var noiseTexture3 = GetOrCreatePerlinNoiseTexture(perlinTextureWidth, perlinTextureWidth, 3f);

            chunkComponent.StartFog(position, scale, width, height, noiseTexture1, noiseTexture2, noiseTexture3, direction);

            chunks.Add(chunkCoords, chunkComponent);
            chunkObject.SetActive(true);
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
                        foreach (var chunk in chunks)
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
            
            foreach (var chunk in chunks)
            {
                //chunk.Value.UpdateFadeFog(toClearCurrentVisibleFog);
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
