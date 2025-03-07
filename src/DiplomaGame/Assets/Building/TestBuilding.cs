using GameUtilities.Utils;
using UnityEngine;

public class TestBuilding :MonoBehaviour
{
    private Building selectedBuilding;

    private ItemList<BuildingItem> _list;
    private MapGrid<BuildingGridObject> _grid;

    private void Awake()
    {
        _list = new ItemList<BuildingItem>();
        _grid = new MapGrid<BuildingGridObject>(
            MapConfig.MapWidth,
            MapConfig.MapHeight,
            MapConfig.CellSize,
            new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY),
            (g, x, y) => new BuildingGridObject(g, x, y)
        );
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
            var clickPosition = UtilsClass.GetMouseWorldPosition();
            var gridPosition = GridService.GetCellGridPosition(clickPosition);

            BuildingManager.Build(gridPosition, selectedBuilding, _list, _grid);
        }
    }
}
