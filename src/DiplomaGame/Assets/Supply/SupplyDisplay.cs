using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Supplies
{
    internal class SupplyDisplay
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
        /// Reference to parent GameObject that organizes all Supply objects in hierarchy
        /// </summary>
        private static Transform _SuppliesFolder;

        public static void InitializeCaches()
        {
            _suppliesCache = SuppliesConfig.Supplies.ToDictionary(s => s.Id);
            _texturesCache = SupplyTexturesConfig.SupplyTextures.ToDictionary(t => t.SupplyId, t => t.Texture);
        }

        public static void DisplayMap(int[,] map, MapGrid<BuildingGridObject> grid)
        {
            InitializeCaches();

            for (var x = 0; x < MapConfig.MapWidth; x++)
            {
                for (var y = 0; y < MapConfig.MapHeight; y++)
                {
                    if (map[x, y] == -1) continue;

                    var supplyId = map[x, y];
                    var resourceId = System.Guid.NewGuid();

                    var supplyName = _suppliesCache[supplyId].Name;
                    var texture = _texturesCache[supplyId];

                    var newResourceObject = CreateSupplyGameObject(supplyName);
                    var gridPosition = new Vector2Int(x, y);

                    SetupResourceSprite(newResourceObject, texture);
                    SetSupplyPosition(newResourceObject, gridPosition, texture, grid);
                    SetupSupplyCollider(newResourceObject, gridPosition, texture, grid);
                }
            }
        }

        private static Transform GetSuppliesFolder()
        {
            if (_SuppliesFolder != null) return _SuppliesFolder;
            var folderGO = GameObject.Find("Supplies");
            if (folderGO == null)
            {
                folderGO = new GameObject("Supplies");
            }
            _SuppliesFolder = folderGO.transform;
            return _SuppliesFolder;
        }

        private static GameObject CreateSupplyGameObject(string SupplyName)
        {
            var Supply = new GameObject(SupplyName);
            Supply.transform.SetParent(GetSuppliesFolder());
            return Supply;
        }

        private static void SetupResourceSprite(GameObject SupplyObject, Texture2D spriteTexture)
        {
            var renderer = SupplyObject.AddComponent<SpriteRenderer>();

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

            var (finalScale, _) = CalculateSupplyScale(spriteTexture);
            SupplyObject.transform.localScale = new Vector3(finalScale, finalScale, 1);
        }

        private static (float finalScale, Vector2 objectSize) CalculateSupplyScale(Texture2D texture)
        {
            var margin = MapConfig.CellSize * _defaultOffset;

            var textureUnitWidth = texture.width / _unitPerCell;
            var textureUnitHeight = texture.height / _unitPerCell;

            var totalVisualWidthInUnits = MapConfig.CellSize - (margin * 2);
            var totalVisualHeightInUnits = MapConfig.CellSize - (margin * 2);

            var scaleToFitCellX = totalVisualWidthInUnits / textureUnitWidth;
            var scaleToFitCellY = totalVisualHeightInUnits / textureUnitHeight;

            var baseScale = Mathf.Min(scaleToFitCellX, scaleToFitCellY);
            var finalScale = baseScale;

            return (finalScale, new Vector2(textureUnitWidth * finalScale, textureUnitHeight * finalScale));
        }

        //private static void PlaceSupplyInGrid(Vector2Int gridPosition, System.Guid SupplyId)
        //{
        //    for (var x = gridPosition.x; x < gridPosition.x + selectedSupply.WidthCell; x++)
        //    {
        //        for (var y = gridPosition.y; y < gridPosition.y + selectedSupply.HeightCell; y++)
        //        {
        //            var SupplyGridObject = new SupplyGridObject(grid, x, y, SupplyId);
        //            var worldPosition = grid.GetWorldPosition(x, y);
        //            grid.SetGridObject(worldPosition, SupplyGridObject);
        //        }
        //    }
        //}

        private static void SetSupplyPosition(GameObject SupplyObject, Vector2Int gridPosition, Texture2D texture, MapGrid<BuildingGridObject> grid)
        {
            var worldPosition = grid.GetWorldPosition(gridPosition.x, gridPosition.y);
            var (_, objectSize) = CalculateSupplyScale(texture);

            var offset = CalculateOffset(objectSize);
            worldPosition.x += offset.x;
            worldPosition.y += offset.y;
            worldPosition.z = CalculateZOffset(gridPosition);

            SupplyObject.transform.position = worldPosition;
        }

        private static Vector2 CalculateOffset(Vector2 objectSize)
        {
            var margin = MapConfig.CellSize * _defaultOffset;

            var availableWidth = MapConfig.CellSize - margin * 2;
            var availableHeight = MapConfig.CellSize - margin * 2;

            var defaultOffsetX = margin + (availableWidth - objectSize.x) / 2;
            var defaultOffsetY = margin + (availableHeight - objectSize.y) / 2;

            return new Vector2(defaultOffsetX, defaultOffsetY);
        }

        private static float CalculateZOffset(Vector2Int gridPosition)
        {
            return (MapConfig.MapHeight * MapConfig.CellSize - gridPosition.y) * -0.001f;
        }

        private static void SetupSupplyCollider(GameObject SupplyObject, Vector2Int gridPosition, Texture2D texture, MapGrid<BuildingGridObject> grid)
        {
            var collider = SupplyObject.AddComponent<BoxCollider2D>();
            var (finalScale, _) = CalculateSupplyScale(texture);

            var colliderWidth = MapConfig.CellSize;
            var colliderHeight = MapConfig.CellSize;
            collider.size = new Vector2(colliderWidth / finalScale, colliderHeight / finalScale);

            var colliderPosition = grid.GetWorldPosition(gridPosition.x, gridPosition.y);
            var SupplyPosition = SupplyObject.transform.position;
            var colliderOffset = CalculateColliderOffset(SupplyPosition, colliderPosition, finalScale);
            collider.offset = colliderOffset;
        }

        private static Vector2 CalculateColliderOffset(Vector3 SupplyPosition, Vector3 colliderPosition, float finalScale)
        {
            var offset = new Vector2(SupplyPosition.x - colliderPosition.x, SupplyPosition.y - colliderPosition.y);
            return new Vector2(
                MapConfig.CellSize / 2 / finalScale - offset.x / finalScale,
                MapConfig.CellSize / 2 / finalScale - offset.y / finalScale
            );
        }
    }
}
