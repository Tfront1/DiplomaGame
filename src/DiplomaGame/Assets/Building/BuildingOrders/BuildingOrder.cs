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
    public List<UnitItem> AssignedUnits { get; }
    public TownItem HomeTown { get; }
    public bool IsStopped { get; set; } = false;
    public int OrderPriority { get; set; }

    private Dictionary<Guid, List<CraftingComponent>> _assignedResources;

    public event EventHandler<OrderCompletedArgs> OnCompleted;

    public BuildingOrder(Guid orderId, BuildingItem targetBuilding, List<CraftingComponent> requiredResources, TownItem townItem, int orderPriority)
    {
        Id = orderId;
        TargetBuilding = targetBuilding;
        RequiredResources = requiredResources;
        DeliveredResources = new List<CraftingComponent>();
        AssignedUnits = new List<UnitItem>();
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

                var newAction = new MoveResourcesForBuildingAction(unit, TargetBuilding);
                UnitActionManager.Instance.QueueAction(newAction);

                return true;
            }
        }
        else if(buildingsToTakeResources.Count > 0)
        {
            AssignedUnits.Add(unit);
            _assignedResources[unit.Id] = resourcesForUnitToTake;

            var newAction = new MoveResourcesForBuildingAction(unit, TargetBuilding, buildingsToTakeResources);
            UnitActionManager.Instance.QueueAction(newAction);

            return true;
        }

        if (resourcesForUnitToTake.Count == 0)
            return false;
        
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
            }

            UnitActionManager.Instance.InterruptCurrentAction(unit);
        }

        TargetBuilding.OnResourcesDelivered -= DeliverResources;

        AssignedUnits.Clear();
        _assignedResources.Clear();
    }

    public bool HasAssignedUnit(UnitItem unit)
    {
        return AssignedUnits.Contains(unit);
    }

    public void UnassignUnit(UnitItem unit)
    {
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
        var assignedUnitsCopy = new List<UnitItem>(AssignedUnits);
        foreach (var unit in assignedUnitsCopy)
        {
            UnassignUnit(unit);
        }

        foreach (var unit in assignedUnitsCopy)
        {
            AssignUnit(unit);
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

        // Створюємо глибоку копію списку ресурсів, щоб не змінювати оригінальний список
        var remainingResources = resourcesToTake.Select(r => 
            new CraftingComponent(r.BackpackItem, r.Quantity))
            .ToList();

        // Перевіряємо, чи є будівлі, які містять всі необхідні ресурси
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
            // Для цієї будівлі ми беремо всі необхідні ресурси
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

            // Список ресурсів, які будуть взяті з цієї будівлі
            var resourcesFromThisBuilding = new List<CraftingComponent>();

            foreach (var resource in remainingResources.ToList())
            {
                if (bestBuilding.Backpack.HasResource(resource.BackpackItem))
                {
                    var available = bestBuilding.Backpack.GetResourceQuantity(resource.BackpackItem);
                    var toTake = Math.Min(available, resource.Quantity);

                    if (toTake > 0)
                    {
                        // Додаємо ресурс до списку ресурсів, які будуть взяті з цієї будівлі
                        resourcesFromThisBuilding.Add(new CraftingComponent(resource.BackpackItem, toTake));

                        // Оновлюємо залишковий список ресурсів
                        resource.Quantity -= toTake;
                        if (resource.Quantity <= 0)
                        {
                            remainingResources.Remove(resource);
                        }
                    }
                }
            }

            // Додаємо будівлю та список ресурсів, які будуть з неї взяті
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
