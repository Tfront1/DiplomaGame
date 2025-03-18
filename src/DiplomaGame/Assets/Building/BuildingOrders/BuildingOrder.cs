using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Items.Crafts;
using Town;
using UnityEngine;
using static BuildingItem;

public class BuildingOrder
{
    public Guid Id { get; set; }
    public BuildingItem TargetBuilding { get; set; }
    public List<CraftingComponent> RequiredResources { get; }
    public List<CraftingComponent> DeliveredResources { get; }
    public HashSet<UnitItem> AssignedUnits { get; }
    public TownItem HomeTown { get; }
    public bool IsStopped { get; set; } = false;
    public int OrderPriority { get; set; }

    private Dictionary<Guid, List<CraftingComponent>> _assignedResources;
    private HashSet<UnitItem> _builders = new();

    public event EventHandler<OrderCompletedArgs> OnCompleted;
    public BuildingOrder(Guid orderId, BuildingItem targetBuilding, List<CraftingComponent> requiredResources, TownItem townItem, int orderPriority)
    {
        Id = orderId;
        TargetBuilding = targetBuilding;
        RequiredResources = requiredResources;
        DeliveredResources = new List<CraftingComponent>();
        AssignedUnits = new HashSet<UnitItem>();
        HomeTown = townItem;
        OrderPriority = orderPriority;

        _assignedResources = new Dictionary<Guid, List<CraftingComponent>>();

        foreach (var resource in requiredResources)
        {
            DeliveredResources.Add(new CraftingComponent(resource.BackpackItem, 0));
        }

        TargetBuilding.OnResourcesDelivered += DeliverResources;
        OrderPriority = orderPriority;
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

    public bool AssignUnit(UnitItem unit)
    {
        var remainingResources = GetRemainingResources();

        if (remainingResources.Count == 0)
            return false;

        if (AssignedUnits.Contains(unit))
            return false;

        if (unit.HomeTown.Id != HomeTown.Id)
            return false;

        var resourcesUnitHave = CalculateResourcesUnitHave(unit);
        var resourcesForUnitToTake = CalculateResourcesForUnitToTake(unit);
        if (resourcesForUnitToTake.Count == 0 && resourcesUnitHave.Count == 0)
        {
            UnassignUnit(unit, false);
            return false;
        }

        var buildingsToTakeResources = GetClosestBuildingsWithResources(unit, resourcesForUnitToTake);

        // If unit have resources
        if (resourcesUnitHave.Count > 0)
        {
            // If no buildings to take resources OR distance is closer to target building than to collect resources from buildings
            // Go directly to target building
            if (buildingsToTakeResources.Count == 0 ||
                ShouldGoDirectlyToTarget(unit, TargetBuilding, 
                    buildingsToTakeResources.Select(x => x.Building).ToList(), 
                    resourcesUnitHave, resourcesForUnitToTake))
            {
                AssignedUnits.Add(unit);
                _assignedResources[unit.Id] = resourcesUnitHave;

                _builders.Add(unit);

                if (HomeTown.BuildingTownOrder.Builders.ContainsKey(unit))
                {
                    HomeTown.BuildingTownOrder.Builders[unit] = false;
                }

                var newAction = new MoveResourcesForBuildingAction(unit, TargetBuilding);
                UnitActionManager.Instance.QueueAction(newAction);
            }
        }
        else if(buildingsToTakeResources.Count > 0)
        {
            AssignedUnits.Add(unit);
            _assignedResources[unit.Id] = resourcesForUnitToTake;

            _builders.Add(unit);

            if (HomeTown.BuildingTownOrder.Builders.ContainsKey(unit))
            {
                HomeTown.BuildingTownOrder.Builders[unit] = false;
            }

            var newAction = new MoveResourcesForBuildingAction(unit, TargetBuilding, buildingsToTakeResources);
            UnitActionManager.Instance.QueueAction(newAction);
        }

        return true;
    }

    public bool HasAnyResourceForBuilding()
    {
        var hasResource = false;

        foreach (var resource in RequiredResources)
        {
            var availableInTown = HomeTown.TotalBackpack.GetResourceQuantity(resource.BackpackItem);

            if (availableInTown > 0)
            {
                hasResource = true;
                break;
            }
        }

        return hasResource;
    }

    public void DeliverResources(object sender, ResourceDeliveredArgs args)
    {
        foreach (var deliveredComponent in args.DeliveredComponents)
        {
            var existingComponent = DeliveredResources.FirstOrDefault(c => c.BackpackItem.Id == deliveredComponent.BackpackItem.Id);
            if (existingComponent != null)
            {
                existingComponent.Quantity += deliveredComponent.Quantity;
            }
        }

        AssignedUnits.Remove(args.Unit);
        _assignedResources.Remove(args.Unit.Id);

        CheckCompletion();
    }

    public void Cancel()
    {
        foreach (var unit in AssignedUnits)
        {
            var action = UnitActionManager.Instance.GetCurrentUnitAction(unit);
            if (action is MoveResourcesForBuildingAction moveAction)
            {
                moveAction._interruptedByOrder = true;
                UnitActionManager.Instance.InterruptCurrentAction(unit);
            }
        }

        foreach (var builder in _builders)
        {
            if (HomeTown.BuildingTownOrder.Builders.ContainsKey(builder))
            {
                HomeTown.BuildingTownOrder.Builders[builder] = true;
            }
        }

        TargetBuilding.OnResourcesDelivered -= DeliverResources;

        AssignedUnits.Clear();
        _assignedResources.Clear();
        _builders.Clear();
    }

    public bool HasAssignedUnit(UnitItem unit)
    {
        return AssignedUnits.Contains(unit);
    }

    public void UnassignUnit(UnitItem unit, bool continueToWork = true)
    {
        if (!continueToWork)
        {
            _builders.Remove(unit);
            if (HomeTown.BuildingTownOrder.Builders.ContainsKey(unit))
            {
                HomeTown.BuildingTownOrder.Builders[unit] = true;
            }
        }

        var action = UnitActionManager.Instance.GetCurrentUnitAction(unit);
        if (action is MoveResourcesForBuildingAction moveAction)
        {
            moveAction._interruptedByOrder = true;
            UnitActionManager.Instance.InterruptCurrentAction(unit);
        }

        _assignedResources.Remove(unit.Id);
        AssignedUnits.Remove(unit);
    }

    public void RecalculateAssignUnits()
    {
        if (_builders.Count == 0 && AssignedUnits.Count == 0)
        {
            return;
        }

        var availableBuilders = new List<UnitItem>(_builders);
        var currentlyAssignedUnits = new List<UnitItem>(AssignedUnits);
        var allUnits = new List<UnitItem>(availableBuilders);
        allUnits.AddRange(currentlyAssignedUnits);
        
        var allUnitsSet = allUnits.ToHashSet();

        foreach (var unit in allUnitsSet)
        {
            UnassignUnit(unit);
        }

        var sortedUnits = GetUnitsNotToUpdate(allUnitsSet.ToList());

        foreach (var unit in allUnitsSet)
        {
            if (!sortedUnits.Contains(unit))
            {
                sortedUnits.Add(unit);
            }
        }

        foreach (var unit in sortedUnits)
        {
            AssignUnit(unit);
        }
    }

    public List<UnitItem> GetUnitsNotToUpdate(List<UnitItem> units)
    {
        var result = new List<UnitItem>();

        var otherUnits = new List<UnitItem>();

        foreach (var unit in units)
        {
            var resourcesUnitHave = CalculateResourcesUnitHave(unit);
            var resourcesForUnitToTake = CalculateResourcesForUnitToTake(unit);

            if (resourcesForUnitToTake.Count == 0 && resourcesUnitHave.Count >= 0)
            {
                otherUnits.Add(unit);
            }
        }

        result.AddRange(otherUnits);

        return result;
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

            var availableInTown = HomeTown.TotalBackpack.GetResourceQuantity(resource.BackpackItem);

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
                .ThenBy(b => Vector2.Distance(new Vector2(unit.X, unit.Y), new Vector2(b.X, b.Y)))
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

    private void CheckCompletion()
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
            Cancel();

            var args = new OrderCompletedArgs(this);
            OnCompleted?.Invoke(this, args);
        }
    }

    public class OrderCompletedArgs : EventArgs
    {
        public BuildingOrder BuildingOrder { get; set; }

        public OrderCompletedArgs(BuildingOrder order)
        {
            BuildingOrder = order;
        }
    }
}
