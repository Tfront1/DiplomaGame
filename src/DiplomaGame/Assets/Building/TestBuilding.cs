using System;
using System.Linq;
using GameUtilities.Utils;
using Town;
using UnityEngine;

public class TestBuilding :MonoBehaviour
{
    private Building selectedBuilding;

    private TownItem _townItem;

    private void Awake()
    {
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
            _townItem = TestAction._town;
            selectedBuilding = BuildingsConfig.Buildings[4];
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            var clickPosition = UtilsClass.GetMouseWorldPosition();
            var gridPosition = GridService.GetCellGridPosition(clickPosition);

            BuildingManager.BuildInstantly(new Vector2Int(10, 10), BuildingsConfig.Buildings[3], _townItem);
            BuildingManager.BuildInstantly(new Vector2Int(15, 15), BuildingsConfig.Buildings[3], _townItem);

            var res = ResourcesConfig.ResourceElements.Find(x => x.Id == 1);

            _townItem.Buildings[0].Backpack.AddItem(res, 1000);
            _townItem.Buildings[1].Backpack.AddItem(res, 500);

            BuildingManager.BuildWithFoundation(gridPosition, selectedBuilding, BuildingsConfig.Buildings.First(), _townItem);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            var clickPosition = UtilsClass.GetMouseWorldPosition();
            var gridPosition = GridService.GetCellGridPosition(clickPosition);


            BuildingManager.RemoveBuilding(gridPosition);
        }
    }
}
