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
    public Vector2Int Coords => new(X, Y);
    public Building Building { get; set; }
    public GameObject BuildingGameObject { get; set; }
    public SpriteRenderer SpriteRenderer { get; set; }
    public BoxCollider2D Collider { get; set; }

    //Gameplay
    public Building Construction { get; set; }
    public bool IsBuilt { get; set; }
    public bool IsAllDelivered { get; set; } = false;
    public float  BuildingTimePassed { get; set; }
    private CraftingRecipe _buildingCraft;

    public event EventHandler<ResourceDeliveredArgs> OnResourcesDelivered;
    public event EventHandler<BuildingCompletedEventArgs> OnBuildingComplete;
    public event EventHandler<BuildingDestroyedEventArgs> OnDestroyed;

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
        building.CraftsIds.ForEach(craft => Crafts.Add(CraftingRecipesConfig.CraftingRecipes.Find(recipe => recipe.Id == craft)));
        CraftingRecipesConfig.CraftingRecipesDictionary.TryGetValue(Building.BuildingCraftId, out var buildingCraft);
        _buildingCraft = buildingCraft;
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

    public void AddBuildingMaterials(UnitItem unit)
    {
        var unitBackpack = unit.Backpack;

        if (_buildingCraft != null)
        {
            foreach (var component in _buildingCraft.Components)
            {
                if (unitBackpack.HasResource(component.BackpackItem))
                {
                    var resourceHave = Backpack.GetResourceQuantity(component.BackpackItem);
                    var resourceNeed = component.Quantity - resourceHave;

                    if (resourceNeed > 0)
                    {
                        var unitResourceCount = unitBackpack.GetResourceQuantity(component.BackpackItem);

                        var resourceCount = Math.Min(unitResourceCount, resourceNeed);

                        var deliveredResources =
                            new List<CraftingComponent> { new(component.BackpackItem, resourceCount) };

                        var deliverArgs = new ResourceDeliveredArgs(unit, deliveredResources);
                        OnResourcesDelivered?.Invoke(this, deliverArgs);

                        unitBackpack.RemoveResource(component.BackpackItem, resourceCount);
                        Backpack.AddItem(component.BackpackItem, resourceCount);
                    }
                }
            }
        }
    }

    public bool CanProvideResources(Backpack unitBackpack)
    {
        if (_buildingCraft == null)
        {
            return false;
        }

        foreach (var component in _buildingCraft.Components)
        {
            var resourceHave = Backpack.GetResourceQuantity(component.BackpackItem);

            var resourceNeed = component.Quantity - resourceHave;

            if (resourceNeed > 0 && unitBackpack.HasResource(component.BackpackItem))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasAllRequiredResources()
    {
        if (_buildingCraft == null)
        {
            return false;
        }

        foreach (var component in _buildingCraft.Components)
        {
            var resourceHave = Backpack.GetResourceQuantity(component.BackpackItem);

            if (resourceHave < component.Quantity)
            {
                return false;
            }
        }
        return true;
    }

    public bool UpdateBuildingProgress(float deltaTime, UnitItem builder)
    {
        if (HasAllRequiredResources())
        {
            if (_buildingCraft == null)
            {
                return false;
            }

            BuildingTimePassed += deltaTime;

            if (BuildingTimePassed >= _buildingCraft.CraftingTime)
            {
                CompleteBuildingConstruction();
                return true;
            }
        }

        return false;
    }

    private void CompleteBuildingConstruction()
    {
        var args = new BuildingCompletedEventArgs(this);

        OnBuildingComplete?.Invoke(this, args);

        Backpack?.Clear();
        IsBuilt = true;
    }

    public void Destroy()
    {
        Backpack?.Clear();

        var args = new BuildingDestroyedEventArgs(this);
        OnDestroyed?.Invoke(this, args);

        Destroy(BuildingGameObject);
    }

    public Guid GetId()
    {
        return Id;
    }


    public class BuildingDestroyedEventArgs : EventArgs
    {
        public BuildingItem BuildingItem { get; }

        public BuildingDestroyedEventArgs(BuildingItem building)
        {
            BuildingItem = building;
        }
    }

    public class BuildingCompletedEventArgs : EventArgs
    {
        public BuildingItem BuildingItem { get; }

        public BuildingCompletedEventArgs(BuildingItem building)
        {
            BuildingItem = building;
        }
    }
    
    public class ResourceDeliveredArgs : EventArgs
    {
        public UnitItem Unit { get; private set; }
        public List<CraftingComponent> DeliveredComponents { get; private set; }

        public ResourceDeliveredArgs(UnitItem unit, List<CraftingComponent> deliveredComponents)
        {
            Unit = unit;
            DeliveredComponents = deliveredComponents;
        }
    }
}
