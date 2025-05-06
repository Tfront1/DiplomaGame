using System.Collections.Generic;
using System.IO;
using UnityEngine;
using File = System.IO.File;

namespace FogOfWar
{
    public class FogOfWarChunk : MonoBehaviour
    {
        public Vector3 chunkWorldPosition;
        public Vector3 fogScale;
        public int chunkWidth;
        public int chunkHeight;
        public Vector2 _fogDirection;

        private Material fogMaterial;
        private RenderTexture fogRenderTexture;
        private Shader fogShader;
        private Material fogBlendMaterial;
        private SpriteRenderer fogSpriteRenderer;
        private RenderTexture currentVisibilityTexture;

        private GameObject highGameObject;
        private RenderTexture highLayer = null;
        private SpriteRenderer highFogSpriteRenderer;
        private Sprite highFogSprite;
        private Material highMaterial;
        private Texture2D highTexture;

        private GameObject midGameObject;
        private RenderTexture midLayer = null;
        private SpriteRenderer midFogSpriteRenderer;
        private Sprite midFogSprite;
        private Material midMaterial;
        private Texture2D midTexture;

        private GameObject botGameObject;
        private RenderTexture botLayer = null;
        private SpriteRenderer botFogSpriteRenderer;
        private Sprite botFogSprite;
        private Material botMaterial;
        private Texture2D botTexture;

        private Material fogCloudMaterial;

        private HashSet<UnitItem> _units = new();
        private HashSet<BuildingItem> _buildings = new();

        public void SetupFog(Vector3 coords, Vector3 scale, int width, int height, Vector2 direction)
        {
            chunkWorldPosition = coords;
            fogScale = scale;
            chunkWidth = width;
            chunkHeight = height;
            _fogDirection = direction;

            InitializeResources();
        }
        
        public void StartFog(Texture2D noise1, Texture2D noise2, Texture2D noise3)
        {
            gameObject.SetActive(true);

            botTexture = noise1;
            midTexture = noise2;
            highTexture = noise3;

            SetupVariables();
            SetupFogDisplay();
            SetupFogLayers();

            highGameObject?.SetActive(true);
            midGameObject?.SetActive(true);
            botGameObject?.SetActive(true);

        }

        public void StopFog()
        {
            _units.Clear();
            _buildings.Clear();

            if (fogSpriteRenderer != null)
            {
                fogSpriteRenderer.material = null;
            }

            if (highFogSpriteRenderer != null)
            {
                highFogSpriteRenderer.material = null;
                highGameObject?.SetActive(false);
            }

            if (midFogSpriteRenderer != null)
            {
                midFogSpriteRenderer.material = null;
                midGameObject?.SetActive(false);
            }

            if (botFogSpriteRenderer != null)
            {
                botFogSpriteRenderer.material = null;
                botGameObject?.SetActive(false);
            }

            ReleaseRenderTexture(ref fogRenderTexture);
            ReleaseRenderTexture(ref currentVisibilityTexture);
            ReleaseRenderTexture(ref highLayer);
            ReleaseRenderTexture(ref midLayer);
            ReleaseRenderTexture(ref botLayer);

            gameObject.SetActive(false);
        }

        public void UpdateUnitData(UnitItem unit)
        {
            UpdateFogOfWar(unit);
        }

        public void UpdateFadeFog(bool toClearCurrentVisibleFog)
        {
            if (toClearCurrentVisibleFog)
            {
                ClearCurrentVisibility();
            }
            FadeFog();
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

        private void InitializeResources()
        {
            fogShader = FogOfWarDisplay.Instance.FogShader;
            fogMaterial = FogOfWarDisplay.Instance.FogMaterial;
            fogCloudMaterial = FogOfWarDisplay.Instance.FogCloudMaterial;

            fogBlendMaterial = new Material(fogShader);
            fogMaterial = new Material(fogMaterial);
        }

        private void SetupFogDisplay()
        {
            fogSpriteRenderer = GetComponent<SpriteRenderer>();
            if (fogSpriteRenderer == null)
            {
                fogSpriteRenderer = gameObject.AddComponent<SpriteRenderer>();

                fogSpriteRenderer.sprite = FogOfWarDisplay.Instance.WhiteSprite;
            }

            fogSpriteRenderer.transform.localScale = fogScale;
            fogSpriteRenderer.transform.position = chunkWorldPosition;

            fogSpriteRenderer.material = fogMaterial;

            fogSpriteRenderer.sortingOrder = 1;
        }

        private void SetupVariables()
        {
            fogRenderTexture = new RenderTexture(chunkWidth * (int)MapConfig.CellSize,
                chunkHeight * (int)MapConfig.CellSize,
                0,
                RenderTextureFormat.R16);
            fogRenderTexture.Create();

            currentVisibilityTexture = new RenderTexture(chunkWidth * (int)MapConfig.CellSize,
                chunkHeight * (int)MapConfig.CellSize,
                0,
                RenderTextureFormat.R16);
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
            if (highGameObject == null)
            {
                highGameObject = CreateFogLayerObject("HighLayer");
            }
            if (highFogSpriteRenderer == null)
            {
                highFogSpriteRenderer = highGameObject.GetComponent<SpriteRenderer>();

            }
            SetupSpriteRenderer(highFogSpriteRenderer, ref highLayer, ref highFogSprite, highTexture,  4, new Color(0.3f, 0.3f, 0.3f, 1f));
            highMaterial = highFogSpriteRenderer.material;

            if (midGameObject == null)
            {
                midGameObject = CreateFogLayerObject("MidLayer");
            }
            if (midFogSpriteRenderer == null)
            {
                midFogSpriteRenderer = midGameObject.GetComponent<SpriteRenderer>();

            }
            SetupSpriteRenderer(midFogSpriteRenderer, ref midLayer, ref midFogSprite, midTexture, 3, new Color(0.2f, 0.2f, 0.2f, 1f));
            midMaterial = midFogSpriteRenderer.material;

            if (botGameObject == null)
            {
                botGameObject = CreateFogLayerObject("BotLayer");
            }
            if (botFogSpriteRenderer == null)
            {
                botFogSpriteRenderer = botGameObject.GetComponent<SpriteRenderer>();
            }
            SetupSpriteRenderer(botFogSpriteRenderer, ref botLayer, ref botFogSprite, botTexture, 2, new Color(0.1f, 0.1f, 0.1f, 1f));
            botMaterial = botFogSpriteRenderer.material;
        }

        private GameObject CreateFogLayerObject(string nameGO)
        {
            var fogObject = new GameObject(nameGO);

            fogObject.transform.SetParent(transform);

            fogObject.AddComponent<SpriteRenderer>();

            var scaleX = 100f / (chunkWidth * MapConfig.CellSize);
            var scaleY = 100f / (chunkHeight * MapConfig.CellSize);
            fogObject.transform.localScale = new Vector3(scaleX, scaleY, 1); fogObject.transform.localPosition = new Vector3(0, 0, 0);

            return fogObject;
        }

        private void SetupSpriteRenderer(SpriteRenderer spriteRenderer, ref RenderTexture renderTexture, ref Sprite sprite, Texture2D texture, int sortOrder, Color layerColor)
        {
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(texture.width, texture.height, 0, RenderTextureFormat.R16);
                renderTexture.wrapMode = TextureWrapMode.Repeat;
                renderTexture.enableRandomWrite = true;
                renderTexture.Create();

                var copyMat = new Material(Shader.Find("Hidden/CopyRedChannel"));
                Graphics.Blit(texture, renderTexture, copyMat);
            }

            if (sprite == null)
            {
                sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100.0f
                );
            }

            spriteRenderer.sprite = sprite;

            spriteRenderer.sortingOrder = sortOrder;

            var material = new Material(fogCloudMaterial);

            material.SetTexture("_MainTex", renderTexture);
            material.SetTexture("_FogTex", fogRenderTexture);
            material.SetColor("_FogColor", layerColor);

            material.SetVector("_LayerSpeed", _fogDirection);
            material.SetFloat("_TimeScale", 0.03f);

            spriteRenderer.material = material;

            var width = chunkWidth * MapConfig.CellSize;
            var height = chunkHeight * MapConfig.CellSize;
            spriteRenderer.size = new Vector2(width, height);
        }

        private void UpdateFogOfWar(UnitItem unit)
        {
            if (unit == null || unit.UnitGameObject == null)
            {
                if(_units.Contains(unit))
                    _units.Remove(unit);
                return;
            }

            var unitVisionRadius = unit.Unit.VisionRadius;

            var playerPos = WorldToUV(unit.UnitGameObject.transform.position);
            var visionRadius = unitVisionRadius * MapConfig.CellSize / (chunkWidth * MapConfig.CellSize);

            if (playerPos.x + visionRadius < 0 || playerPos.x - visionRadius > 1 ||
                playerPos.y + visionRadius < 0 || playerPos.y - visionRadius > 1)
            {
                return;
            }

            _units.Add(unit);

            fogBlendMaterial.SetVector("_PlayerPos", new Vector4(playerPos.x, playerPos.y, 0, 0));
            fogBlendMaterial.SetFloat("_VisionRadius", visionRadius);

            var temp = RenderTexture.GetTemporary(chunkWidth * (int)MapConfig.CellSize,
                chunkHeight * (int)MapConfig.CellSize,
                0,
                RenderTextureFormat.R16);

            var temp2 = RenderTexture.GetTemporary(chunkWidth * (int)MapConfig.CellSize,
                chunkHeight * (int)MapConfig.CellSize,
                0,
                RenderTextureFormat.R16);

            Graphics.Blit(fogRenderTexture, temp, fogBlendMaterial, 0);
            Graphics.Blit(temp, fogRenderTexture);

            Graphics.Blit(temp, temp2);

            CreateVisibilityTexture(temp2);

            RenderTexture.ReleaseTemporary(temp);
            RenderTexture.ReleaseTemporary(temp2);
        }

        public void CreateVisibilityTexture(RenderTexture unitsTexture)
        {
            fogBlendMaterial.SetTexture("_CurrentVisibilityTex", currentVisibilityTexture);
            ClearCurrentVisibility();

            foreach (var unit in _units)
            {
                var playerPos = WorldToUV(unit.UnitGameObject.transform.position);
                var visionRadius = unit.Unit.VisionRadius * MapConfig.CellSize / (chunkWidth * MapConfig.CellSize);

                fogBlendMaterial.SetVector("_PlayerPos", new Vector4(playerPos.x, playerPos.y, 0, 0));
                fogBlendMaterial.SetFloat("_VisionRadius", visionRadius);

                Graphics.Blit(currentVisibilityTexture, unitsTexture, fogBlendMaterial, 1);
                Graphics.Blit(unitsTexture, currentVisibilityTexture);
            }

            foreach (var building in _buildings)
            {
                var buildingPos = WorldToUV(building.BuildingGameObject.transform.position);
                var visionRadius = building.Building.VisionRadius * MapConfig.CellSize / (chunkWidth * MapConfig.CellSize);

                fogBlendMaterial.SetVector("_PlayerPos", new Vector4(buildingPos.x, buildingPos.y, 0, 0));
                fogBlendMaterial.SetFloat("_VisionRadius", visionRadius);

                Graphics.Blit(currentVisibilityTexture, unitsTexture, fogBlendMaterial, 1);
                Graphics.Blit(unitsTexture, currentVisibilityTexture);
            }
        }

        public void UpdateBuildingData(BuildingItem building)
        {
            if (building == null || building.BuildingGameObject == null)
            {
                if (_buildings.Contains(building))
                    _buildings.Remove(building);
                return;
            }

            var buildingVisionRadius = building.Building.VisionRadius;

            var buildingPos = WorldToUV(building.BuildingGameObject.transform.position);
            var visionRadius = buildingVisionRadius * MapConfig.CellSize / (chunkWidth * MapConfig.CellSize);

            if (buildingPos.x + visionRadius < 0 || buildingPos.x - visionRadius > 1 ||
                buildingPos.y + visionRadius < 0 || buildingPos.y - visionRadius > 1)
            {
                return;
            }

            _buildings.Add(building);

            fogBlendMaterial.SetVector("_PlayerPos", new Vector4(buildingPos.x, buildingPos.y, 0, 0));
            fogBlendMaterial.SetFloat("_VisionRadius", visionRadius);

            var temp = RenderTexture.GetTemporary(chunkWidth * (int)MapConfig.CellSize,
                chunkHeight * (int)MapConfig.CellSize,
                0,
                RenderTextureFormat.R16);

            var temp2 = RenderTexture.GetTemporary(chunkWidth * (int)MapConfig.CellSize,
                chunkHeight * (int)MapConfig.CellSize,
                0,
                RenderTextureFormat.R16);

            Graphics.Blit(fogRenderTexture, temp, fogBlendMaterial, 0);
            Graphics.Blit(temp, fogRenderTexture);

            Graphics.Blit(temp, temp2);

            UpdateCurrentVisibilityWithBuildings(temp2);

            RenderTexture.ReleaseTemporary(temp);
            RenderTexture.ReleaseTemporary(temp2);
        }

        private void UpdateCurrentVisibilityWithBuildings(RenderTexture buildingsTexture)
        {
            fogBlendMaterial.SetTexture("_CurrentVisibilityTex", currentVisibilityTexture);

            foreach (var building in _buildings)
            {
                var buildingPos = WorldToUV(building.BuildingGameObject.transform.position);

                fogBlendMaterial.SetVector("_PlayerPos", new Vector4(buildingPos.x, buildingPos.y, 0, 0));

                Graphics.Blit(currentVisibilityTexture, buildingsTexture, fogBlendMaterial, 1);
                Graphics.Blit(buildingsTexture, currentVisibilityTexture);
            }
        }

        public void ClearCurrentVisibility()
        {
            var rt = RenderTexture.active;
            RenderTexture.active = currentVisibilityTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = rt;
        }

        private void FadeFog()
        {
            var temp = RenderTexture.GetTemporary(fogRenderTexture.width, fogRenderTexture.height, 0,
                RenderTextureFormat.R16);
            Graphics.Blit(fogRenderTexture, temp, fogBlendMaterial, 2);
            Graphics.Blit(temp, fogRenderTexture);
            RenderTexture.ReleaseTemporary(temp);
        }

        private Vector2 WorldToUV(Vector3 worldPos)
        {
            var chunkMapWidth = chunkWidth * MapConfig.CellSize;
            var chunkMapHeight = chunkHeight * MapConfig.CellSize;

            var chunkCornerX = chunkWorldPosition.x - (chunkMapWidth / 2f);
            var chunkCornerY = chunkWorldPosition.y - (chunkMapHeight / 2f);

            var relativeX = worldPos.x - chunkCornerX;
            var relativeY = worldPos.y - chunkCornerY;

            var normalizedX = relativeX / chunkMapWidth;
            var normalizedY = relativeY / chunkMapHeight;

            return new Vector2(normalizedX, normalizedY);
        }

        private void ReleaseRenderTexture(ref RenderTexture texture)
        {
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
                texture = null;
            }
        }

        private void OnDestroy()
        {
            _units.Clear();
            _buildings.Clear();

            if (fogMaterial != null)
                fogMaterial.SetTexture("_FogTex", null);

            if (highMaterial != null)
                highMaterial.SetTexture("_MainTex", null);

            if (midMaterial != null)
                midMaterial.SetTexture("_MainTex", null);

            if (botMaterial != null)
                botMaterial.SetTexture("_MainTex", null);

            ReleaseRenderTexture(ref fogRenderTexture);
            ReleaseRenderTexture(ref currentVisibilityTexture);
            ReleaseRenderTexture(ref highLayer);
            ReleaseRenderTexture(ref midLayer);
            ReleaseRenderTexture(ref botLayer);
        }
    }
}