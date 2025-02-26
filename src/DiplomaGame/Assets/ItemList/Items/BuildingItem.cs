using System;
using UnityEngine;

public class BuildingItem : IItemListObject
{
    public int X { get; set; }
    public int Y { get; set; }
    public Guid Guid { get; set; }
    public Building Building { get; set; }
    public GameObject BuildingGameObject { get; set; }

    public BuildingItem(int x, int y, Guid guid, Building building, GameObject buildingGameObject)
    {
        X = x;
        Y = y;
        Guid = guid;
        Building = building;
        BuildingGameObject = buildingGameObject;
    }

    public BuildingItem(Vector2Int position, Guid guid, Building building, GameObject buildingGameObject)
    {
        X = position.x;
        Y = position.y;
        Guid = guid;
        Building = building;
        BuildingGameObject = buildingGameObject;
    }

    public Guid GetGuid()
    {
        return Guid;
    }
}
