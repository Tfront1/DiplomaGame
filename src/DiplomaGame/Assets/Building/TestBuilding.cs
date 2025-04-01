using System;
using GameUtilities.Utils;
using Town;
using UnityEngine;

public class TestBuilding :MonoBehaviour
{
    private Building selectedBuilding;

    private TownItem _townItem;

    private void Awake()
    {
        _townItem = new TownItem("TestBuildingTown", Guid.NewGuid(), false);
        _townItem = TestAction._enemyTown;
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
            selectedBuilding = BuildingsConfig.Buildings[4];
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            var clickPosition = UtilsClass.GetMouseWorldPosition();
            var gridPosition = GridService.GetCellGridPosition(clickPosition);

            BuildingManager.BuildInstantly(gridPosition, selectedBuilding, _townItem);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            var clickPosition = UtilsClass.GetMouseWorldPosition();
            var gridPosition = GridService.GetCellGridPosition(clickPosition);


            BuildingManager.RemoveBuilding(gridPosition);
        }
    }
}
