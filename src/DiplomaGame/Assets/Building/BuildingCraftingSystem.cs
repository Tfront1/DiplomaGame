using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Items;
using Assets.Items.Crafts;
using Items.Resource.BackPack;
using UnityEngine;

public class BuildingCraftingSystem
{
    public BuildingItem Building { get; }
    public CraftingRecipe CurrentCraftingRecipe { get; private set; }
    public int CurrentCraftingCount { get; private set; } = 0;
    public bool IsCrafting { get; private set; } = false;
    public float CraftingProcessTime { get; private set; } = 0f;
    public List<CraftingComponent> RequiredResources { get; }
    public List<CraftingComponent> DeliveredResources { get; }
    public Backpack Backpack { get; set; }
    public HashSet<UnitItem> AssignedUnits { get; }
    public bool IsAllDelivered { get; set; } = false;

    private Dictionary<Guid, List<CraftingComponent>> _assignedResources;
    private HashSet<UnitItem> _deliverers = new();

    public float CraftingProgress => IsCrafting ?
        CraftingProcessTime / (CurrentCraftingRecipe?.CraftingTime ?? 1f) : 0f;

    private int MaxUnitCraftingCount = 5;

    public BuildingCraftingSystem(BuildingItem building)
    {
        Building = building;

        DeliveredResources = new List<CraftingComponent>();
        AssignedUnits = new HashSet<UnitItem>();
        RequiredResources = new List<CraftingComponent>();
        _assignedResources = new Dictionary<Guid, List<CraftingComponent>>();
    }

    public bool CanCraftRecipe(CraftingRecipe recipe, int count = 1)
    {
        if (recipe.WhereToCraftId != Building.Building.Id && recipe.WhereToCraftId != 0)
            return false;

        return ItemCraftingManager.CanCraftMultiple(recipe, Building.HomeTown.TotalBackpack, count);
    }

    public int GetMaxPossibleCrafts(CraftingRecipe recipe)
    {
        return ItemCraftingManager.CalculateMaxPossibleCrafts(recipe, Building.HomeTown.TotalBackpack);
    }

    public void StartCraft(CraftingRecipe recipe, int count = 1)
    {
        if (recipe == null)
            return;

        CurrentCraftingRecipe = recipe;
        IsCrafting = true;
        CurrentCraftingCount = count;

        var totalResourceCount = 0;

        foreach (var resource in CurrentCraftingRecipe.Components)
        {
            DeliveredResources.Add(new CraftingComponent(resource.BackpackItem, 0));
            RequiredResources.Add(new CraftingComponent(resource.BackpackItem, resource.Quantity));
            totalResourceCount += resource.Quantity;
        }

        Backpack = new Backpack(totalResourceCount);
    }

    public List<CraftingComponent> GetRemainingResources()
    {
        List<CraftingComponent> remainingResources = new();

        for (var i = 0; i < RequiredResources.Count; i++)
        {
            var remaining = RequiredResources[i].Quantity - DeliveredResources[i].Quantity;

            foreach (var assignedList in _assignedResources.Values)
            {
                var assignedResource = assignedList.FirstOrDefault(ar => ar.BackpackItem.Id == RequiredResources[i].BackpackItem.Id);
                if (assignedResource != null)
                {
                    remaining -= assignedResource.Quantity;
                }
            }

            if (remaining > 0)
            {
                remainingResources.Add(new CraftingComponent(RequiredResources[i].BackpackItem, remaining));
            }
        }

        return remainingResources;
    }

    public void StopCraft()
    {
        var assignedUnitsCopy = AssignedUnits.ToList();

        foreach (var unit in AssignedUnits)
        {
            var action = UnitActionManager.Instance.GetCurrentUnitAction(unit);
            if (action is TransportResourcesForBuildingAction moveAction)
            {
                moveAction.InterruptedByOrder = true;
                UnitActionManager.Instance.InterruptCurrentAction(unit);
            }

            if (action is CraftAction buildAction)
            {
                buildAction.InterruptedByCraft = true;
                UnitActionManager.Instance.InterruptCurrentAction(unit);
            }
        }

        foreach (var unit in assignedUnitsCopy)
        {
            UnassignUnitFromCraft(unit);
        }

        IsCrafting = false;
        IsAllDelivered = false;
        CurrentCraftingCount = 0;
        CurrentCraftingRecipe = null;
        CraftingProcessTime = 0f;

        Backpack.Clear();
        AssignedUnits.Clear();
        _assignedResources.Clear();
        _deliverers.Clear();
    }

    public bool UpdateCraftingProgress(float deltaTime)
    {
        CraftingProcessTime += deltaTime;

        var isCompleted = CraftingProcessTime >= CurrentCraftingRecipe.CraftingTime;

        if (isCompleted)
        {
            CompleteCraft();
        }

        return isCompleted;
    }

    private void CompleteCraft()
    {
        var craftedItem = ItemFactory.CreateItem(CurrentCraftingRecipe.ResultId, CurrentCraftingRecipe.ResultType);
        if (BackpackTransfer.Instance.AddItemToTownHall(Building.HomeTown, craftedItem))
        {
            CurrentCraftingCount--;
            if (CurrentCraftingCount > 0)
            {
                StartCraft(CurrentCraftingRecipe, CurrentCraftingCount);
            }
        }
        else
        {
            Debug.Log("No space for created craft");
        }

        StopCraft();
    }

    public bool AssignUnitToCraft(UnitItem unit)
    {
        if (AssignedUnits.Count >= MaxUnitCraftingCount)
            return false;

        if (AssignedUnits.Contains(unit))
            return false;

        var remainingResources = GetRemainingResources();

        if (IsAllDelivered || remainingResources.Count == 0)
        {
            AssignedUnits.Add(unit);
            _deliverers.Add(unit);

            var craftAction = new CraftAction(unit, this);
            UnitActionManager.Instance.ExecuteImmediately(craftAction);

            return true;
        }

        var resourcesUnitHave = CalculateResourcesUnitHave(unit);
        var resourcesForUnitToTake = CalculateResourcesForUnitToTake(unit);
        if (resourcesForUnitToTake.Count == 0 && resourcesUnitHave.Count == 0)
        {
            UnassignUnitFromCraft(unit, false);
            return false;
        }

        var buildingsToTakeResources = GetClosestBuildingsWithResources(unit, resourcesForUnitToTake);

        // If unit have resources
        if (resourcesUnitHave.Count > 0)
        {
            // If no buildings to take resources OR distance is closer to target building than to collect resources from buildings
            // Go directly to target building
            if (buildingsToTakeResources.Count == 0 ||
                ShouldGoDirectlyToTarget(unit, Building,
                    buildingsToTakeResources.Select(x => x.Building).ToList(),
                    resourcesUnitHave, resourcesForUnitToTake))
            {
                AssignedUnits.Add(unit);
                _assignedResources[unit.Id] = resourcesUnitHave;

                _deliverers.Add(unit);

                var newAction = new TransportResourcesForBuildingAction(unit, Building);
                UnitActionManager.Instance.ExecuteImmediately(newAction);
            }
            else if (buildingsToTakeResources.Count > 0)
            {
                AssignedUnits.Add(unit);
                _assignedResources[unit.Id] = resourcesForUnitToTake;

                _deliverers.Add(unit);

                var newAction = new TransportResourcesForBuildingAction(unit, Building, buildingsToTakeResources);
                UnitActionManager.Instance.ExecuteImmediately(newAction);
            }
        }
        else if (buildingsToTakeResources.Count > 0)
        {
            AssignedUnits.Add(unit);
            _assignedResources[unit.Id] = resourcesForUnitToTake;

            _deliverers.Add(unit);

            var newAction = new TransportResourcesForBuildingAction(unit, Building, buildingsToTakeResources);
            UnitActionManager.Instance.ExecuteImmediately(newAction);
        }

        return true;
    }

    public void UnassignUnitFromCraft(UnitItem unit, bool continueToWork = true)
    {
        if (!continueToWork)
        {
            _deliverers.Remove(unit);
        }

        if (!IsAllDelivered)
        {
            var action = UnitActionManager.Instance.GetCurrentUnitAction(unit);
            if (action is TransportResourcesForBuildingAction moveAction)
            {
                moveAction.InterruptedByOrder = true;
                moveAction.Cancel();
            }

            _assignedResources.Remove(unit.Id);
            AssignedUnits.Remove(unit);
        }
        else
        {
            if (AssignedUnits.Contains(unit))
            {
                AssignedUnits.Remove(unit);
                var action = UnitActionManager.Instance.GetCurrentUnitAction(unit);
                if (action is CraftAction craftAction)
                {
                    craftAction.InterruptedByCraft = true;
                    craftAction.Cancel();
                }
            }
        }
    }

    public void RecalculateAssignUnits()
    {
        if (CurrentCraftingRecipe == null)
        {
            return;
        }

        if (_deliverers.Count == 0 && AssignedUnits.Count == 0)
        {
            return;
        }

        if (!IsAllDelivered)
        {
            var availableDeliverers = new List<UnitItem>(_deliverers);
            var currentlyAssignedUnits = new List<UnitItem>(AssignedUnits);

            var unitsToSkip = new HashSet<UnitItem>();

            foreach (var unit in currentlyAssignedUnits)
            {
                var action = UnitActionManager.Instance.GetCurrentUnitAction(unit);

                if (action is TransportResourcesForBuildingAction)
                {
                    if (!ShouldReassignTransportUnit(unit))
                    {
                        unitsToSkip.Add(unit);
                    }
                }
            }

            var allUnitsToReassign = new HashSet<UnitItem>();

            foreach (var unit in availableDeliverers)
            {
                if (!unitsToSkip.Contains(unit))
                {
                    allUnitsToReassign.Add(unit);
                }
            }

            foreach (var unit in currentlyAssignedUnits)
            {
                if (!unitsToSkip.Contains(unit))
                {
                    allUnitsToReassign.Add(unit);
                }
            }

            foreach (var unit in allUnitsToReassign)
            {
                UnassignUnitFromCraft(unit);
            }

            foreach (var unit in allUnitsToReassign)
            {
                AssignUnitToCraft(unit);
            }
        }
        else
        {
            var availableBuilders = new List<UnitItem>(_deliverers);

            foreach (var unit in availableBuilders)
            {
                var action = UnitActionManager.Instance.GetCurrentUnitAction(unit);

                if (action is not CraftAction)
                {
                    if (AssignedUnits.Contains(unit))
                    {
                        UnassignUnitFromCraft(unit, false);
                    }

                    AssignUnitToCraft(unit);
                }
            }
        }
    }
    
    private bool ShouldReassignTransportUnit(UnitItem unit)
    {
        if (unit.Backpack.CurrentCapacity == 0)
        {
            return true;
        }

        var isCarryingNeededResources = false;

        foreach (var item in unit.Backpack.GetAllItems())
        {
            foreach (var component in CurrentCraftingRecipe.Components)
            {
                if (component.BackpackItem.Id == item.Item.Id)
                {
                    isCarryingNeededResources = true;
                }
            }
        }

        if (!isCarryingNeededResources)
        {
            return true;
        }

        return false;
    }

    public void AddCraftingMaterials(UnitItem unit)
    {
        var unitBackpack = unit.Backpack;

        foreach (var component in CurrentCraftingRecipe.Components)
        {
            if (unitBackpack.HasResource(component.BackpackItem))
            {
                var resourceHave = Backpack.GetResourceQuantity(component.BackpackItem);
                var resourceNeed = component.Quantity - resourceHave;

                if (resourceNeed > 0)
                {
                    var unitResourceCount = unitBackpack.GetResourceQuantity(component.BackpackItem);

                    var resourceCount = Math.Min(unitResourceCount, resourceNeed);

                    unitBackpack.RemoveItem(component.BackpackItem, resourceCount);
                    Backpack.AddItem(component.BackpackItem, resourceCount);
                }
            }
        }

        CheckCompletionDelivery();
    }
    
    public bool HasAssignedUnit(UnitItem unit)
    {
        return AssignedUnits.Contains(unit);
    }

    private void CheckCompletionDelivery()
    {
        var allDelivered = true;

        for (var i = 0; i < RequiredResources.Count; i++)
        {
            if (DeliveredResources[i].Quantity < RequiredResources[i].Quantity)
            {
                allDelivered = false;
                break;
            }
        }

        if (allDelivered)
        {
            IsAllDelivered = true;
            RecalculateAssignUnits();
        }
    }

    private List<CraftingComponent> CalculateResourcesUnitHave(UnitItem unit)
    {
        var neededResources = GetRemainingResources();
        List<CraftingComponent> result = new();

        foreach (var resource in neededResources)
        {
            if (unit.Backpack.HasResource(resource.BackpackItem))
            {
                var quantity = unit.Backpack.GetResourceQuantity(resource.BackpackItem);

                if (quantity > 0)
                {
                    result.Add(new CraftingComponent(resource.BackpackItem, quantity));
                }
            }
        }

        return result;
    }

    private List<CraftingComponent> CalculateResourcesForUnitToTake(UnitItem unit)
    {
        var neededResources = GetRemainingResources();
        List<CraftingComponent> result = new();
        var availableCapacity = unit.Backpack.GetFreeQuantity();

        if (availableCapacity <= 0)
            return result;

        foreach (var resource in neededResources)
        {
            var alreadyCarrying = unit.Backpack.HasResource(resource.BackpackItem)
                ? unit.Backpack.GetResourceQuantity(resource.BackpackItem)
                : 0;

            var stillNeeded = resource.Quantity - alreadyCarrying;

            if (stillNeeded <= 0)
                continue;

            var availableInTown = Building.HomeTown.TotalBackpack.GetResourceQuantity(resource.BackpackItem);

            if (availableInTown <= 0)
                continue;

            var canTake = Math.Min(Math.Min(stillNeeded, availableInTown), availableCapacity);

            result.Add(new CraftingComponent(resource.BackpackItem, canTake));
            availableCapacity -= canTake;

            if (availableCapacity <= 0)
                break;
        }

        return result;
    }

    private bool ShouldGoDirectlyToTarget(UnitItem unit, BuildingItem targetBuilding, List<BuildingItem> resourceBuildings, List<CraftingComponent> currentResources, List<CraftingComponent> resourcesToTake)
    {
        if (resourceBuildings.Count == 0)
        {
            return true;
        }

        var currentResourcesTotal = currentResources.Sum(r => r.Quantity);
        if (currentResourcesTotal == 0)
        {
            return false;
        }

        var distanceToTarget = Vector2.Distance(GridService.GetWorldPosition(targetBuilding.X, targetBuilding.Y), unit.Coords);
        var distanceToResourceBuilding = Vector2.Distance(GridService.GetWorldPosition(resourceBuildings.First().X, resourceBuildings.First().Y), unit.Coords);
        var distanceFromResourceToTarget = Vector2.Distance(GridService.GetWorldPosition(resourceBuildings.First().X, resourceBuildings.First().Y), GridService.GetWorldPosition(targetBuilding.X, targetBuilding.Y));
        var totalIndirectRoute = distanceToResourceBuilding + distanceFromResourceToTarget;

        var potentialResourcesTotal = currentResourcesTotal;
        foreach (var resource in resourcesToTake)
        {
            potentialResourcesTotal += resource.Quantity;
        }

        var distanceEfficiency = distanceToTarget / totalIndirectRoute;

        var resourceEfficiency = (float)currentResourcesTotal / potentialResourcesTotal;

        const float DISTANCE_WEIGHT = 0.4f;
        const float RESOURCE_WEIGHT = 0.6f;

        var totalEfficiency = (distanceEfficiency * DISTANCE_WEIGHT) + (resourceEfficiency * RESOURCE_WEIGHT);

        const float EFFICIENCY_THRESHOLD = 0.65f;

        return totalEfficiency >= EFFICIENCY_THRESHOLD;
    }

    private List<(BuildingItem Building, List<CraftingComponent> ResourcesForBuilding)> GetClosestBuildingsWithResources(UnitItem unit, List<CraftingComponent> resourcesToTake)
    {
        var result = new List<(BuildingItem Building, List<CraftingComponent> ResourcesForBuilding)>();
        if (resourcesToTake.Count == 0)
        {
            return result;
        }

        var remainingResources = resourcesToTake.Select(r =>
            new CraftingComponent(r.BackpackItem, r.Quantity))
            .ToList();

        var buildingsWithAllResources = unit.HomeTown.Buildings
            .Where(b => b.Backpack != null && b.IsBuilt)
            .Where(b => remainingResources.All(r =>
                b.Backpack.HasResource(r.BackpackItem) &&
                b.Backpack.GetResourceQuantity(r.BackpackItem) >= r.Quantity))
            .OrderBy(b => Vector2.Distance(new Vector2(unit.X, unit.Y), GridService.GetWorldPosition(b.X, b.Y)))
            .ToList();

        if (buildingsWithAllResources.Count > 0)
        {
            var closestVault = buildingsWithAllResources.First();
            var resourcesToBeTaken = remainingResources.Select(r =>
                new CraftingComponent(r.BackpackItem, r.Quantity))
                .ToList();

            result.Add((closestVault, resourcesToBeTaken));
            return result;
        }

        var visitedBuildings = new HashSet<BuildingItem>();
        while (remainingResources.Count > 0)
        {
            var bestBuilding = unit.HomeTown.Buildings
                .Where(b => b.Backpack != null && b.IsBuilt && !visitedBuildings.Contains(b))
                .Where(b => remainingResources.Any(r =>
                    b.Backpack.HasResource(r.BackpackItem) &&
                    b.Backpack.GetResourceQuantity(r.BackpackItem) > 0))
                .OrderByDescending(b => remainingResources.Sum(r =>
                    b.Backpack.HasResource(r.BackpackItem) ?
                        Math.Min(b.Backpack.GetResourceQuantity(r.BackpackItem), r.Quantity) : 0))
                .ThenBy(b => Vector2.Distance(new Vector2(unit.X, unit.Y), GridService.GetWorldPosition(b.X, b.Y)))
                .FirstOrDefault();

            if (bestBuilding == null) break;

            visitedBuildings.Add(bestBuilding);

            var resourcesFromThisBuilding = new List<CraftingComponent>();

            foreach (var resource in remainingResources.ToList())
            {
                if (bestBuilding.Backpack.HasResource(resource.BackpackItem))
                {
                    var available = bestBuilding.Backpack.GetResourceQuantity(resource.BackpackItem);
                    var toTake = Math.Min(available, resource.Quantity);

                    if (toTake > 0)
                    {
                        resourcesFromThisBuilding.Add(new CraftingComponent(resource.BackpackItem, toTake));

                        resource.Quantity -= toTake;
                        if (resource.Quantity <= 0)
                        {
                            remainingResources.Remove(resource);
                        }
                    }
                }
            }

            result.Add((bestBuilding, resourcesFromThisBuilding));

            if (remainingResources.Count == 0)
            {
                break;
            }
        }

        return result;
    }
}
