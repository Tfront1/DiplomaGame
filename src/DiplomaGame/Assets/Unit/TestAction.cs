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

    public UnitActionManager ActionManager = new();


    public Vector2 _end;

    public TownItem _town = new TownItem("Test", Guid.NewGuid());
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var unit in _town.Units)
            {
                var waitAction = new WaitUnitAction(unit, 2000f);
                ActionManager.QueueAction(waitAction);
            }
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            _end = UtilsClass.GetMouseWorldPosition();

            var unit = SelectorManager.SelectedItems.First() as UnitItem;
            if (unit != null)
            {
                ItemListRegistry.ItemChanged += (type, action, item) => {
                    FindAndDrawPath(new Vector2(unit.X, unit.Y), _end);
                };
                var moveAction = new MoveUnitAction(unit, _end);
                ActionManager.ExecuteImmediately(moveAction);
                FindAndDrawPath(new Vector2(unit.X, unit.Y), _end);
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
                    ActionManager.QueueAction(moveGroupAction);
                    FindAndDrawPath(new Vector2(unit.X, unit.Y), _end);

                    ItemListRegistry.ItemChanged += (type, action, item) => {
                        FindAndDrawPath(new Vector2(unit.X, unit.Y), _end);
                    };
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
            ActionManager.PauseAllActions();
        }
        //Resume
        else if (Input.GetKeyDown(KeyCode.D))
        {
            ActionManager.ResumeAllActions();
        }
        //Stop doing
        else if (Input.GetKeyDown(KeyCode.F))
        {
            foreach (var unit in _town.Units)
            {
                ActionManager.InterruptCurrentAction(unit);
            }
        }
    }

    private void FindAndDrawPath(Vector2 start, Vector2 end)
    {/*
        _path = PathFinder.Instance.FindPath(start, end);
        if (_path == null || _path.Count() == 0)
        {
            UtilsClass.CreateWorldTextPopup(
                "No path found!",
                UtilsClass.GetMouseWorldPosition(),
                Color.red,
                1.5f
            );
        }
        UtilsClass.DrawPath(_path, Color.black, 1);*/
    }

    private List<UnitItem> SpawnUnit()
    {
        var clickPosition = UtilsClass.GetMouseWorldPosition();

        var random = new System.Random();
        List<UnitItem> units = new();
        for (var i = 0; i < 100; i++)
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
            //randomX = clickPosition.x;
            //randomY = clickPosition.y;
            var randomPosition = new Vector2(randomX, randomY);

            _town.AddUnit(UnitItem.Create(randomPosition, Guid.NewGuid(), unit, unitGameObject, _town));
        }

        return units;
    }


}
