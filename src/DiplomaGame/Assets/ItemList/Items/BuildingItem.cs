using Items.Resource.BackPack;
using System;
using System.Collections.Generic;
using Assets.Items.Crafts;
using Selection.Interfaces;
using Town;
using UnityEngine;

public class BuildingItem : MonoBehaviour, IItemListObject, ISelectable
{
    public Guid Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public Building Building { get; set; }
    public GameObject BuildingGameObject { get; set; }
    public SpriteRenderer SpriteRenderer { get; set; }
    public BoxCollider2D Collider { get; set; }

    //Gameplay
    public float HP { get; set; }
    public Backpack Backpack { get; set; }
    public List<CraftingRecipe> Crafts { get; set; } = new();
    public TownItem HomeTown { get; set; }
    public bool IsSelected { get; set; } = false;
    public GameObject SelectionIndicator { get; set; }

    public static BuildingItem Create(Vector2Int position, Guid guid, Building building, GameObject buildingGameObject, TownItem townItem, Backpack backpack)
    {
        var buildingItem = buildingGameObject.AddComponent<BuildingItem>();
        buildingItem.Initialize(position, guid, building, buildingGameObject, backpack, townItem);
        return buildingItem;
    }

    public void Initialize(Vector2Int position, Guid guid, Building building, GameObject buildingGameObject, Backpack backpack, TownItem townItem)
    {
        X = position.x;
        Y = position.y;
        Id = guid;
        Building = building;
        BuildingGameObject = buildingGameObject;

        SpriteRenderer = BuildingGameObject.GetComponent<SpriteRenderer>();
        Collider = BuildingGameObject.GetComponent<BoxCollider2D>();

        if (SelectionIndicator == null)
        {
            SelectionIndicator = new GameObject("SelectionIndicator");
            SelectionIndicator.transform.SetParent(transform);
            SelectionIndicator.transform.localPosition = Vector3.zero;

            var indicatorRenderer = SelectionIndicator.AddComponent<SpriteRenderer>();
            indicatorRenderer.sprite = GetComponent<SpriteRenderer>().sprite;
            indicatorRenderer.color = new Color(0, 1, 0, 0.6f);
            indicatorRenderer.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder - 1;

            SelectionIndicator.transform.localScale = new Vector3(1.2f, 1.2f, 1);
        }
        SelectionIndicator.SetActive(false);

        HP = building.MaxHP;
        Backpack = backpack;
        HomeTown = townItem;
        building.CraftsIds.ForEach(craft => Crafts.Add(CraftingRecipeConfig.CraftingRecipes.Find(recipe => recipe.Id == craft)));
    }

    public void OnSelect()
    {
        IsSelected = true;
        SelectionIndicator.SetActive(true);
    }

    public void OnDeselect()
    {
        IsSelected = false;
        SelectionIndicator.SetActive(false);
    }

    public Guid GetGuid()
    {
        return Id;
    }

}
