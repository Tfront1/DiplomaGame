using System;
using Items.Resource.BackPack;
using Items.Resource;
using Supplies;
using UnityEngine;

public class SupplyItem : IItemListObject
{
    public int X { get; set; }
    public int Y { get; set; }
    public Guid Guid { get; set; }
    public Supply Supply { get; set; }
    public GameObject SupplyGameObject { get; set; }

    //Gameplay
    public Backpack Backpack { get; }
    public ResourceElement ResourceElement { get; }

    public SupplyItem(Vector2Int position, Guid guid, Supply supply, GameObject supplyGameObject, Backpack backpack)
    {
        X = position.x;
        Y = position.y;
        Guid = guid;
        Supply = supply;
        SupplyGameObject = supplyGameObject;

        Backpack = backpack;
        ResourceElement = ResourcesConfig.ResourceElements.Find(x => x.Id == supply.ResourceId);

        Backpack.FillWithSingleItem(ResourceElement);
        Debug.Log($"Resource:{ResourceElement.Name} Count:{Backpack.GetResourceQuantity(ResourceElement)}");
    }

    public Guid GetGuid()
    {
        return Guid;
    }
}
