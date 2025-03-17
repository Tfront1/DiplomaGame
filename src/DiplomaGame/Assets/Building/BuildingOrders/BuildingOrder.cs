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

        targetBuilding.OnResourcesDelivered += DeliverResources;
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

        var resourcesForUnit = CalculateResourcesForUnitToTake(unit);

        if (resourcesForUnit.Count == 0)
            return false;

        AssignedUnits.Add(unit);
        _assignedResources[unit.Id] = resourcesForUnit;
        
        return true;
    }

    public bool AssignUnitWithHisResources(UnitItem unit)
    {
        var remainingResources = GetRemainingResources();
        if (remainingResources.Count == 0)
            return false;

        if (AssignedUnits.Contains(unit))
            return false;

        if (unit.HomeTown.Id != HomeTown.Id)
            return false;

        var unitResources = CalculateResourcesUnitHave(unit);

        if (unitResources.Count == 0)
            return false;

        AssignedUnits.Add(unit);
        _assignedResources[unit.Id] = unitResources;

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

        Debug.Log($"All resources delivered Order {TargetBuilding.Backpack}");

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
        _assignedResources.Remove(unit.Id);
        AssignedUnits.Remove(unit);
    }

    public List<CraftingComponent> CalculateResourcesUnitHave(UnitItem unit)
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

    public List<CraftingComponent> CalculateResourcesForUnitToTake(UnitItem unit)
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

    public void RecalculateAssignUnits()
    {
        var assignedUnitsCopy = new List<UnitItem>(AssignedUnits);
        Cancel();

        foreach (var unit in assignedUnitsCopy)
        {
            if (GetRemainingResources().Count != 0)
            {
                var newAction = new MoveResourcesForBuildingAction(unit, TargetBuilding);
                UnitActionManager.Instance.QueueAction(newAction);
            }
        }
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
