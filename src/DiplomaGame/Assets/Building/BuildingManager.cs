using System.Linq;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
	[SerializeField]
    public Texture2D texture;

	private MapGrid<BuildingGridObject> _grid;
	private Building selectedBuilding; // The building selected from the configuration

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
        var clickPosition = GetMouseWorldPosition();
        var gridPosition = _grid.GetCellGridPosition(clickPosition);

        if (!CanPlaceBuilding(gridPosition)) return;

        var buildingId = System.Guid.NewGuid();
        var newBuildingObject = CreateBuildingGameObject(selectedBuilding.Name);

        SetupBuildingSprite(newBuildingObject);
        PlaceBuildingInGrid(gridPosition, buildingId);
        SetBuildingPosition(newBuildingObject, gridPosition);
        SetupBuildingCollider(newBuildingObject, gridPosition);
    }

    private Vector3 GetMouseWorldPosition()
    {
        var clickPosition = Input.mousePosition;
        clickPosition.z = 0.0f;
        return Camera.main.ScreenToWorldPoint(clickPosition);
    }

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

    private GameObject CreateBuildingGameObject(string buildingName)
    {
        return new GameObject(buildingName);
    }

    private void SetupBuildingSprite(GameObject buildingObject)
    {
        var renderer = buildingObject.AddComponent<SpriteRenderer>();
        var newBuildingSprite = Sprite.Create(
            texture,
            new Rect(0.0f, 0.0f, texture.width, texture.height),
            Vector2.zero
        );
        renderer.sprite = newBuildingSprite;

        var (finalScale, _) = CalculateBuildingScale();
        buildingObject.transform.localScale = new Vector3(finalScale, finalScale, 1);
    }

    private (float finalScale, Vector2 objectSize) CalculateBuildingScale()
    {
        var textureUnitWidth = texture.width / 100.0f;
        var textureUnitHeight = texture.height / 100.0f;

        var totalVisualWidthInUnits = MapConfig.CellSize * selectedBuilding.VisualWidthCell;
        var totalVisualHeightInUnits = MapConfig.CellSize * selectedBuilding.VisualHeightCell;

        var scaleToFitCellX = totalVisualWidthInUnits / textureUnitWidth;
        var scaleToFitCellY = totalVisualHeightInUnits / textureUnitHeight;

        var baseScale = Mathf.Min(scaleToFitCellX, scaleToFitCellY);
        var finalScale = baseScale * selectedBuilding.Scale;

        return (finalScale, new Vector2(textureUnitWidth * finalScale, textureUnitHeight * finalScale));
    }

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

    private void SetBuildingPosition(GameObject buildingObject, Vector2Int gridPosition)
    {
        var worldPosition = _grid.GetWorldPosition(gridPosition.x, gridPosition.y);
        var (finalScale, objectSize) = CalculateBuildingScale();

        var offset = CalculateOffset(objectSize);
        worldPosition.x += offset.x;
        worldPosition.y += offset.y;
        worldPosition.z = CalculateZOffset(gridPosition);

        buildingObject.transform.position = worldPosition;
    }

    private Vector2 CalculateOffset(Vector2 objectSize)
    {
        var defaultOffsetX = (MapConfig.CellSize * selectedBuilding.VisualWidthCell - objectSize.x) / 2;
        var defaultOffsetY = (MapConfig.CellSize * selectedBuilding.VisualHeightCell - objectSize.y) / 2;

        if (!selectedBuilding.RandomPos) return new Vector2(defaultOffsetX, defaultOffsetY);

        var maxOffsetX = MapConfig.CellSize * selectedBuilding.VisualWidthCell - objectSize.x;
        var maxOffsetY = MapConfig.CellSize * selectedBuilding.VisualHeightCell - objectSize.y;

        return new Vector2(
            Random.Range(0, maxOffsetX),
            Random.Range(0, maxOffsetY)
        );
    }

    private float CalculateZOffset(Vector2Int gridPosition)
    {
        return (MapConfig.MapHeight * MapConfig.CellSize - gridPosition.y) * -0.001f;
    }

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

    private Vector2 CalculateColliderOffset(Vector3 buildingPosition, Vector3 colliderPosition, float finalScale)
    {
        var offset = new Vector2(buildingPosition.x - colliderPosition.x, buildingPosition.y - colliderPosition.y);
        return new Vector2(
            MapConfig.CellSize * selectedBuilding.WidthCell / 2 / finalScale - offset.x / finalScale,
            MapConfig.CellSize * selectedBuilding.HeightCell / 2 / finalScale - offset.y / finalScale
        );
    }
}
