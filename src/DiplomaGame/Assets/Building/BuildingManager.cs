using System;
using System.Collections.Generic;
using System.Linq;
using Items.Resource.BackPack;
using Town;
using UnityEngine;
using UnityEngine.UI;
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
    public static Dictionary<int, BuildingTexture> _buildingTextureConfigCache;

    /// <summary>
    /// Cache for building sprite to avoid repeated searches
    /// </summary>
    public static Dictionary<int, Sprite> _buildingSpriteCache = new();
    
    /// <summary>
    /// Reference to parent GameObject that organizes all Building objects in hierarchy
    /// </summary>
    private static Transform _buildingFolder;

    private static bool _isInitializedCaches = false;

    private static ItemList<BuildingItem> _buildingItemList;
    private static MapGrid<BuildingGridObject> _grid;

    private static GameObject _previewBuildingObject;
    private static Building _previewBuilding;
    private static BuildingTexture _previewBuildingTexture;
    private static SpriteRenderer _previewSpriteRenderer;
    private static bool _canPlaceBuilding;
    private static TownItem _town;

    /// <summary>
    /// Initializes building caches with data from configs
    /// </summary>
    public static void InitializeCaches()
    {
        _buildingItemList = new ItemList<BuildingItem>();
        _grid = new MapGrid<BuildingGridObject>(
            MapConfig.MapWidth,
            MapConfig.MapHeight,
            MapConfig.CellSize,
            new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY),
            (g, x, y) => new BuildingGridObject(g, x, y)
        );

        _buildingTextureConfigCache = BuildingTexturesConfig.BuildingTexture.ToDictionary(t => t.BuildingId);

        foreach (var (buildingId, texture) in _buildingTextureConfigCache)
        {
            if (texture.Texture != null)
            {
                var buildingSprite = Sprite.Create(
                    texture.Texture,
                    new Rect(0.0f, 0.0f, texture.Texture.width, texture.Texture.height),
                    Vector2.zero
                );

                _buildingSpriteCache[buildingId] = buildingSprite;
            }
        }

        _isInitializedCaches = true; 
    }

    public static bool BuildWithFoundation(Vector2Int gridPosition, Building building, Building construction, TownItem townItem)
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

        var buildingConstruction = SetConstructionVariables(building, construction);

        if (buildingConstruction == null)
        {
            return false;
        }

        var buildingConstructionTexture = SetTextureConstructionVariables(_buildingTextureConfigCache[building.Id],
            _buildingTextureConfigCache[construction.Id]);

        SetupBuildingSprite(newBuildingObject, buildingConstructionTexture, construction);
        SetBuildingPosition(newBuildingObject, gridPosition, buildingConstruction, buildingConstructionTexture);
        SetupBuildingCollider(newBuildingObject, gridPosition, buildingConstruction, buildingConstructionTexture);

        var buildingItem = CreateBuildingItem(gridPosition, buildingGuid, building, newBuildingObject, townItem, buildingConstruction);

        PlaceBuildingInGrid(gridPosition, buildingGuid, _grid, building);
        AddBuildingToList(buildingItem, _buildingItemList);
        GridRegistry.UpsertGrid(_grid);
        ItemListRegistry.UpsertList(_buildingItemList);

        townItem.AddBuilding(buildingItem);
        TeleportUnitsToEdgeOfTheBuilding(buildingItem);

        LayoutRebuilder.ForceRebuildLayoutImmediate(buildingItem.gameObject.GetComponent<RectTransform>());
        buildingItem.BuildingController.BuildingActionUI.UpdateUIScale();

        return true;
    }

    public static bool BuildInstantly(Vector2Int gridPosition, Building building, TownItem townItem)
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

        var buildingItem = CreateBuildingItem(gridPosition, buildingGuid, building, newBuildingObject, townItem);

        PlaceBuildingInGrid(gridPosition, buildingGuid, _grid, building);
        AddBuildingToList(buildingItem, _buildingItemList);
        GridRegistry.UpsertGrid(_grid);
        ItemListRegistry.UpsertList(_buildingItemList);

        townItem.AddBuilding(buildingItem);
        TeleportUnitsToEdgeOfTheBuilding(buildingItem);
        LayoutRebuilder.ForceRebuildLayoutImmediate(buildingItem.gameObject.GetComponent<RectTransform>());
        buildingItem.BuildingController.BuildingActionUI.UpdateUIScale();

        return true;
    }

    public static void CompleteBuilding(object sender, BuildingItem.BuildingCompletedEventArgs e)
    {
        var buildingItem = e.BuildingItem;
        var buildingTexture = _buildingTextureConfigCache[buildingItem.Building.Id];
        var buildingGameObject = buildingItem.BuildingGameObject;

        SetupBuildingSprite(buildingGameObject, buildingTexture, buildingItem.Building, true);
        SetBuildingPosition(buildingGameObject, buildingItem.Coords, buildingItem.Building, _buildingTextureConfigCache[buildingItem.Building.Id]);
        SetupBuildingCollider(buildingGameObject, buildingItem.Coords, buildingItem.Building, _buildingTextureConfigCache[buildingItem.Building.Id]);

        buildingItem.HP = buildingItem.Building.MaxHP;
        if (buildingItem.Building.BackpackCapacity > 0)
        {
            buildingItem.Backpack.SetMaxCapacity(buildingItem.Building.BackpackCapacity);
        }
        else
        {
            buildingItem.Backpack = null;
        }

        buildingItem.CreateSelectionIndicator();

        TeleportUnitsToEdgeOfTheBuilding(buildingItem);
        LayoutRebuilder.ForceRebuildLayoutImmediate(buildingItem.gameObject.GetComponent<RectTransform>());
        buildingItem.BuildingController.BuildingActionUI.UpdateUIScale();

        buildingItem.HomeTown.RemoveBuilding(buildingItem);
        buildingItem.HomeTown.AddBuilding(buildingItem);

        buildingItem.CreateProgressBar();
    }

    public static bool RemoveBuilding(Vector2Int gridPosition)
    {
        var buildingId = _grid.GetGridObject(gridPosition);
        if (buildingId == null)
        {
            return false;
        }

        var buildingItem = _buildingItemList.GetValue(buildingId.Guid);
        if (buildingItem == null)
        {
            return false;
        }

        buildingItem.OnDestroyed -= RemoveBuilding;
        buildingItem.OnBuildingComplete -= CompleteBuilding;

        RemoveBuildingFromGrid(gridPosition, _grid, buildingItem.Building);
        RemoveBuildingFromList(buildingItem.Id, _buildingItemList);

        buildingItem.Destroy();

        return true;
    }

    public static void PrePlacementBuilding(Building building, TownItem town)
    {
        if (!_isInitializedCaches)
        {
            InitializeCaches();
        }

        _town = town;

        ClearPrePlacementBuilding();

        _previewBuilding = building;
        _previewBuildingTexture = _buildingTextureConfigCache[building.Id];

        _previewBuildingObject = CreateBuildingGameObject($"Preview_{building.Name}");
        SetupPreviewBuildingSprite(_previewBuildingObject, _previewBuildingTexture, building);

        GameplayInputHandler.Instance.OnMousePosition += MovePrePlacementBuilding;
        GameplayInputHandler.Instance.OnMouseLeftClick += TryPlaceBuilding;
        GameplayInputHandler.Instance.OnMouseRightClick += CancelPrePlacementBuilding;

        MovePrePlacementBuilding(Input.mousePosition);

        Debug.Log($"Started placement mode for {building.Name}");
    }

    private static void SetupPreviewBuildingSprite(GameObject buildingObject, BuildingTexture buildingTexture, Building building)
    {
        _previewSpriteRenderer = buildingObject.AddComponent<SpriteRenderer>();

        var newBuildingSprite = Sprite.Create(
            buildingTexture.Texture,
            new Rect(0.0f, 0.0f, buildingTexture.Texture.width, buildingTexture.Texture.height),
            Vector2.zero
        );
        _previewSpriteRenderer.sprite = newBuildingSprite;

        var (finalScale, _) = CalculateBuildingScale(building, buildingTexture);
        buildingObject.transform.localScale = new Vector3(finalScale, finalScale, 1);

        _previewSpriteRenderer.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
        _previewSpriteRenderer.sortingOrder = 100;
    }

    private static void MovePrePlacementBuilding(Vector2 mousePosition)
    {
        if (_previewBuildingObject == null || _previewBuilding == null)
            return;

        var gridPosition = GridService.GetCellGridPosition(mousePosition);

        _canPlaceBuilding = GridService.CanPlaceAtPosition(
            gridPosition,
            new Vector2Int(_previewBuilding.WidthCell, _previewBuilding.HeightCell),
            GridRegistry.GetAllGridsList().ToArray()) &&
            ItemListService.CanPlaceAtPosition(
                gridPosition,
                new Vector2Int(_previewBuilding.WidthCell, _previewBuilding.HeightCell),
                ItemListRegistry.GetAllListsItemsList().ToArray()
            );

        UpdatePreviewColor(_canPlaceBuilding);

        SetBuildingPosition(_previewBuildingObject, gridPosition, _previewBuilding, _previewBuildingTexture);
    }

    private static void UpdatePreviewColor(bool canPlace)
    {
        if (_previewSpriteRenderer == null)
            return;

        if (canPlace)
        {
            _previewSpriteRenderer.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
        }
        else
        {
            _previewSpriteRenderer.color = new Color(1.0f, 0.3f, 0.3f, 0.7f);
        }
    }

    private static void TryPlaceBuilding(Vector2 mousePosition)
    {
        if (_previewBuildingObject == null || _previewBuilding == null)
            return;

        var gridPosition = GridService.GetCellGridPosition(mousePosition);

        if (_canPlaceBuilding)
        {
            var success = false;
            if(CraftingRecipesConfig.CraftingRecipesDictionary.TryGetValue(_previewBuilding.BuildingCraftId, out var craft))
            {
                if (craft != null)
                {
                    if (craft.CraftingTime == 0f)
                    {
                        success = BuildInstantly(gridPosition, _previewBuilding, _town);
                    }
                    else
                    {
                        success = BuildWithFoundation(gridPosition, _previewBuilding,
                            BuildingsConfig.Buildings.Find(x => x.Id == 1), _town);
                    }
                }
            }

            if (success)
            {
                Debug.Log($"Successfully placed {_previewBuilding.Name} at {gridPosition}");
                ClearPrePlacementBuilding();
            }
        }
        else
        {
            Debug.Log($"Cannot place {_previewBuilding.Name} at {gridPosition}");
        }
    }

    private static void CancelPrePlacementBuilding(Vector2 mousePosition)
    {
        ClearPrePlacementBuilding();
        Debug.Log("Building placement canceled");
    }

    private static void ClearPrePlacementBuilding()
    {
        if (_previewBuildingObject != null)
        {
            GameObject.Destroy(_previewBuildingObject);
            _previewBuildingObject = null;
        }

        _previewBuilding = null;
        _previewBuildingTexture = null;
        _previewSpriteRenderer = null;

        if (GameplayInputHandler.Instance != null)
        {
            GameplayInputHandler.Instance.OnMousePosition -= MovePrePlacementBuilding;
            GameplayInputHandler.Instance.OnMouseLeftClick -= TryPlaceBuilding;
            GameplayInputHandler.Instance.OnMouseRightClick -= CancelPrePlacementBuilding;
        }
    }

    private static void RemoveBuilding(object sender, BuildingItem.BuildingDestroyedEventArgs e)
    {
        var gridPosition = new Vector2Int(e.BuildingItem.X, e.BuildingItem.Y);

        var buildingItem = _buildingItemList.GetValue(_grid.GetGridObject(gridPosition).Guid);
        if (buildingItem == null)
        {
            return;
        }

        buildingItem.OnDestroyed -= RemoveBuilding;
        buildingItem.OnBuildingComplete -= CompleteBuilding;

        RemoveBuildingFromGrid(gridPosition, _grid, buildingItem.Building);
        RemoveBuildingFromList(buildingItem.Id, _buildingItemList);
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
    /// <param name="hasSpriteRender">Indicates whether the object already has a SpriteRenderer component.
    /// If true, the existing renderer will be used;
    /// if false, a new one will be added.</param>
    private static void SetupBuildingSprite(GameObject buildingObject, BuildingTexture buildingTexture, Building building, bool hasSpriteRender = false)
    {
        SpriteRenderer renderer;
        if (!hasSpriteRender)
        {
            renderer = buildingObject.AddComponent<SpriteRenderer>();
        }
        else
        {
            renderer = buildingObject.GetComponent<SpriteRenderer>();
        }

        if (_buildingSpriteCache.TryGetValue(building.Id, out var sprite))
        {
            renderer.sprite = sprite;
        }
        else
        {
            Sprite newSprite = Sprite.Create(
                buildingTexture.Texture,
                new Rect(0.0f, 0.0f, buildingTexture.Texture.width, buildingTexture.Texture.height),
                Vector2.zero
            );

            _buildingSpriteCache[buildingTexture.BuildingId] = newSprite;
            renderer.sprite = newSprite;
        }

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

    private static BuildingItem CreateBuildingItem(Vector2Int gridPosition, Guid buildingGuid, Building building, 
        GameObject buildingGameObject, TownItem townItem, Building buildingConstruction = null)
    {
        Backpack backpack = null;

        if (buildingConstruction != null)
        {
            backpack = new Backpack(buildingConstruction.BackpackCapacity);
        }
        else if (building.BackpackCapacity > 0 )
        {
            backpack = new Backpack(building.BackpackCapacity);
        }

        var isBuild = buildingConstruction == null;

        var buildingItem =
            BuildingItem.Create(gridPosition, buildingGuid, building, buildingGameObject, townItem, backpack, isBuild, buildingConstruction);

        buildingItem.OnDestroyed += RemoveBuilding;
        if (buildingConstruction == null)
        {
            buildingItem.IsBuilt = true;

        }
        else
        {
            buildingItem.OnBuildingComplete += CompleteBuilding;
            buildingItem.IsBuilt = false;
        }

        return buildingItem;
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

    private static void RemoveBuildingFromGrid(Vector2Int gridPosition,
        MapGrid<BuildingGridObject> grid, Building building)
    {
        for (var x = gridPosition.x; x < gridPosition.x + building.WidthCell; x++)
        {
            for (var y = gridPosition.y; y < gridPosition.y + building.HeightCell; y++)
            {
                grid.RemoveGridObject(x, y);
            }
        }
    }

    private static void AddBuildingToList(BuildingItem buildingItem, ItemList<BuildingItem> buildingItemList)
    {
        buildingItemList.Add(buildingItem);
    }

    private static void RemoveBuildingFromList( Guid buildingGuid, ItemList<BuildingItem> buildingItemList)
    {
        buildingItemList.Remove(buildingGuid);
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
        var collider = buildingObject.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = buildingObject.AddComponent<BoxCollider2D>();
        }
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

    private static Building SetConstructionVariables(Building building, Building construction)
    {
        var copyConstruction = new Building();

        CraftingRecipesConfig.CraftingRecipesDictionary.TryGetValue(building.BuildingCraftId, out var buildingCraft);
        if (buildingCraft == null)
        {
            return null;
        }

        copyConstruction.BackpackCapacity = buildingCraft.GetAllComponentsQuantity();

        copyConstruction.HasMargin = building.HasMargin;
        copyConstruction.Name = building.Name;
        copyConstruction.HeightCell = building.HeightCell;
        copyConstruction.WidthCell = building.WidthCell;
        copyConstruction.BuildingType = construction.BuildingType;
        copyConstruction.MaxHP = construction.MaxHP;

        return copyConstruction;
    }

    private static BuildingTexture SetTextureConstructionVariables(BuildingTexture building, BuildingTexture construction)
    {
        var buildingTexture = new BuildingTexture
        {
            RandomPos = building.RandomPos,
            Scale = building.Scale,
            VisualHeightCell = building.VisualHeightCell,
            VisualWidthCell = building.VisualWidthCell,
            Texture = construction.Texture
        };

        return buildingTexture;
    }

    public static void TeleportUnitsToEdgeOfTheBuilding(BuildingItem building)
    {
        var buildingCollider = building.Collider;

        if (buildingCollider == null)
        {
            return;
        }

        var buildingBounds = buildingCollider.bounds;
        var collidingObjects = Physics2D.OverlapBoxAll(
            buildingBounds.center,
            buildingBounds.size,
            0f,
            LayerMask.GetMask("Units")
        );

        foreach (var unitCollider in collidingObjects)
        {
            if (unitCollider.gameObject == building.gameObject)
                continue;

            var unitItem = unitCollider.GetComponent<UnitItem>();
            if (unitItem == null)
                continue;

            var unitPosition = unitItem.CenterCoords;

            var closestPoint = GetClosestPointOnBuildingEdge2D(unitPosition, buildingBounds);

            unitItem.SetPosition(closestPoint);
        }
    }

    private static Vector2 GetClosestPointOnBuildingEdge2D(Vector2 unitPosition, Bounds buildingBounds)
    {
        Vector2 center = buildingBounds.center;
        Vector2 halfSize = buildingBounds.extents;
        var keyPoints = new Vector2[8];
        keyPoints[0] = new Vector2(center.x - halfSize.x, center.y - halfSize.y);
        keyPoints[1] = new Vector2(center.x + halfSize.x, center.y - halfSize.y);
        keyPoints[2] = new Vector2(center.x - halfSize.x, center.y + halfSize.y);
        keyPoints[3] = new Vector2(center.x + halfSize.x, center.y + halfSize.y);
        keyPoints[4] = new Vector2(center.x, center.y - halfSize.y);
        keyPoints[5] = new Vector2(center.x, center.y + halfSize.y);
        keyPoints[6] = new Vector2(center.x - halfSize.x, center.y);
        keyPoints[7] = new Vector2(center.x + halfSize.x, center.y);

        var closestPoint = center;
        var minDistance = float.MaxValue;
        foreach (var point in keyPoints)
        {
            if (GridService.IsWorldPositionInMapBounds(point))
            {
                var distance = Vector2.Distance(unitPosition, point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = point;
                }
            }
        }

        var directionFromCenter = (closestPoint - center).normalized;
        var offsetPoint = closestPoint + directionFromCenter * 0.2f;

        var collision = Physics2D.OverlapCircle(offsetPoint, 0.1f, LayerMask.GetMask("Objects"));
        if (collision == null)
        {
            return offsetPoint;
        }
        else
        {
            return closestPoint;
        }
    }
}
