using System;
using GameUtilities.Utils;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Town;
using Random = UnityEngine.Random;

public class TestAction : MonoBehaviour
{
    public static List<Vector2> _path;

    public Vector2 _end;

    public static TownItem _town;
    public static TownItem _enemyTown;

    public void Start()
    {
        _town = TownRegistry.UserTown;

        _enemyTown = TownRegistry.TownList.Find(x => !x.IsUnitControlTown);
        /*
        _town = new TownItem("Town", Guid.NewGuid());
        BuildingManager.BuildInstantly(new Vector2Int(5, 5),
            BuildingsConfig.Buildings.Find(x => x.BuildingType == Building.BuildingTypes.TownHall), _town);

        _enemyTown = new TownItem("Enemy town",Guid.NewGuid(), false);
        BuildingManager.BuildInstantly(new Vector2Int(10, 10),
            BuildingsConfig.Buildings.Find(x => x.BuildingType == Building.BuildingTypes.TownHall), _enemyTown);

        */
        _town.TownHall.Backpack.AddItem(WeaponConfig.WeaponElements.Find(x => x.Id == 2));
    }

    private void Update()
    {
        //Spawn unit
        if (Input.GetKeyDown(KeyCode.Q))
        {
            var clickPosition = UtilsClass.GetMouseWorldPosition();
            var unitItem = UnitManager.CreateUnit(clickPosition, 3, _town);
            
            var rand = Random.Range(1, 2);
            var res = ResourcesConfig.ResourceElements.Find(x => x.Id == rand);

            unitItem.Backpack.FillWithSingleItem(res);

            //SpawnUnit(_town);
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            var clickPosition = UtilsClass.GetMouseWorldPosition();
            UnitManager.CreateUnit(clickPosition, 3, _enemyTown);
            //SpawnUnit(_enemyTown);
        }

        /*
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var unit in _town.Units)
            {
                var waitAction = new WaitUnitAction(unit, 2000f);
                UnitActionManager.Instance.QueueAction(waitAction);
            }
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            _end = UtilsClass.GetMouseWorldPosition();

            var unit = SelectorManager.SelectedItems.First() as UnitItem;
            if (unit != null)
            {
                var moveAction = new MoveUnitAction(unit, _end);
                UnitActionManager.Instance.ExecuteImmediately(moveAction);
            }
        }

        //Move to mouse in query
        else if (Input.GetKeyDown(KeyCode.W))
        {
            _end = UtilsClass.GetMouseWorldPosition();
            
            foreach (var item in SelectorManager.SelectedItems)
            {
                var unit = item as UnitItem;
                if (unit != null)
                {
                    var moveGroupAction = new MoveGroupUnitAction(unit, _end, true);
                    UnitActionManager.Instance.QueueAction(moveGroupAction);
                }
            }
        }
        //Spawn unit
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            _town.Units.AddRange(SpawnUnit());
        }
        //Pause
        else if (Input.GetKeyDown(KeyCode.S))
        {
            UnitActionManager.Instance.PauseAllActions();
        }
        //Resume
        else if (Input.GetKeyDown(KeyCode.D))
        {
            UnitActionManager.Instance.ResumeAllActions();
        }
        //Stop doing
        else if (Input.GetKeyDown(KeyCode.F))
        {
            foreach (var item in SelectorManager.SelectedItems)
            {
                var unit = item as UnitItem;
                if (unit != null)
                {
                    UnitActionManager.Instance.InterruptCurrentAction(unit);
                }
            }
        }
        //Bring Resources
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            var buildingItem = _town.Buildings[2];

            foreach (var item in SelectorManager.SelectedItems)
            {
                var unit = item as UnitItem;
                if (unit != null)
                {
                    _town.BuildingTownOrder.AssignUnitToOrder(unit, buildingItem);
                }
            }
        }
        //Add builders to town
        else if (Input.GetKeyDown(KeyCode.X))
        {
            foreach (var item in SelectorManager.SelectedItems)
            {
                var unit = item as UnitItem;
                if (unit != null)
                {
                    _town.BuildingTownOrder.AddBuilder(unit);
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            var mousePosition = UtilsClass.GetMouseWorldPosition();
            var selectedItems = SelectorManager.Instance.GetSelectedItem(mousePosition);

            SupplyItem supply = null;

            if (selectedItems.Item1.Count == 1)
            {
                if (selectedItems.Item1.First() is SupplyItem supplyItem)
                {
                    supply = supplyItem;
                }
            }

            foreach (var item in SelectorManager.SelectedItems)
            {
                var unit = item as UnitItem;
                if (unit != null)
                {
                    var collectAction = new CollectSupplyAction(unit, supply);
                    UnitActionManager.Instance.QueueAction(collectAction);
                }
            }
        }
        */
    }

    public static void SpawnUnit(TownItem town)
    {
        var clickPosition = UtilsClass.GetMouseWorldPosition();

        var random = new System.Random();
        List<UnitItem> units = new();
        for (var i = 0; i < 1; i++)
        {
            var unitGameObject = new GameObject("TestUnit");
            var renderer = unitGameObject.AddComponent<SpriteRenderer>();

            var fileData = File.ReadAllBytes("Assets/Textures/Units/Archer/Idle/Idle1.png");
            var texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();

            var newBuildingSprite = Sprite.Create(
                texture,
                new Rect(0.0f, 0.0f, texture.width, texture.height),
                Vector2.zero
            );

            var unit = UnitsConfig.Units.Find(x => x.Id == 3);
            renderer.sprite = newBuildingSprite;
            unitGameObject.transform.localScale = new Vector3(25f, 25f, 1f);

            var randomX = clickPosition.x + (float)(random.NextDouble() * 50);
            var randomY = clickPosition.y + (float)(random.NextDouble() * 50);
            randomX = clickPosition.x;
            randomY = clickPosition.y;
            var randomPosition = new Vector2(randomX, randomY);

            var unitItem = UnitItem.Create(randomPosition, Guid.NewGuid(), UtilsClass.GetRandomName(), unit, unitGameObject, town);

            town.AddUnit(unitItem);

            var rand = Random.Range(1, 2);
            var res = ResourcesConfig.ResourceElements.Find(x => x.Id == rand);

            unitItem.Backpack.FillWithSingleItem(res);
        }
    }
}
