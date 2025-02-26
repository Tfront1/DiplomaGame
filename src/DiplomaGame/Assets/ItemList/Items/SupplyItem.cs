using System;
using Supplies;
using UnityEngine;

public class SupplyItem : IItemListObject
{
    public int X { get; set; }
    public int Y { get; set; }
    public Guid Guid { get; set; }
    public Supply Supply { get; set; }
    public GameObject SupplyGameObject { get; set; }

    public SupplyItem(int x, int y, Guid guid, Supply supply, GameObject supplyGameObject)
    {
        X = x;
        Y = y;
        Guid = guid;
        Supply = supply;
        SupplyGameObject = supplyGameObject;
    }

    public SupplyItem(Vector2Int position, Guid guid, Supply supply, GameObject supplyGameObject)
    {
        X = position.x;
        Y = position.y;
        Guid = guid;
        Supply = supply;
        SupplyGameObject = supplyGameObject;
    }

    public Guid GetGuid()
    {
        return Guid;
    }
}
