using System;
using GameUtilities.Utils;
using Town;
using UnityEngine;

public class TestBuilding :MonoBehaviour
{
    private Building selectedBuilding;

    private ItemList<BuildingItem> _list;
    private MapGrid<BuildingGridObject> _grid;
    private TownItem _townItem;

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
        _townItem = new TownItem("TestBuildingTown", Guid.NewGuid());
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
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            var clickPosition = UtilsClass.GetMouseWorldPosition();
            var gridPosition = GridService.GetCellGridPosition(clickPosition);

            BuildingManager.Build(gridPosition, selectedBuilding, _list, _grid, _townItem);
        }
    }
}
