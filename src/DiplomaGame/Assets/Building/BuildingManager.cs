using System;
using System.Collections.Generic;
using System.Linq;
using Items.Resource.BackPack;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;

public class BuildingManager : MonoBehaviour
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
    /// Cache for building texture configurations to avoid repeated searches
    /// </summary>
    private static Dictionary<int, BuildingTexture> _buildingTextureConfigCache;

    /// <summary>
    /// Reference to parent GameObject that organizes all Building objects in hierarchy
    /// </summary>
    private static Transform _buildingFolder;

    private static bool _isInitializedCaches = false;

    /// <summary>
    /// Initializes building caches with data from configs
    /// </summary>
    private static void InitializeCaches()
    {
        _buildingTextureConfigCache = BuildingTexturesConfig.BuildingTexture.ToDictionary(t => t.BuildingId);

        _isInitializedCaches = true; 
    }
    
    public static bool Build(Vector2Int gridPosition, Building building, ItemList<BuildingItem> buildingItemList, MapGrid<BuildingGridObject> grid)
    {
        if (!_isInitializedCaches)
        {
            InitializeCaches();
        }

        if (!GridService.CanPlaceAtPosition(
                gridPosition,
                new Vector2Int(building.WidthCell,
                    building.HeightCell),
                GridRegistry.GetAllGridsList().ToArray()) ||
            !ItemListService.CanPlaceAtPosition(
                gridPosition,
                new Vector2Int(building.WidthCell, building.HeightCell),
                ItemListRegistry.GetAllListsItemsList().ToArray()
            ))
        {
            return false;
        }

        var buildingGuid = Guid.NewGuid();
        var newBuildingObject = CreateBuildingGameObject(building.Name);

        SetupBuildingSprite(newBuildingObject, _buildingTextureConfigCache[building.Id], building);
        SetBuildingPosition(newBuildingObject, gridPosition, building, _buildingTextureConfigCache[building.Id]);
        SetupBuildingCollider(newBuildingObject, gridPosition, building, _buildingTextureConfigCache[building.Id]);

        PlaceBuildingInGrid(gridPosition, buildingGuid, grid, building);
        AddBuildingToList(gridPosition, buildingGuid, buildingItemList, building, newBuildingObject);
        GridRegistry.UpsertGrid(grid);
        ItemListRegistry.UpsertList(buildingItemList);

        return true;
    }

    /// <summary>
    /// Gets or creates a parent folder for buildings
    /// </summary>
    /// <returns>Transform of the buildings folder</returns>
    private static Transform GetBuildingsFolder()
    {
        if (_buildingFolder != null) return _buildingFolder;
        var folderGO = GameObject.Find("Buildings");
        if (folderGO == null)
        {
            folderGO = new GameObject("Buildings");
        }
        _buildingFolder = folderGO.transform;
        return _buildingFolder;
    }

    /// <summary>
    /// Creates a new building GameObject
    /// </summary>
    /// <param name="buildingName">Name for the new building</param>
    /// <returns>Created building GameObject</returns>
    private static GameObject CreateBuildingGameObject(string buildingName)
    {
        var building = new GameObject(buildingName);
        building.transform.SetParent(GetBuildingsFolder());
        return building;
    }

    /// <summary>
    /// Sets up sprite renderer and scale for the building
    /// </summary>
    /// <param name="buildingObject">Target building object</param>
    /// <param name="buildingTexture">Texture for the sprite</param>
    /// <param name="building">Building data</param>
    private static void SetupBuildingSprite(GameObject buildingObject, BuildingTexture buildingTexture, Building building)
    {
        var renderer = buildingObject.AddComponent<SpriteRenderer>();
        var newBuildingSprite = Sprite.Create(
            buildingTexture.Texture,
            new Rect(0.0f, 0.0f, buildingTexture.Texture.width, buildingTexture.Texture.height),
            Vector2.zero
        );
        renderer.sprite = newBuildingSprite;

        var (finalScale, _) = CalculateBuildingScale(building, buildingTexture);
        buildingObject.transform.localScale = new Vector3(finalScale, finalScale, 1);
    }

    /// <summary>
    /// Calculates building scale and size with margins
    /// </summary>
    /// <param name="building">Building to calculate for</param>
    /// <param name="buildingTexture">Building's texture</param>
    /// <returns>Scale and size vector</returns>
    private static (float finalScale, Vector2 objectSize) CalculateBuildingScale(Building building, BuildingTexture buildingTexture)
    {
        var margin = 0.0f;
        if (building.HasMargin)
        {
            margin = MapConfig.CellSize * _defaultOffset;
        }

        var textureUnitWidth = buildingTexture.Texture.width / _unitPerCell;
        var textureUnitHeight = buildingTexture.Texture.height / _unitPerCell;

        var totalVisualWidthInUnits = MapConfig.CellSize * buildingTexture.VisualWidthCell - (margin * 2);
        var totalVisualHeightInUnits = MapConfig.CellSize * buildingTexture.VisualHeightCell - (margin * 2);

        var scaleToFitCellX = totalVisualWidthInUnits / textureUnitWidth;
        var scaleToFitCellY = totalVisualHeightInUnits / textureUnitHeight;

        var baseScale = Mathf.Min(scaleToFitCellX, scaleToFitCellY);
        var finalScale = baseScale * buildingTexture.Scale;

        return (finalScale, new Vector2(textureUnitWidth * finalScale, textureUnitHeight * finalScale));
    }

    /// <summary>
    /// Places building in grid and creates grid objects
    /// </summary>
    /// <param name="gridPosition">Position to place at</param>
    /// <param name="buildingId">Building's unique ID</param>
    /// <param name="grid">Target grid</param>
    /// <param name="building">Building to place</param>
    private static void PlaceBuildingInGrid(Vector2Int gridPosition, Guid buildingId, MapGrid<BuildingGridObject> grid, Building building)
    {
        for (var x = gridPosition.x; x < gridPosition.x + building.WidthCell; x++)
        {
            for (var y = gridPosition.y; y < gridPosition.y + building.HeightCell; y++)
            {
                var buildingGridObject = new BuildingGridObject(grid, x, y, buildingId);
                var worldPosition = grid.GetWorldPosition(x, y);
                grid.SetGridObject(worldPosition, buildingGridObject);
            }
        }
    }

    private static void AddBuildingToList(Vector2Int gridPosition, Guid buildingGuid, ItemList<BuildingItem> buildingItemList, Building building, GameObject buildingGameObject)
    {
        Backpack backpack = null;
        if (building.BackpackCapacity > 0)
        {
            backpack = new Backpack(building.BackpackCapacity);
        }

        var buildingItemItem = new BuildingItem(gridPosition, buildingGuid, building, buildingGameObject, backpack);
        buildingItemList.Add(buildingItemItem);
    }

    /// <summary>
    /// Sets building position in world space
    /// </summary>
    /// <param name="buildingObject">Building to position</param>
    /// <param name="gridPosition">Grid position</param>
    /// <param name="building">Building data</param>
    /// <param name="buildingTexture">Building's texture</param>
    private static void SetBuildingPosition(GameObject buildingObject, Vector2Int gridPosition, Building building, BuildingTexture buildingTexture)
    {
        var worldPosition = GridService.GetWorldPosition(gridPosition.x, gridPosition.y);
        var (_, objectSize) = CalculateBuildingScale(building, buildingTexture);

        var offset = CalculateOffset(objectSize, building, buildingTexture);
        worldPosition.x += offset.x;
        worldPosition.y += offset.y;
        worldPosition.z = CalculateZOffset(worldPosition);

        buildingObject.transform.position = worldPosition;
    }

    /// <summary>
    /// Calculates offset for building position
    /// </summary>
    /// <param name="objectSize">Building's size</param>
    /// <param name="building">Building data</param>
    /// <param name="buildingTexture">Building's texture</param>
    /// <returns>Offset vector</returns>
    private static Vector2 CalculateOffset(Vector2 objectSize, Building building, BuildingTexture buildingTexture)
    {
        var margin = 0.0f;
        if (building.HasMargin)
        {
            margin = MapConfig.CellSize * _defaultOffset;
        }

        var availableWidth = (MapConfig.CellSize * buildingTexture.VisualWidthCell) - (margin * 2);
        var availableHeight = (MapConfig.CellSize * buildingTexture.VisualHeightCell) - (margin * 2);

        var defaultOffsetX = margin + (availableWidth - objectSize.x) / 2;
        var defaultOffsetY = margin + (availableHeight - objectSize.y) / 2;

        if (!buildingTexture.RandomPos) return new Vector2(defaultOffsetX, defaultOffsetY);

        var maxOffsetX = availableWidth - objectSize.x;
        var maxOffsetY = availableHeight - objectSize.y;

        return new Vector2(
            margin + Random.Range(0, maxOffsetX),
            margin + Random.Range(0, maxOffsetY)
        );
    }

    /// <summary>
    /// Calculates Z offset for sprite layering
    /// </summary>
    /// <param name="worldPosition">World position</param>
    /// <returns>Z coordinate offset</returns>
    private static float CalculateZOffset(Vector2 worldPosition)
    {
        return (MapConfig.MapHeight * MapConfig.CellSize - worldPosition.y) * -0.001f;
    }

    /// <summary>
    /// Sets up building's BoxCollider2D
    /// </summary>
    /// <param name="buildingObject">Target building</param>
    /// <param name="gridPosition">Grid position</param>
    /// <param name="building">Building data</param>
    /// <param name="buildingTexture">Building's texture</param>
    private static void SetupBuildingCollider(GameObject buildingObject, Vector2Int gridPosition, Building building, BuildingTexture buildingTexture)
    {
        var collider = buildingObject.AddComponent<BoxCollider2D>();
        var (finalScale, _) = CalculateBuildingScale(building, buildingTexture);

        float colliderWidth, colliderHeight;

        if (building.HasMargin)
        {
            var margin = MapConfig.CellSize * _defaultOffset;
            colliderWidth = MapConfig.CellSize * building.WidthCell - (margin * 2);
            colliderHeight = MapConfig.CellSize * building.HeightCell - (margin * 2);
        }
        else
        {
            colliderWidth = MapConfig.CellSize * building.WidthCell;
            colliderHeight = MapConfig.CellSize * building.HeightCell;
        }

        collider.size = new Vector2(colliderWidth / finalScale, colliderHeight / finalScale);

        var colliderPosition = GridService.GetWorldPosition(gridPosition.x, gridPosition.y);
        var buildingPosition = buildingObject.transform.position;
        var colliderOffset = CalculateColliderOffset(buildingPosition, colliderPosition, finalScale, building);
        collider.offset = colliderOffset;

        buildingObject.layer = LayerMask.NameToLayer("Objects");
    }

    /// <summary>
    /// Calculates collider offset relative to building
    /// </summary>
    /// <param name="buildingPosition">Building's position</param>
    /// <param name="colliderPosition">Base collider position</param>
    /// <param name="finalScale">Building's scale</param>
    /// <param name="building">Building data</param>
    /// <returns>Collider offset vector</returns>
    private static Vector2 CalculateColliderOffset(Vector3 buildingPosition, Vector3 colliderPosition, float finalScale, Building building)
    {
        var offset = new Vector2(buildingPosition.x - colliderPosition.x, buildingPosition.y - colliderPosition.y);
        return new Vector2(
            MapConfig.CellSize * building.WidthCell / 2 / finalScale - offset.x / finalScale,
            MapConfig.CellSize * building.HeightCell / 2 / finalScale - offset.y / finalScale
        );
    }
}
