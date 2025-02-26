using System;
using UnityEngine;

public class UnitItem : IUnit
{
    public float X { get; set; }
    public float Y { get; set; }
    public Guid Guid { get; set; }
    public Unit Unit { get; set; }
    public GameObject UnitGameObject { get; set; }

    public UnitItem(float x, float y, Guid guid, Unit unit, GameObject unitGameObject)
    {
        X = x;
        Y = y;
        Guid = guid;
        Unit = unit;
        UnitGameObject = unitGameObject;
    }

    public UnitItem(Vector2Int position, Guid guid, Unit unit, GameObject unitGameObject)
    {
        X = position.x;
        Y = position.y;
        Guid = guid;
        Unit = unit;
        UnitGameObject = unitGameObject;
    }

    public void SetPosition(Vector2 position)
    {
        var zPos = (MapConfig.MapHeight * MapConfig.CellSize - UnitGameObject.transform.position.y) * -0.001f;
        UnitGameObject.transform.position = new Vector3(position.x, position.y, zPos);
        X = position.x;
        Y = position.y;
    }

    public Guid GetGuid()
    {
        return Guid;
    }
}
