using Items.Resource.BackPack;
using System;
using System.Collections.Generic;
using Assets.Items.Crafts;
using Town;
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
    public List<CraftingRecipe> Crafts { get; set; } = new();
    public TownItem HomeTown { get; set; }

    public BuildingItem(Vector2Int position, Guid guid, Building building, GameObject buildingGameObject, Backpack backpack)
    {
        X = position.x;
        Y = position.y;
        Guid = guid;
        Building = building;
        BuildingGameObject = buildingGameObject;

        HP = building.MaxHP;
        Backpack = backpack;
        building.CraftsIds.ForEach(craft => Crafts.Add(CraftingRecipeConfig.CraftingRecipes.Find(recipe => recipe.Id == craft)));
    }

    public Guid GetGuid()
    {
        return Guid;
    }

}
