using Items.Resource.BackPack;
using System;
using UnityEngine;

public class BuildingItem : IItemListObject
{
    public int X { get; set; }
    public int Y { get; set; }
    public Guid Guid { get; set; }
    public Building Building { get; set; }
    public GameObject BuildingGameObject { get; set; }

    //Gameplay
    public float HP { get; set; }
    public Backpack Backpack { get; }

    public BuildingItem(int x, int y, Guid guid, Building building, GameObject buildingGameObject, Backpack backpack)
    {
        X = x;
        Y = y;
        Guid = guid;
        Building = building;
        BuildingGameObject = buildingGameObject;

        HP = building.MaxHP;
        Backpack = backpack;
    }

    public BuildingItem(Vector2Int position, Guid guid, Building building, GameObject buildingGameObject, Backpack backpack)
    {
        X = position.x;
        Y = position.y;
        Guid = guid;
        Building = building;
        BuildingGameObject = buildingGameObject;

        HP = building.MaxHP;
        Backpack = backpack;
    }

    public Guid GetGuid()
    {
        return Guid;
    }
}
