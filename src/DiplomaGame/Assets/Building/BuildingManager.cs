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

        if (!CanPlaceBuilding(gridPosition)) return;

        var buildingId = System.Guid.NewGuid();
        var newBuildingObject = CreateBuildingGameObject(selectedBuilding.Name);

        SetupBuildingSprite(newBuildingObject, texture);
        PlaceBuildingInGrid(gridPosition, buildingId);
        SetBuildingPosition(newBuildingObject, gridPosition);
        SetupBuildingCollider(newBuildingObject, gridPosition);
    }

    /// <summary>
    /// Checks if a building can be placed at the specified grid position.
    /// Iterates through all cells that the building would occupy and verifies they are empty.
    /// </summary>
    /// <param name="gridPosition">The grid position to check</param>
    /// <returns>true if building can be placed, false if any cell is occupied</returns>
    private bool CanPlaceBuilding(Vector2Int gridPosition)
    {
        for (var x = gridPosition.x; x < gridPosition.x + selectedBuilding.WidthCell; x++)
        {
            for (var y = gridPosition.y; y < gridPosition.y + selectedBuilding.HeightCell; y++)
            {
                if (_grid.GetGridObject(x, y).GetGuid() != System.Guid.Empty)
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Creates a new GameObject instance for a building with the specified name
    /// </summary>
    /// <param name="buildingName">The name of the building</param>
    /// <returns>The created GameObject instance</returns>
    private GameObject CreateBuildingGameObject(string buildingName)
    {
        return new GameObject(buildingName);
    }

    /// <summary>
    /// Initializes the visual representation of a building by setting up its sprite renderer and scale.
    /// Creates a new sprite from the provided texture and applies it to the building object.
    /// Calculates and sets the appropriate scale to maintain proper proportions within the grid cell.
    /// </summary>
    /// <param name="buildingObject">The building GameObject to which the sprite will be added</param>
    /// <param name="spriteTexture">The texture to be used for creating the building's sprite</param>
    private void SetupBuildingSprite(GameObject buildingObject, Texture2D spriteTexture)
    {
        var renderer = buildingObject.AddComponent<SpriteRenderer>();
        var newBuildingSprite = Sprite.Create(
            spriteTexture,
            new Rect(0.0f, 0.0f, texture.width, texture.height),
            Vector2.zero
        );
        renderer.sprite = newBuildingSprite;

        var (finalScale, _) = CalculateBuildingScale();
        buildingObject.transform.localScale = new Vector3(finalScale, finalScale, 1);
    }

    /// <summary>
    /// Calculates the scale and size of the building considering margins.
    /// Takes into account margin settings if enabled for the building.
    /// Ensures the building fits within the available cell space while maintaining aspect ratio.
    /// </summary>
    /// <returns>A tuple containing the final scale and object size</returns>
    private (float finalScale, Vector2 objectSize) CalculateBuildingScale()
    {
        var margin = 0.0f;
        if (selectedBuilding.Margin)
        {
            margin = MapConfig.CellSize * _defaultOffset;
        }

        var textureUnitWidth = texture.width / _unitPerCell;
        var textureUnitHeight = texture.height / _unitPerCell;

        var totalVisualWidthInUnits = MapConfig.CellSize * selectedBuilding.VisualWidthCell - (margin * 2);
        var totalVisualHeightInUnits = MapConfig.CellSize * selectedBuilding.VisualHeightCell - (margin * 2);

        var scaleToFitCellX = totalVisualWidthInUnits / textureUnitWidth;
        var scaleToFitCellY = totalVisualHeightInUnits / textureUnitHeight;

        var baseScale = Mathf.Min(scaleToFitCellX, scaleToFitCellY);
        var finalScale = baseScale * selectedBuilding.Scale;

        return (finalScale, new Vector2(textureUnitWidth * finalScale, textureUnitHeight * finalScale));
    }

    /// <summary>
    /// Places the building in the grid by creating grid objects for each cell
    /// that the building occupies. Associates all cells with the building's unique ID.
    /// </summary>
    /// <param name="gridPosition">The starting position in the grid</param>
    /// <param name="buildingId">The unique identifier for the building</param>
    private void PlaceBuildingInGrid(Vector2Int gridPosition, System.Guid buildingId)
    {
        for (var x = gridPosition.x; x < gridPosition.x + selectedBuilding.WidthCell; x++)
        {
            for (var y = gridPosition.y; y < gridPosition.y + selectedBuilding.HeightCell; y++)
            {
                var buildingGridObject = new BuildingGridObject(_grid, x, y, buildingId);
                var worldPosition = _grid.GetWorldPosition(x, y);
                _grid.SetGridObject(worldPosition, buildingGridObject);
            }
        }
    }

    /// <summary>
    /// Sets the building's position in the world space, taking into account
    /// offsets, margins, and Z-coordinate for proper layering.
    /// </summary>
    /// <param name="buildingObject">The building GameObject to position</param>
    /// <param name="gridPosition">The grid position where the building is placed</param>
    private void SetBuildingPosition(GameObject buildingObject, Vector2Int gridPosition)
    {
        var worldPosition = _grid.GetWorldPosition(gridPosition.x, gridPosition.y);
        var (_, objectSize) = CalculateBuildingScale();
        
        var offset = CalculateOffset(objectSize);
        worldPosition.x += offset.x;
        worldPosition.y += offset.y;
        worldPosition.z = CalculateZOffset(gridPosition);

        buildingObject.transform.position = worldPosition;
    }

    /// <summary>
    /// Calculates the offset for positioning the building within its grid cells.
    /// Handles both centered and random positioning, respecting margin settings.
    /// The offset ensures the building stays within the available space defined by
    /// the cell size and margins.
    /// </summary>
    /// <param name="objectSize">The size of the building object</param>
    /// <returns>The calculated offset vector for X and Y coordinates</returns>
    private Vector2 CalculateOffset(Vector2 objectSize)
    {
        var margin = 0.0f;
        if (selectedBuilding.Margin)
        {
            margin = MapConfig.CellSize * _defaultOffset;
        }

        var availableWidth = (MapConfig.CellSize * selectedBuilding.VisualWidthCell) - (margin * 2);
        var availableHeight = (MapConfig.CellSize * selectedBuilding.VisualHeightCell) - (margin * 2);

        var defaultOffsetX = margin + (availableWidth - objectSize.x) / 2;
        var defaultOffsetY = margin + (availableHeight - objectSize.y) / 2;

        if (!selectedBuilding.RandomPos) return new Vector2(defaultOffsetX, defaultOffsetY);

        var maxOffsetX = availableWidth - objectSize.x;
        var maxOffsetY = availableHeight - objectSize.y;

        return new Vector2(
            margin + Random.Range(0, maxOffsetX),
            margin + Random.Range(0, maxOffsetY)
        );
    }

    /// <summary>
    /// Calculates the Z-coordinate offset for proper sprite layering.
    /// Buildings higher on the grid (lower Y values) appear behind
    /// buildings lower on the grid (higher Y values).
    /// </summary>
    /// <param name="gridPosition">The grid position of the building</param>
    /// <returns>The calculated Z-offset for proper layering</returns>
    private float CalculateZOffset(Vector2Int gridPosition)
    {
        return (MapConfig.MapHeight * MapConfig.CellSize - gridPosition.y) * -0.001f;
    }

    /// <summary>
    /// Sets up the collider for the building. Adds a BoxCollider2D component
    /// and configures its size and offset based on the building's dimensions
    /// and position in the grid.
    /// </summary>
    /// <param name="buildingObject">The building GameObject to setup the collider for</param>
    /// <param name="gridPosition">The grid position of the building</param>
    private void SetupBuildingCollider(GameObject buildingObject, Vector2Int gridPosition)
    {
        var collider = buildingObject.AddComponent<BoxCollider2D>();
        var (finalScale, _) = CalculateBuildingScale();

        var colliderWidth = MapConfig.CellSize * selectedBuilding.WidthCell;
        var colliderHeight = MapConfig.CellSize * selectedBuilding.HeightCell;
        collider.size = new Vector2(colliderWidth / finalScale, colliderHeight / finalScale);

        var colliderPosition = _grid.GetWorldPosition(gridPosition.x, gridPosition.y);
        var buildingPosition = buildingObject.transform.position;
        var colliderOffset = CalculateColliderOffset(buildingPosition, colliderPosition, finalScale);
        collider.offset = colliderOffset;
    }

    /// <summary>
    /// Calculates the offset for the building's collider relative to its position.
    /// Ensures the collider is properly aligned with the building's visual representation
    /// while maintaining correct collision boundaries.
    /// </summary>
    /// <param name="buildingPosition">The world position of the building</param>
    /// <param name="colliderPosition">The base position for the collider</param>
    /// <param name="finalScale">The final scale of the building</param>
    /// <returns>The calculated offset vector for the collider</returns>
    private Vector2 CalculateColliderOffset(Vector3 buildingPosition, Vector3 colliderPosition, float finalScale)
    {
        var offset = new Vector2(buildingPosition.x - colliderPosition.x, buildingPosition.y - colliderPosition.y);
        return new Vector2(
            MapConfig.CellSize * selectedBuilding.WidthCell / 2 / finalScale - offset.x / finalScale,
            MapConfig.CellSize * selectedBuilding.HeightCell / 2 / finalScale - offset.y / finalScale
        );
    }
}
