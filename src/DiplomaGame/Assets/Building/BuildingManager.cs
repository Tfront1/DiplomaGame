using System.Linq;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
	[SerializeField]
    public Texture2D texture;
    
	private MapGrid<BuildingGridObject> _grid;
	private Building selectedBuilding; // The building selected from the configuration

    /// <summary>
    /// How much percent would be minimum offset
    /// </summary>
    private readonly float _defaultOffset = 0.05f;

    /// <summary>
    /// How many pixels in a unit of measurement
    /// </summary>
    private readonly float _unitPerCell = 100.0f;

    /// <summary>
    /// Reference to parent GameObject that organizes all Building objects in hierarchy
    /// </summary>
    private static Transform _buildingFolder;

    private void Awake()
	{
		_grid = new MapGrid<BuildingGridObject>(
			MapConfig.MapWidth,
			MapConfig.MapHeight,
			MapConfig.CellSize,
			new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY),
			(g, x, y) => new BuildingGridObject(g, x, y)
		);

		// Example: Select the first building from the configuration list (you can change this logic)
		selectedBuilding = BuildingsConfig.Buildings.First();
	}

	private void Update()
	{
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedBuilding = BuildingsConfig.Buildings[0];
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedBuilding = BuildingsConfig.Buildings[1];
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedBuilding = BuildingsConfig.Buildings[2];
        }
        else if (Input.GetMouseButtonDown(0))
        {
            HandleBuildingPlacement();
        }
    }

    private void HandleBuildingPlacement()
    {
        var clickPosition = GameUtilities.Utils.UtilsClass.GetMouseWorldPosition();
        var gridPosition = _grid.GetCellGridPosition(clickPosition);

        if (!CanPlaceBuilding(gridPosition, _grid, selectedBuilding)) return;

        var buildingId = System.Guid.NewGuid();
        var newBuildingObject = CreateBuildingGameObject(selectedBuilding.Name);

        SetupBuildingSprite(newBuildingObject, texture, selectedBuilding);
        PlaceBuildingInGrid(gridPosition, buildingId,_grid, selectedBuilding);
        SetBuildingPosition(newBuildingObject, gridPosition, selectedBuilding, texture);
        SetupBuildingCollider(newBuildingObject, gridPosition, selectedBuilding, texture, _grid);
    }

    /// <summary>
    /// Checks if a building can be placed at the grid position
    /// </summary>
    /// <param name="gridPosition">Target position in grid</param>
    /// <param name="grid">Grid to place on</param>
    /// <param name="building">Building to place</param>
    /// <returns>True if can place, false if occupied</returns>
    private bool CanPlaceBuilding(Vector2Int gridPosition, MapGrid<BuildingGridObject> grid, Building building)
    {
        var gridWidth = grid.Width;
        var gridHeight = grid.Height;

        if (gridPosition.x < 0 || gridPosition.y < 0 ||
            gridPosition.x + building.WidthCell > gridWidth ||
            gridPosition.y + building.HeightCell > gridHeight)
        {
            return false;
        }

        if (gridPosition.x + building.VisualWidthCell > gridWidth ||
            gridPosition.y + building.VisualHeightCell > gridHeight)
        {
            return false;
        }

        for (var x = gridPosition.x; x < gridPosition.x + building.WidthCell; x++)
        {
            for (var y = gridPosition.y; y < gridPosition.y + building.HeightCell; y++)
            {
                if (grid.GetGridObject(x, y).GetGuid() != System.Guid.Empty)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Gets or creates a parent folder for buildings
    /// </summary>
    /// <returns>Transform of the buildings folder</returns>
    private Transform GetBuildingsFolder()
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
    private GameObject CreateBuildingGameObject(string buildingName)
    {
        var building = new GameObject(buildingName);
        building.transform.SetParent(GetBuildingsFolder());
        return building;
    }

    /// <summary>
    /// Sets up sprite renderer and scale for the building
    /// </summary>
    /// <param name="buildingObject">Target building object</param>
    /// <param name="spriteTexture">Texture for the sprite</param>
    /// <param name="building">Building data</param>
    private void SetupBuildingSprite(GameObject buildingObject, Texture2D spriteTexture, Building building)
    {
        var renderer = buildingObject.AddComponent<SpriteRenderer>();
        var newBuildingSprite = Sprite.Create(
            spriteTexture,
            new Rect(0.0f, 0.0f, spriteTexture.width, spriteTexture.height),
            Vector2.zero
        );
        renderer.sprite = newBuildingSprite;

        var (finalScale, _) = CalculateBuildingScale(building, spriteTexture);
        buildingObject.transform.localScale = new Vector3(finalScale, finalScale, 1);
    }

    /// <summary>
    /// Calculates building scale and size with margins
    /// </summary>
    /// <param name="building">Building to calculate for</param>
    /// <param name="spriteTexture">Building's texture</param>
    /// <returns>Scale and size vector</returns>
    private (float finalScale, Vector2 objectSize) CalculateBuildingScale(Building building, Texture2D spriteTexture)
    {
        var margin = 0.0f;
        if (building.Margin)
        {
            margin = MapConfig.CellSize * _defaultOffset;
        }

        var textureUnitWidth = spriteTexture.width / _unitPerCell;
        var textureUnitHeight = spriteTexture.height / _unitPerCell;

        var totalVisualWidthInUnits = MapConfig.CellSize * building.VisualWidthCell - (margin * 2);
        var totalVisualHeightInUnits = MapConfig.CellSize * building.VisualHeightCell - (margin * 2);

        var scaleToFitCellX = totalVisualWidthInUnits / textureUnitWidth;
        var scaleToFitCellY = totalVisualHeightInUnits / textureUnitHeight;

        var baseScale = Mathf.Min(scaleToFitCellX, scaleToFitCellY);
        var finalScale = baseScale * building.Scale;

        return (finalScale, new Vector2(textureUnitWidth * finalScale, textureUnitHeight * finalScale));
    }

    /// <summary>
    /// Places building in grid and creates grid objects
    /// </summary>
    /// <param name="gridPosition">Position to place at</param>
    /// <param name="buildingId">Building's unique ID</param>
    /// <param name="grid">Target grid</param>
    /// <param name="building">Building to place</param>
    private void PlaceBuildingInGrid(Vector2Int gridPosition, System.Guid buildingId, MapGrid<BuildingGridObject> grid, Building building)
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

    /// <summary>
    /// Sets building position in world space
    /// </summary>
    /// <param name="buildingObject">Building to position</param>
    /// <param name="gridPosition">Grid position</param>
    /// <param name="building">Building data</param>
    /// <param name="spriteTexture">Building's texture</param>
    private void SetBuildingPosition(GameObject buildingObject, Vector2Int gridPosition, Building building, Texture2D spriteTexture)
    {
        var worldPosition = _grid.GetWorldPosition(gridPosition.x, gridPosition.y);
        var (_, objectSize) = CalculateBuildingScale(building, spriteTexture);

        var offset = CalculateOffset(objectSize, building);
        worldPosition.x += offset.x;
        worldPosition.y += offset.y;
        worldPosition.z = CalculateZOffset(gridPosition);

        buildingObject.transform.position = worldPosition;
    }

    /// <summary>
    /// Calculates offset for building position
    /// </summary>
    /// <param name="objectSize">Building's size</param>
    /// <param name="building">Building data</param>
    /// <returns>Offset vector</returns>
    private Vector2 CalculateOffset(Vector2 objectSize, Building building)
    {
        var margin = 0.0f;
        if (building.Margin)
        {
            margin = MapConfig.CellSize * _defaultOffset;
        }

        var availableWidth = (MapConfig.CellSize * building.VisualWidthCell) - (margin * 2);
        var availableHeight = (MapConfig.CellSize * building.VisualHeightCell) - (margin * 2);

        var defaultOffsetX = margin + (availableWidth - objectSize.x) / 2;
        var defaultOffsetY = margin + (availableHeight - objectSize.y) / 2;

        if (!building.RandomPos) return new Vector2(defaultOffsetX, defaultOffsetY);

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
    /// <param name="gridPosition">Grid position</param>
    /// <returns>Z coordinate offset</returns>
    private float CalculateZOffset(Vector2Int gridPosition)
    {
        return (MapConfig.MapHeight * MapConfig.CellSize - gridPosition.y) * -0.001f;
    }

    /// <summary>
    /// Sets up building's BoxCollider2D
    /// </summary>
    /// <param name="buildingObject">Target building</param>
    /// <param name="gridPosition">Grid position</param>
    /// <param name="building">Building data</param>
    /// <param name="spriteTexture">Building's texture</param>
    /// <param name="grid">Target grid</param>
    private void SetupBuildingCollider(GameObject buildingObject, Vector2Int gridPosition, Building building, Texture2D spriteTexture, MapGrid<BuildingGridObject> grid)
    {
        var collider = buildingObject.AddComponent<BoxCollider2D>();
        var (finalScale, _) = CalculateBuildingScale(building, spriteTexture);

        var colliderWidth = MapConfig.CellSize * building.WidthCell;
        var colliderHeight = MapConfig.CellSize * building.HeightCell;
        collider.size = new Vector2(colliderWidth / finalScale, colliderHeight / finalScale);

        var colliderPosition = grid.GetWorldPosition(gridPosition.x, gridPosition.y);
        var buildingPosition = buildingObject.transform.position;
        var colliderOffset = CalculateColliderOffset(buildingPosition, colliderPosition, finalScale, building);
        collider.offset = colliderOffset;
    }

    /// <summary>
    /// Calculates collider offset relative to building
    /// </summary>
    /// <param name="buildingPosition">Building's position</param>
    /// <param name="colliderPosition">Base collider position</param>
    /// <param name="finalScale">Building's scale</param>
    /// <param name="building">Building data</param>
    /// <returns>Collider offset vector</returns>
    private Vector2 CalculateColliderOffset(Vector3 buildingPosition, Vector3 colliderPosition, float finalScale, Building building)
    {
        var offset = new Vector2(buildingPosition.x - colliderPosition.x, buildingPosition.y - colliderPosition.y);
        return new Vector2(
            MapConfig.CellSize * building.WidthCell / 2 / finalScale - offset.x / finalScale,
            MapConfig.CellSize * building.HeightCell / 2 / finalScale - offset.y / finalScale
        );
    }
}
