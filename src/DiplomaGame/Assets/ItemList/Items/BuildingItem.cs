using Supplies;
using System;
using UnityEngine;

public class BuildingItem : IItemListObject
{
    public int X { get; set; }
    public int Y { get; set; }
    public Guid Guid { get; set; }
    public Building Building { get; set; }

    public BuildingItem(int x, int y, Guid guid, Building building)
    {
        X = x;
        Y = y;
        Guid = guid;
        Building = building;
    }

    public BuildingItem(Vector2Int position, Guid guid, Building building)
    {
        X = position.x;
        Y = position.y;
        Guid = guid;
        Building = building;
    }

    public Guid GetGuid()
    {
        return Guid;
    }
}
