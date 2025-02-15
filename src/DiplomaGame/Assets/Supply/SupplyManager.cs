using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Supplies
{
    internal class SupplyManager : MonoBehaviour
    {
        /// <summary>
        /// How much percent would be minimum offset
        /// </summary>
        private static readonly float _defaultOffset = 0.05f;

        /// <summary>
        /// How many pixels in a unit of measurement
        /// </summary>
        private static readonly float _unitPerCell = 100.0f;

        /// <summary>
        /// Cache for quick Supply lookup by ID to avoid repeated searches
        /// </summary>
        private static Dictionary<int, Supply> _suppliesCache;

        /// <summary>
        /// Cache for quick Texture lookup by supply ID to avoid repeated searches
        /// </summary>
        private static Dictionary<int, Texture2D> _texturesCache;

        /// <summary>
        /// Cache for sprites to avoid recreating sprites from the same textures
        /// </summary>
        private static Dictionary<Texture2D, Sprite> _spriteCache = new();

        /// <summary>
        /// Cache for supply texture configurations to avoid repeated searches
        /// </summary>
        private static Dictionary<int, SupplyTexture> _supplyTextureConfigCache;

        /// <summary>
        /// Reference to parent GameObject that organizes all Supply objects in hierarchy
        /// </summary>
        private static Transform _suppliesFolder;

        /// <summary>
        /// Initializes supply caches with data from configs
        /// </summary>
        public static void InitializeCaches()
        {
            _suppliesCache = SuppliesConfig.Supplies.ToDictionary(s => s.Id);
            _texturesCache = SupplyTexturesConfig.SupplyTextures.ToDictionary(t => t.SupplyId, t => t.Texture);
            _supplyTextureConfigCache = SupplyTexturesConfig.SupplyTextures.ToDictionary(t => t.SupplyId);
        }

        /// <summary>
        /// Displays supplies on the map based on provided grid data
        /// </summary>
        /// <param name="grid">Target grid for supplies</param>
        /// <param name="supplyInts">Dictionary of supply position and type of supply</param>
        /// <param name="supplyItemsList">List of supplies items</param>
        public static void DisplaySupplyMap(MapGrid<SupplyGridObject> grid, Dictionary<(int, int), int> supplyInts, ItemList<SupplyItem> supplyItemsList)
        {
            InitializeCaches();
            
            foreach (var (position, supplyId) in supplyInts)
            {
                var x = position.Item1;
                var y = position.Item2;

                if (supplyId <= 0) continue;

                var gridPosition = new Vector2Int(x, y);
                var supplyGuid = Guid.NewGuid();
                var supplyTexture = _supplyTextureConfigCache[supplyId];
                var supply = _suppliesCache[supplyId];

                GridRegistry.UpsertGrid(grid);

                if (!GridService.CanPlaceAtPosition(
                        gridPosition,
                        new Vector2Int(supply.WidthCell, supply.HeightCell),
                        GridRegistry.GetAllGridsList().ToArray()) ||
                    !ItemListService.CanPlaceAtPosition(
                        gridPosition,
                        new Vector2Int(supply.WidthCell, supply.HeightCell),
                        ItemListRegistry.GetAllListsItemsList().ToArray()
                    ))
                {
                    //TODO: To do...
                    Debug.Log($"Supply overlaps on object, X: {x}, Y: {y}");
                    continue;
                }

                var supplyName = _suppliesCache[supplyId].Name;
                var texture = _texturesCache[supplyId];
                var newResourceObject = CreateSupplyGameObject(supplyName);

                SetupResourceSprite(newResourceObject, texture, supplyTexture);
                PlaceSupplyInGrid(gridPosition, supplyGuid, grid, supply);
                AddSupplyToList(gridPosition, supplyGuid, supplyItemsList, supply);
                SetSupplyPosition(newResourceObject, gridPosition, texture, grid, supplyTexture);
                SetupSupplyCollider(newResourceObject, gridPosition, texture, grid, supply, supplyTexture);

                ItemListRegistry.UpsertList(supplyItemsList);
            }
        }

        /// <summary>
        /// Gets or creates supplies parent folder
        /// </summary>
        /// <returns>Transform of supplies folder</returns>
        private static Transform GetSuppliesFolder()
        {
            if (_suppliesFolder != null) return _suppliesFolder;
            var folderGO = GameObject.Find("Supplies");
            if (folderGO == null)
            {
                folderGO = new GameObject("Supplies");
            }
            _suppliesFolder = folderGO.transform;
            return _suppliesFolder;
        }

        /// <summary>
        /// Creates a new supply GameObject
        /// </summary>
        /// <param name="supplyName">Name for the supply</param>
        /// <returns>Created supply GameObject</returns>
        private static GameObject CreateSupplyGameObject(string supplyName)
        {
            var supply = new GameObject(supplyName);
            supply.transform.SetParent(GetSuppliesFolder());
            return supply;
        }

        /// <summary>
        /// Sets up sprite renderer and scale for supply
        /// </summary>
        /// <param name="supplyObject">Target supply object</param>
        /// <param name="spriteTexture">Texture for sprite</param>
        /// <param name="supplyTexture">Supply texture config</param>
        private static void SetupResourceSprite(GameObject supplyObject, Texture2D spriteTexture, SupplyTexture supplyTexture)
        {
            var renderer = supplyObject.AddComponent<SpriteRenderer>();

            if (!_spriteCache.TryGetValue(spriteTexture, out var sprite))
            {
                sprite = Sprite.Create(
                    spriteTexture,
                    new Rect(0.0f, 0.0f, spriteTexture.width, spriteTexture.height),
                    Vector2.zero
                );
                _spriteCache[spriteTexture] = sprite;
            }

            renderer.sprite = sprite;

            var (finalScale, _) = CalculateSupplyScale(spriteTexture, supplyTexture);
            supplyObject.transform.localScale = new Vector3(finalScale, finalScale, 1);
        }

        /// <summary>
        /// Calculates supply scale and size with margins
        /// </summary>
        /// <param name="texture">Supply texture</param>
        /// <param name="supplyTexture">Supply texture config</param>
        /// <returns>Scale and size vector</returns>
        private static (float finalScale, Vector2 objectSize) CalculateSupplyScale(Texture2D texture, SupplyTexture supplyTexture)
        {
            var margin = MapConfig.CellSize * _defaultOffset;

            var textureUnitWidth = texture.width / _unitPerCell;
            var textureUnitHeight = texture.height / _unitPerCell;

            var totalVisualWidthInUnits = MapConfig.CellSize * supplyTexture.VisualWidthCell - (margin * 2);
            var totalVisualHeightInUnits = MapConfig.CellSize * supplyTexture.VisualHeightCell - (margin * 2);

            var scaleToFitCellX = totalVisualWidthInUnits / textureUnitWidth;
            var scaleToFitCellY = totalVisualHeightInUnits / textureUnitHeight;

            var baseScale = Mathf.Min(scaleToFitCellX, scaleToFitCellY);
            var finalScale = baseScale;

            return (finalScale, new Vector2(textureUnitWidth * finalScale, textureUnitHeight * finalScale));
        }

        /// <summary>
        /// Places supply in grid and creates grid objects
        /// </summary>
        /// <param name="gridPosition">Position to place at</param>
        /// <param name="supplyGuid">Supply's unique ID</param>
        /// <param name="grid">Target grid</param>
        /// <param name="supply">Supply config</param>
        private static void PlaceSupplyInGrid(Vector2Int gridPosition, Guid supplyGuid, MapGrid<SupplyGridObject> grid, Supply supply)
        {
            for (var x = gridPosition.x; x < gridPosition.x + supply.WidthCell; x++)
            {
                for (var y = gridPosition.y; y < gridPosition.y + supply.HeightCell; y++)
                {
                    var SupplyGridObject = new SupplyGridObject(grid, x, y, supplyGuid);
                    var worldPosition = grid.GetWorldPosition(x, y);
                    grid.SetGridObject(worldPosition, SupplyGridObject);
                }
            }
        }
        
        private static void AddSupplyToList(Vector2Int gridPosition, Guid supplyGuid, ItemList<SupplyItem> supplyItemList, Supply supply)
        {
            var supplyItem = new SupplyItem(gridPosition, supplyGuid, supply);
            supplyItemList.Add(supplyItem);
        }

        /// <summary>
        /// Sets supply position in world space
        /// </summary>
        /// <param name="supplyObject">Supply to position</param>
        /// <param name="gridPosition">Grid position</param>
        /// <param name="texture">Supply texture</param>
        /// <param name="grid">Target grid</param>
        /// <param name="supplyTexture">Supply texture config</param>
        private static void SetSupplyPosition(GameObject supplyObject, Vector2Int gridPosition, Texture2D texture, MapGrid<SupplyGridObject> grid, SupplyTexture supplyTexture)
        {
            var worldPosition = grid.GetWorldPosition(gridPosition.x, gridPosition.y);
            var (_, objectSize) = CalculateSupplyScale(texture, supplyTexture);

            var offset = CalculateOffset(objectSize, supplyTexture);
            worldPosition.x += offset.x;
            worldPosition.y += offset.y;
            worldPosition.z = CalculateZOffset(gridPosition);

            supplyObject.transform.position = worldPosition;
        }

        /// <summary>
        /// Calculates offset for supply position
        /// </summary>
        /// <param name="objectSize">Supply's size</param>
        /// <param name="supplyTexture">Supply texture config</param>
        /// <returns>Offset vector</returns>
        private static Vector2 CalculateOffset(Vector2 objectSize, SupplyTexture supplyTexture)
        {
            var margin = MapConfig.CellSize * _defaultOffset;

            var availableWidth = (MapConfig.CellSize * supplyTexture.VisualWidthCell) - (margin * 2);
            var availableHeight = (MapConfig.CellSize * supplyTexture.VisualHeightCell) - (margin * 2);

            var defaultOffsetX = margin + (availableWidth - objectSize.x) / 2;
            var defaultOffsetY = margin + (availableHeight - objectSize.y) / 2;

            return new Vector2(defaultOffsetX, defaultOffsetY);
        }

        /// <summary>
        /// Calculates Z offset for sprite layering
        /// </summary>
        /// <param name="gridPosition">Grid position</param>
        /// <returns>Z coordinate offset</returns>
        private static float CalculateZOffset(Vector2Int gridPosition)
        {
            return (MapConfig.MapHeight * MapConfig.CellSize - gridPosition.y) * -0.001f;
        }

        /// <summary>
        /// Sets up supply's BoxCollider2D
        /// </summary>
        /// <param name="supplyObject">Target supply</param>
        /// <param name="gridPosition">Grid position</param>
        /// <param name="texture">Supply texture</param>
        /// <param name="grid">Target grid</param>
        /// <param name="supply">Supply config</param>
        /// <param name="supplyTexture">Supply texture config</param>
        private static void SetupSupplyCollider(GameObject supplyObject, Vector2Int gridPosition, Texture2D texture, MapGrid<SupplyGridObject> grid, Supply supply, SupplyTexture supplyTexture)
        {
            var collider = supplyObject.AddComponent<BoxCollider2D>();
            var (finalScale, _) = CalculateSupplyScale(texture, supplyTexture);

            var colliderWidth = MapConfig.CellSize * supply.WidthCell;
            var colliderHeight = MapConfig.CellSize * supply.HeightCell;
            collider.size = new Vector2(colliderWidth / finalScale, colliderHeight / finalScale);

            var colliderPosition = grid.GetWorldPosition(gridPosition.x, gridPosition.y);
            var supplyPosition = supplyObject.transform.position;
            var colliderOffset = CalculateColliderOffset(supplyPosition, colliderPosition, finalScale, supply);
            collider.offset = colliderOffset;
        }

        /// <summary>
        /// Calculates collider offset relative to supply
        /// </summary>
        /// <param name="supplyPosition">Supply's position</param>
        /// <param name="colliderPosition">Base collider position</param>
        /// <param name="finalScale">Supply's scale</param>
        /// <param name="supply">Supply config</param>
        /// <returns>Collider offset vector</returns>
        private static Vector2 CalculateColliderOffset(Vector3 supplyPosition, Vector3 colliderPosition, float finalScale, Supply supply)
        {
            var offset = new Vector2(supplyPosition.x - colliderPosition.x, supplyPosition.y - colliderPosition.y);
            return new Vector2(
                MapConfig.CellSize * supply.WidthCell / 2 / finalScale - offset.x / finalScale,
                MapConfig.CellSize * supply.HeightCell / 2 / finalScale - offset.y / finalScale
            );
        }
    }
}
