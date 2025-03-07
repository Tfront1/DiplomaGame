using System;
using GameUtilities.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

public class TestAction: MonoBehaviour
{
    public static List<Vector2> _path;

    public UnitActionManager ActionManager = new();

    public UnitItem _unitItem;

    public Vector2 _end;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            var waitAction = new WaitUnitAction(_unitItem, 2000f);
            ActionManager.QueueAction(waitAction);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            _end = UtilsClass.GetMouseWorldPosition();

            var moveAction = new MoveUnitAction(_unitItem, _end);
            ActionManager.ExecuteImmediately(moveAction);
            FindAndDrawPath(new Vector2(_unitItem.X, _unitItem.Y), _end);

            ItemListRegistry.ItemChanged += (type, action, item) => {
                FindAndDrawPath(new Vector2(_unitItem.X, _unitItem.Y), _end);
                ActionManager.InterruptCurrentAction(_unitItem);
                moveAction = new MoveUnitAction(_unitItem, _end);
                ActionManager.ExecuteImmediately(moveAction);
            };
        }
        //Move to mouse in query
        else if (Input.GetKeyDown(KeyCode.W))
        {
            _end = UtilsClass.GetMouseWorldPosition();

            var moveAction = new MoveUnitAction(_unitItem, _end);
            ActionManager.QueueAction(moveAction);
            FindAndDrawPath(new Vector2(_unitItem.X, _unitItem.Y), _end);

            ItemListRegistry.ItemChanged += (type, action, item) => {
                FindAndDrawPath(new Vector2(_unitItem.X, _unitItem.Y), _end);
                ActionManager.InterruptCurrentAction(_unitItem);
                moveAction = new MoveUnitAction(_unitItem, _end);
                ActionManager.QueueAction(moveAction);
            };
        }
        //Spawn unit
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            _unitItem = SpawnUnit();
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
            ActionManager.InterruptCurrentAction(_unitItem);
        }
    }

    private void FindAndDrawPath(Vector2 start, Vector2 end)
    {
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
        UtilsClass.DrawPath(_path, Color.black, 1);
    }

    private UnitItem SpawnUnit()
    {
        var clickPosition = UtilsClass.GetMouseWorldPosition();
        var unit = new Unit { Speed = 15f };
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

        renderer.sprite = newBuildingSprite;
        unitGameObject.transform.position = (Vector2)clickPosition;
        unitGameObject.transform.localScale = new Vector3(25f, 25f, 1f);

        var unitItem = new UnitItem(clickPosition, Guid.NewGuid(), unit, unitGameObject);
        return unitItem;
    }


}
