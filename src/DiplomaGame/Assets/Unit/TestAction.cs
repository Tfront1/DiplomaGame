using System;
using GameUtilities.Utils;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Selection;
using Town;

public class TestAction : MonoBehaviour
{
    public static List<Vector2> _path;

    public Vector2 _end;

    public static TownItem _town = new TownItem("Test", Guid.NewGuid());
    
    private void Update()
    {
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
                    var moveResources = new MoveResourcesForBuildingAction(unit, buildingItem);
                    UnitActionManager.Instance.QueueAction(moveResources);
                }
            }
        }
    }

    private List<UnitItem> SpawnUnit()
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

            var unit = new Unit { Speed = 15f };
            renderer.sprite = newBuildingSprite;
            unitGameObject.transform.localScale = new Vector3(25f, 25f, 1f);

            var randomX = clickPosition.x + (float)(random.NextDouble() * 50);
            var randomY = clickPosition.y + (float)(random.NextDouble() * 50);
            randomX = clickPosition.x;
            randomY = clickPosition.y;
            var randomPosition = new Vector2(randomX, randomY);

            var unitItem = UnitItem.Create(randomPosition, Guid.NewGuid(), unit, unitGameObject, _town);

            _town.AddUnit(unitItem);

            var res = ResourcesConfig.ResourceElements.Find(x => x.Id == 1);

            //unitItem.Backpack.FillWithSingleItem(res);
        }

        return units;
    }


}
