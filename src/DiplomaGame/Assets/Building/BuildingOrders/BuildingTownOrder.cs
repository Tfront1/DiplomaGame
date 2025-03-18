using Assets.Items.Crafts;
using System;
using System.Collections.Generic;
using System.Linq;
using Town;

public class BuildingTownOrder
{
    public Dictionary<UnitItem, bool> Builders { get; } = new();
    private Dictionary<BuildingItem, BuildingOrder> _activeOrders = new();
    private Dictionary<BuildingItem, BuildingOrder> _completedOrders = new();

    private static int test = 1;

    public BuildingOrder CreateOrder(BuildingItem targetBuilding, List<CraftingComponent> requiredResources, TownItem townItem, int orderPriority = 1)
    {
        orderPriority = test;
        test++;
        if (_activeOrders.ContainsKey(targetBuilding))
            return null;

        var order = new BuildingOrder(targetBuilding.Id, targetBuilding, requiredResources, townItem, orderPriority);

        order.OnCompleted += HandleOrderCompleted;

        _activeOrders[targetBuilding] = order;

        var isHighestPriority = !_activeOrders.Values.Any(o => o != order && o.OrderPriority > orderPriority);
        if (isHighestPriority)
        {
            var lowerPriorityOrders = _activeOrders.Values
                .Where(o => o != order && o.OrderPriority < orderPriority)
                .ToList();

            foreach (var lowerOrder in lowerPriorityOrders)
            {
                var busyBuilderItems = Builders
                    .Where(kvp => !kvp.Value)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var builder in busyBuilderItems)
                {
                    ReassignBuilderToHigherPriorityOrder(builder, lowerOrder, order);
                }
            }
        }

        var freeBuilderItems = Builders
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var builder in freeBuilderItems)
        {
            order.AssignUnit(builder);
        }

        return order;
    }

    public void AddBuilder(UnitItem unit)
    {
        Builders.Add(unit, true);

        if (_activeOrders.Count == 0)
            return;

        var highestPriorityOrder = _activeOrders.Values
            .OrderByDescending(o => o.OrderPriority)
            .FirstOrDefault();

        if (highestPriorityOrder != null)
        {
            highestPriorityOrder.AssignUnit(unit);
        }
    }

    public void RemoveBuilder(UnitItem unit)
    {
        foreach (var order in _activeOrders.Values)
        {
            if (order.HasAssignedUnit(unit))
            {
                order.UnassignUnit(unit, false);
                break;
            }
        }

        Builders.Remove(unit);
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
            var isHighestPriority = !_activeOrders.Values.Any(o => o != order && o.OrderPriority > order.OrderPriority);
            if (isHighestPriority)
            {
                var lowerPriorityOrders = _activeOrders.Values
                    .Where(o => o != order && o.OrderPriority < order.OrderPriority)
                    .ToList();

                foreach (var lowerOrder in lowerPriorityOrders)
                {
                    var busyBuilderItems = Builders
                        .Where(kvp => !kvp.Value)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var builder in busyBuilderItems)
                    {
                        ReassignBuilderToHigherPriorityOrder(builder, lowerOrder, order);
                    }
                }
            }

            var freeBuilderItems = Builders
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var builder in freeBuilderItems)
            {
                order.AssignUnit(builder);
            }
            
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
        UnassignUnitFromOrder(unit);

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
                order.UnassignUnit(unit, false);
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

    private void ReassignBuilderToHigherPriorityOrder(UnitItem builder, BuildingOrder oldOrder, BuildingOrder newOrder)
    {
        oldOrder.UnassignUnit(builder, false);

        if (newOrder.AssignUnit(builder))
        {
            Builders[builder] = false;
        }
        else
        {
            Builders[builder] = true;
        }
    }

    private void HandleOrderCompleted(object sender, BuildingOrder.OrderCompletedArgs args)
    {
        var order = args.BuildingOrder;
        var targetBuilding = order.TargetBuilding;

        if (_activeOrders.ContainsKey(targetBuilding))
        {
            UnsubscribeFromEvents(order);
            _activeOrders.Remove(targetBuilding);
            _completedOrders[targetBuilding] = order;

            var freeBuilderItems = Builders
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            if (_activeOrders.Count == 0)
                return;

            var highestPriorityOrder = _activeOrders.Values
                .OrderByDescending(o => o.OrderPriority)
                .FirstOrDefault();

            if (highestPriorityOrder != null)
            {
                freeBuilderItems.ForEach(x => highestPriorityOrder.AssignUnit(x));
            }
        }
    }

    private void UnsubscribeFromEvents(BuildingOrder order)
    {
        order.OnCompleted -= HandleOrderCompleted;
    }
}