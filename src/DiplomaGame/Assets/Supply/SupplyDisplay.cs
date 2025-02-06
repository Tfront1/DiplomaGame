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

        public static void DisplayMap(int[,] map, MapGrid<BuildingGridObject> grid)
        {
            for (var x = 0; x < MapConfig.MapWidth; x++)
            {
                for (var y = 0; y < MapConfig.MapHeight; y++)
                {
                    if (map[x, y] == -1) continue;
                    var resourceId = System.Guid.NewGuid();
                    var newResourceObject = CreateBuildingGameObject(SuppliesConfig.Supplies.Find(ob => ob.Id == map[x, y]).Name);
                    var gridPosition = new Vector2Int(x, y);
                    var texture = SupplyTexturesConfig.SupplyTextures.Find(ob => ob.Id == map[x, y]).Texture;

                    SetupResourceSprite(newResourceObject, texture);
                    //PlaceBuildingInGrid(gridPosition, resourceId);
                    SetBuildingPosition(newResourceObject, gridPosition, texture, grid);
                    SetupBuildingCollider(newResourceObject, gridPosition, texture, grid);
                }
            }
        }

        private static GameObject CreateBuildingGameObject(string buildingName)
        {
            return new GameObject(buildingName);
        }
        private static void SetupResourceSprite(GameObject buildingObject, Texture2D spriteTexture)
        {
            var renderer = buildingObject.AddComponent<SpriteRenderer>();
            var newBuildingSprite = Sprite.Create(
            spriteTexture,
            new Rect(0.0f, 0.0f, spriteTexture.width, spriteTexture.height),
                Vector2.zero
            );
            renderer.sprite = newBuildingSprite;

            var (finalScale, _) = CalculateBuildingScale(spriteTexture);
            buildingObject.transform.localScale = new Vector3(finalScale, finalScale, 1);
        }

        private static (float finalScale, Vector2 objectSize) CalculateBuildingScale(Texture2D texture)
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

        //private static void PlaceBuildingInGrid(Vector2Int gridPosition, System.Guid buildingId)
        //{
        //    for (var x = gridPosition.x; x < gridPosition.x + selectedBuilding.WidthCell; x++)
        //    {
        //        for (var y = gridPosition.y; y < gridPosition.y + selectedBuilding.HeightCell; y++)
        //        {
        //            var buildingGridObject = new BuildingGridObject(grid, x, y, buildingId);
        //            var worldPosition = grid.GetWorldPosition(x, y);
        //            grid.SetGridObject(worldPosition, buildingGridObject);
        //        }
        //    }
        //}

        private static void SetBuildingPosition(GameObject buildingObject, Vector2Int gridPosition, Texture2D texture, MapGrid<BuildingGridObject> grid)
        {
            var worldPosition = grid.GetWorldPosition(gridPosition.x, gridPosition.y);
            var (_, objectSize) = CalculateBuildingScale(texture);

            var offset = CalculateOffset(objectSize);
            worldPosition.x += offset.x;
            worldPosition.y += offset.y;
            worldPosition.z = CalculateZOffset(gridPosition);

            buildingObject.transform.position = worldPosition;
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

        private static void SetupBuildingCollider(GameObject buildingObject, Vector2Int gridPosition, Texture2D texture, MapGrid<BuildingGridObject> grid)
        {
            var collider = buildingObject.AddComponent<BoxCollider2D>();
            var (finalScale, _) = CalculateBuildingScale(texture);

            var colliderWidth = MapConfig.CellSize;
            var colliderHeight = MapConfig.CellSize;
            collider.size = new Vector2(colliderWidth / finalScale, colliderHeight / finalScale);

            var colliderPosition = grid.GetWorldPosition(gridPosition.x, gridPosition.y);
            var buildingPosition = buildingObject.transform.position;
            var colliderOffset = CalculateColliderOffset(buildingPosition, colliderPosition, finalScale);
            collider.offset = colliderOffset;
        }

        private static Vector2 CalculateColliderOffset(Vector3 buildingPosition, Vector3 colliderPosition, float finalScale)
        {
            var offset = new Vector2(buildingPosition.x - colliderPosition.x, buildingPosition.y - colliderPosition.y);
            return new Vector2(
                MapConfig.CellSize / 2 / finalScale - offset.x / finalScale,
                MapConfig.CellSize / 2 / finalScale - offset.y / finalScale
            );
        }
    }
}
