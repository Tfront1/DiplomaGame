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

    public SupplyItem(int x, int y, Guid guid, Supply supply, GameObject supplyGameObject, Backpack backpack)
    {
        X = x;
        Y = y;
        Guid = guid;
        Supply = supply;
        SupplyGameObject = supplyGameObject;

        Backpack = backpack;
        ResourceElement = supply.Type switch
        {
            "Wood" => ResourcesConfig.ResourceElements.Find(resourceElement => resourceElement.Id == 1),
            "Stone" => ResourcesConfig.ResourceElements.Find(resourceElement => resourceElement.Id == 2),
            "Iron Ore" => ResourcesConfig.ResourceElements.Find(resourceElement => resourceElement.Id == 3),
            "Gold Ore" => ResourcesConfig.ResourceElements.Find(resourceElement => resourceElement.Id == 4),
            _ => throw new ArgumentException($"Unknown result type: {supply.Type}")
        };

        Backpack.FillWithSingleItem(ResourceElement);
        Debug.Log($"Resource:{ResourceElement.Name} Count:{Backpack.GetResourceQuantity(ResourceElement)}");
    }

    public SupplyItem(Vector2Int position, Guid guid, Supply supply, GameObject supplyGameObject, Backpack backpack)
    {
        X = position.x;
        Y = position.y;
        Guid = guid;
        Supply = supply;
        SupplyGameObject = supplyGameObject;

        Backpack = backpack;
        ResourceElement = supply.Type switch
        {
            "Wood" => ResourcesConfig.ResourceElements.Find(resourceElement => resourceElement.Id == 1),
            "Stone" => ResourcesConfig.ResourceElements.Find(resourceElement => resourceElement.Id == 2),
            "Iron Ore" => ResourcesConfig.ResourceElements.Find(resourceElement => resourceElement.Id == 3),
            "Gold Ore" => ResourcesConfig.ResourceElements.Find(resourceElement => resourceElement.Id == 4),
            _ => throw new ArgumentException($"Unknown result type: {supply.Type}")
        };

        Backpack.FillWithSingleItem(ResourceElement);
        Debug.Log($"Resource:{ResourceElement.Name} Count:{Backpack.GetResourceQuantity(ResourceElement)}");
    }

    public Guid GetGuid()
    {
        return Guid;
    }
}
