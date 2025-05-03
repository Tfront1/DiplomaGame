using System.Collections.Generic;
using System.IO;
using Town;
using UnityEngine;

namespace FogOfWar
{
    public class FogOfWarDisplay : MonoBehaviour
    {
        private static FogOfWarDisplay _instance;
        private static readonly object _lock = new();
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

        private float visionRadius = 5f;

        private Material fogMaterial;
        private RenderTexture fogRenderTexture;
        private Shader fogShader;
        private Material fogBlendMaterial;
        private SpriteRenderer fogSpriteRenderer;
        private RenderTexture currentVisibilityTexture;

        private const string FOG_SHADER_PATH = "FogOfWar/Shaders/FogOfWarShader";
        private const string FOG_MATERIAL_PATH = "FogOfWar/Materials/FogOfWarMaterial";
        private const string FOG_SPRITE_PATH = "FogOfWar/Sprites/WhiteSquare";
        private const string FOG_CLOUD_PATH = "FogOfWar/Materials/FogCloudOverlay";

        private Dictionary<UnitItem, Vector3> lastUnitPositions = new();

        private RenderTexture highLayer;
        private SpriteRenderer highFogSpriteRenderer;
        private Material highMaterial;
        private Texture2D highTexture;

        private RenderTexture midLayer;
        private SpriteRenderer midFogSpriteRenderer;
        private Material midMaterial;
        private Texture2D midTexture;

        private RenderTexture botLayer;
        private SpriteRenderer botFogSpriteRenderer;
        private Material botMaterial;
        private Texture2D botTexture;

        private Material fogCloudMaterial;

        public void StartFog()
        {
            InitializeResources();
            SetupVariables();
            SetupFogDisplay();
            SetupFogLayers();
        }

        private void InitializeResources()
        {
            fogShader = Resources.Load<Shader>(FOG_SHADER_PATH);
            if (fogShader == null)
            {
                Debug.LogError("No fog of war shader at: Resources/" + FOG_SHADER_PATH);
                return;
            }

            fogMaterial = Resources.Load<Material>(FOG_MATERIAL_PATH);
            if (fogMaterial == null)
            {
                Debug.LogWarning("No fog of war material at: Resources/" + FOG_MATERIAL_PATH + "");
                return;
            }

            fogCloudMaterial = Resources.Load<Material>(FOG_CLOUD_PATH);
            if (fogMaterial == null)
            {
                Debug.LogWarning("No fog cloud material at: Resources/" + FOG_CLOUD_PATH + "");
                return;
            }

            fogBlendMaterial = new Material(fogShader);
        }

        private void SetupFogDisplay()
        {
            fogSpriteRenderer = GetComponent<SpriteRenderer>();
            if (fogSpriteRenderer == null)
            {
                fogSpriteRenderer = gameObject.AddComponent<SpriteRenderer>();

                var whiteSquare = Resources.Load<Sprite>(FOG_SPRITE_PATH);
                if (whiteSquare == null)
                {
                    Debug.LogWarning("No fog of war sprite at: Resources/" + FOG_SPRITE_PATH);
                    return;
                }
               
                fogSpriteRenderer.sprite = whiteSquare;
            }

            fogSpriteRenderer.transform.localScale = new Vector3(MapConfig.MapWidth * MapConfig.CellSize,
                MapConfig.MapHeight * MapConfig.CellSize, 1);
            fogSpriteRenderer.transform.position = new Vector3(
                MapConfig.MapStartPointX + MapConfig.MapWidth * MapConfig.CellSize / 2,
                MapConfig.MapStartPointY + MapConfig.MapHeight * MapConfig.CellSize / 2,
                -25);
            fogSpriteRenderer.material = fogMaterial;

            fogSpriteRenderer.sortingOrder = 1;
        }

        private void SetupVariables()
        {
            fogRenderTexture = new RenderTexture(MapConfig.MapWidth * (int)MapConfig.CellSize,
                MapConfig.MapHeight * (int)MapConfig.CellSize, 0, RenderTextureFormat.R16);
            fogRenderTexture.Create();

            currentVisibilityTexture = new RenderTexture(MapConfig.MapWidth * (int)MapConfig.CellSize,
                MapConfig.MapHeight * (int)MapConfig.CellSize, 0, RenderTextureFormat.R16);
            currentVisibilityTexture.Create();

            Graphics.Blit(null, fogRenderTexture,
                new Material(Shader.Find("Unlit/Color")) { color = Color.white });
            Graphics.Blit(null, currentVisibilityTexture,
                new Material(Shader.Find("Unlit/Color")) { color = Color.black });

            fogMaterial.SetTexture("_FogTex", fogRenderTexture);

            fogMaterial.SetColor("_FogColor", new Color(0.2f, 0.2f, 0.2f, 1.0f));

            fogBlendMaterial.SetTexture("_CurrentVisibilityTex", currentVisibilityTexture);
            fogBlendMaterial.SetFloat("_FadeSpeed", 0.05f);
            fogBlendMaterial.SetTexture("_MainTex", fogRenderTexture);
        }

        private void SetupFogLayers()
        {
            highLayer = CreatePerlinNoiseTexture(3f, out highTexture);
            midLayer = CreatePerlinNoiseTexture(2f,out midTexture);
            botLayer = CreatePerlinNoiseTexture(1f,out botTexture);

            var highFogObject = CreateFogLayerObject("HighLayer");
            highFogSpriteRenderer = highFogObject.GetComponent<SpriteRenderer>();
            SetupSpriteRenderer(highFogSpriteRenderer, highLayer, highTexture, 3, new Color(0.3f, 0.3f, 0.3f, 1f));
            highMaterial = highFogSpriteRenderer.material;
            
            var midFogObject = CreateFogLayerObject("MidLayer");
            midFogSpriteRenderer = midFogObject.GetComponent<SpriteRenderer>();
            SetupSpriteRenderer(midFogSpriteRenderer, midLayer, midTexture, 2, new Color(0.2f, 0.2f, 0.2f, 1f));
            midMaterial = midFogSpriteRenderer.material;

            var botFogObject = CreateFogLayerObject("BotLayer");
            botFogSpriteRenderer = botFogObject.GetComponent<SpriteRenderer>();
            SetupSpriteRenderer(botFogSpriteRenderer, botLayer, botTexture, 1, new Color(0.1f, 0.1f, 0.1f, 1f));
            botMaterial = botFogSpriteRenderer.material;
        }

        private GameObject CreateFogLayerObject(string nameGO)
        {
            var fogObject = new GameObject(nameGO);

            fogObject.transform.SetParent(this.transform);

            fogObject.AddComponent<SpriteRenderer>();
            
            fogObject.transform.localScale = new Vector3(0.1f, 0.1f, 1);
            fogObject.transform.localPosition = new Vector3(0, 0, 0);
            
            return fogObject;
        }

        private void SetupSpriteRenderer(SpriteRenderer spriteRenderer, RenderTexture renderTexture, Texture2D texture, int sortOrder, Color layerColor)
        {
            var newSprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100.0f
            );

            spriteRenderer.sprite = newSprite;

            spriteRenderer.sortingOrder = sortOrder;

            var material = new Material(fogCloudMaterial);

            material.SetTexture("_MainTex", renderTexture);
            material.SetTexture("_FogTex", fogRenderTexture);
            material.SetColor("_FogColor", layerColor);

            material.SetVector("_LayerSpeed", GetRandomDirection());
            material.SetFloat("_TimeScale", 0.03f);

            spriteRenderer.material = material;

            var width = MapConfig.MapWidth * MapConfig.CellSize;
            var height = MapConfig.MapHeight * MapConfig.CellSize;
            spriteRenderer.size = new Vector2(width, height);
        }

        private Vector2 GetRandomDirection(float min = -0.2f, float max = 0.2f)
        {
            return new Vector2(Random.Range(min, max), Random.Range(min, max));
        }

        private void Update()
        {
            var units = TownRegistry.UserTown.Units;
            var toSetupCurrentVisibleFog = true;
            foreach (var unit in units)
            {
                if (unit.IsDestroyed && lastUnitPositions.ContainsKey(unit))
                {
                    lastUnitPositions.Remove(unit);
                }
                else
                {
                    if (!lastUnitPositions.ContainsKey(unit) ||
                        Vector3.Distance(lastUnitPositions[unit], unit.UnitGameObject.transform.position) > 0.1f)
                    {
                        if (toSetupCurrentVisibleFog)
                        {
                            ClearCurrentVisibility();
                            toSetupCurrentVisibleFog = false;
                        }

                        UpdateFogOfWar(unit);
                        lastUnitPositions[unit] = unit.UnitGameObject.transform.position;
                    }
                }
            }

            FadeFog();
        }

        private void UpdateFogOfWar(UnitItem unit)
        {
            if (unit == null || unit.UnitGameObject == null)
                return;

            var playerPos = WorldToUV(unit.UnitGameObject.transform.position);

            fogBlendMaterial.SetVector("_PlayerPos", new Vector4(playerPos.x, playerPos.y, 0, 0));
            fogBlendMaterial.SetFloat("_VisionRadius", visionRadius * MapConfig.CellSize / fogSpriteRenderer.bounds.size.x);

            var temp = RenderTexture.GetTemporary(MapConfig.MapWidth * (int)MapConfig.CellSize,
                MapConfig.MapHeight * (int)MapConfig.CellSize, 0, RenderTextureFormat.R16);
            Graphics.Blit(fogRenderTexture, temp, fogBlendMaterial, 0);
            Graphics.Blit(temp, fogRenderTexture);
            
            Graphics.Blit(currentVisibilityTexture, temp, fogBlendMaterial, 1);
            Graphics.Blit(temp, currentVisibilityTexture);

            RenderTexture.ReleaseTemporary(temp);
        }

        private void ClearCurrentVisibility()
        {
            Graphics.Blit(null, currentVisibilityTexture,
                new Material(Shader.Find("Unlit/Color")) { color = Color.black });

            fogBlendMaterial.SetTexture("_CurrentVisibilityTex", currentVisibilityTexture);
            fogBlendMaterial.SetTexture("_MainTex", fogRenderTexture);
        }

        private void FadeFog()
        {
            var temp = RenderTexture.GetTemporary(fogRenderTexture.width, fogRenderTexture.height, 0, RenderTextureFormat.R16);
            Graphics.Blit(fogRenderTexture, temp, fogBlendMaterial, 2);
            Graphics.Blit(temp, fogRenderTexture);
            RenderTexture.ReleaseTemporary(temp);
        }

        private Vector2 WorldToUV(Vector3 worldPos)
        {
            var mapWidth = MapConfig.MapWidth * MapConfig.CellSize;
            var mapHeight = MapConfig.MapHeight * MapConfig.CellSize;

            var relativeX = worldPos.x - MapConfig.MapStartPointX;
            var relativeY = worldPos.y - MapConfig.MapStartPointY;

            var normalizedX = relativeX / mapWidth;
            var normalizedY = relativeY / mapHeight;

            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);

            return new Vector2(normalizedX, normalizedY);
        }

        private RenderTexture CreatePerlinNoiseTexture(float scale, out Texture2D texture)
        {
            var width = MapConfig.MapWidth * (int)MapConfig.CellSize;
            var height = MapConfig.MapHeight * (int)MapConfig.CellSize;

            var offsetX = Random.Range(0f, 1000f);
            var offsetY = Random.Range(0f, 1000f);

            var tempTexture = new Texture2D(width, height, TextureFormat.R16, false);
            tempTexture.wrapMode = TextureWrapMode.Repeat;

            var pixels = new Color[width * height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    float sample = TileablePerlin(x, y, width, height, scale, offsetX, offsetY);
                    pixels[y * width + x] = new Color(sample, 0, 0, 1);
                }
            }

            tempTexture.SetPixels(pixels);
            tempTexture.Apply();

            var noiseTexture = new RenderTexture(width, height, 0, RenderTextureFormat.R16);
            noiseTexture.wrapMode = TextureWrapMode.Repeat;
            noiseTexture.enableRandomWrite = true;
            noiseTexture.Create();

            var copyMat = new Material(Shader.Find("Hidden/CopyRedChannel"));
            Graphics.Blit(tempTexture, noiseTexture, copyMat);

            texture = tempTexture;

            return noiseTexture;
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

        public static void SaveRenderTextureAsPNG(RenderTexture renderTexture, string fileName)
        {
            // Створюємо тимчасову текстуру для зчитування пікселів з RenderTexture
            var texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

            // Запам'ятовуємо поточну активну RenderTexture
            var previousActive = RenderTexture.active;

            try
            {
                // Встановлюємо нашу RenderTexture як активну
                RenderTexture.active = renderTexture;

                // Зчитуємо пікселі з RenderTexture до Texture2D
                texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                texture2D.Apply();

                // Відновлюємо попередню активну RenderTexture
                RenderTexture.active = previousActive;

                // Конвертуємо текстуру в байти PNG
                var bytes = texture2D.EncodeToPNG();

                var directoryPath = Path.Combine(Application.persistentDataPath, "PerlinTextures");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var filePath = Path.Combine(directoryPath, fileName + ".png");

                // Записуємо байти у файл
                File.WriteAllBytes(filePath, bytes);

                Debug.Log($"RenderTexture успішно збережено як PNG: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Помилка при збереженні RenderTexture як PNG: {e.Message}");
            }
            finally
            {
                // Відновлюємо попередню активну RenderTexture (на випадок помилки)
                RenderTexture.active = previousActive;

                // Знищуємо тимчасову текстуру
                Object.Destroy(texture2D);
            }
        }

        private void OnDestroy()
        {
            if (fogRenderTexture != null)
            {
                fogRenderTexture.Release();
                Destroy(fogRenderTexture);
            }

            if (currentVisibilityTexture != null)
            {
                currentVisibilityTexture.Release();
                Destroy(currentVisibilityTexture);
            }
        }
    }
}
