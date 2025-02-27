using System;
using UnityEngine;

public class UnitItem : IUnit
{
    public float X => UnitGameObject.transform.position.x;
    public float Y => UnitGameObject.transform.position.y;
    public Guid Guid { get; set; }
    public Unit Unit { get; set; }
    public GameObject UnitGameObject { get; set; }

    public UnitItem(float x, float y, Guid guid, Unit unit, GameObject unitGameObject)
    {
        Guid = guid;
        Unit = unit;
        UnitGameObject = unitGameObject;
        SetPosition(new Vector2(x, y));
    }

    public UnitItem(Vector2 position, Guid guid, Unit unit, GameObject unitGameObject)
    {
        Guid = guid;
        Unit = unit;
        UnitGameObject = unitGameObject;
        SetPosition(position);
    }

    public void SetPosition(Vector2 position)
    {
        var zPos = (MapConfig.MapHeight * MapConfig.CellSize - UnitGameObject.transform.position.y) * -0.001f;
        UnitGameObject.transform.position = new Vector3(position.x, position.y, zPos);
    }

    public Guid GetGuid()
    {
        return Guid;
    }
}
