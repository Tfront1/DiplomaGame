using Assets.Items.Crafts;
using System;
using System.Collections.Generic;
using System.Linq;
using Town;

public class BuildingTownOrder
{
    private Dictionary<BuildingItem, BuildingOrder> _activeOrders = new();
    private Dictionary<BuildingItem, BuildingOrder> _completedOrders = new();

    public BuildingOrder CreateOrder(BuildingItem targetBuilding, List<CraftingComponent> requiredResources, TownItem townItem, int orderPriority = 1)
    {
        if (_activeOrders.ContainsKey(targetBuilding))
            return null;

        var order = new BuildingOrder(targetBuilding.Id, targetBuilding, requiredResources, townItem, orderPriority);

        order.OnCompleted += HandleOrderCompleted;

        _activeOrders[targetBuilding] = order;

        return order;
    }

    public BuildingOrder GetOrder(BuildingItem building)
    {
        if (_activeOrders.TryGetValue(building, out var order))
        {
            return order;
        }

        if (_completedOrders.TryGetValue(building, out order))
        {
            return order;
        }

        return null;
    }

    public BuildingOrder GetOrderById(Guid orderId)
    {
        var activeOrder = _activeOrders.Values.FirstOrDefault(o => o.Id == orderId);
        return activeOrder;
    }

    public List<BuildingOrder> GetActiveOrders()
    {
        return _activeOrders.Values.ToList();
    }

    public List<BuildingOrder> GetCompletedOrders()
    {
        return _completedOrders.Values.ToList();
    }

    public BuildingOrder GetOrderForBuilding(BuildingItem building)
    {
        return _activeOrders.GetValueOrDefault(building);
    }

    public void ChangeOrderPriority(int priority, BuildingOrder order)
    {
        if (priority > 0 && priority < 10)
        {
            order.OrderPriority = priority;
        }
    }

    public void CancelOrder(BuildingItem building)
    {
        if (_activeOrders.TryGetValue(building, out var order))
        {
            order.Cancel();
            UnsubscribeFromEvents(order);
            _activeOrders.Remove(building);
        }
    }

    public void CancelOrderById(Guid orderId)
    {
        var order = _activeOrders.Values.FirstOrDefault(o => o.Id == orderId);
        if (order != null)
        {
            order.Cancel();
            UnsubscribeFromEvents(order);
            _activeOrders.Remove(order.TargetBuilding);
        }
    }

    public void RecalculateAllOrders(TownItem town)
    {
        foreach (var order in _activeOrders.Values)
        {
            order.RecalculateAssignUnits();
        }
    }

    public bool AssignUnitToOrder(UnitItem unit, BuildingItem building)
    {
        if (_activeOrders.TryGetValue(building, out var order))
        {
            return order.AssignUnit(unit);
        }

        return false;
    }

    public bool AssignUnitToHighestPriorityOrder(UnitItem unit)
    {
        var priorityOrders = _activeOrders.Values
            .OrderByDescending(o => o.OrderPriority)
            .ToList();

        foreach (var order in priorityOrders)
        {
            if (order.AssignUnit(unit))
            {
                return true;
            }
        }

        return false;
    }

    public bool UnassignUnitFromOrder(UnitItem unit)
    {
        foreach (var order in _activeOrders.Values)
        {
            if (order.HasAssignedUnit(unit))
            {
                order.UnassignUnit(unit);
                UnitActionManager.Instance.InterruptCurrentAction(unit);
                return true;
            }
        }

        return false;
    }

    public void RemoveOrdersForBuilding(BuildingItem building)
    {
        if (_activeOrders.TryGetValue(building, out var order))
        {
            UnsubscribeFromEvents(order);

            order.Cancel();
            _activeOrders.Remove(building);
        }
    }

    public void ClearAllOrders()
    {
        foreach (var order in _activeOrders.Values)
        {
            UnsubscribeFromEvents(order);
            order.Cancel();
        }

        _activeOrders.Clear();
        _completedOrders.Clear();
    }

    private void HandleOrderCompleted(object sender, BuildingOrder.OrderCompletedArgs args)
    {
        var order = args.BuildingOrder;
        var targetBuilding = order.TargetBuilding;

        if (_activeOrders.ContainsKey(targetBuilding))
        {
            _activeOrders.Remove(targetBuilding);
            _completedOrders[targetBuilding] = order;
        }
    }

    private void UnsubscribeFromEvents(BuildingOrder order)
    {
        order.OnCompleted -= HandleOrderCompleted;
    }
}