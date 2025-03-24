using System;
using Assets.Items.Crafts;
using Items.Resource.BackPack;
using System.Collections.Generic;
using Assets.Items.Interfaces;
using Town;
using UnityEngine;

public class BackpackTransfer
{
    private static BackpackTransfer _instance;
    private static readonly object _lock = new();

    public static BackpackTransfer Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new BackpackTransfer();
                }
                return _instance;
            }
        }
    }

    public void TransferSpecificResources(Backpack fromBackpack, Backpack toBackpack, List<CraftingComponent> components)
    {
        foreach (var component in components)
        {
            var resourceType = component.BackpackItem;
            var requiredAmount = component.Quantity;

            var availableResources = fromBackpack.GetDetailedItems();
            if (!availableResources.ContainsKey(resourceType) || availableResources[resourceType] <= 0)
                continue;

            var availableAmount = availableResources[resourceType];

            var targetFreeSpace = toBackpack.GetFreeQuantity();

            var amountToTransfer = Mathf.Min(requiredAmount, availableAmount, targetFreeSpace);

            if (amountToTransfer > 0)
            {
                toBackpack.AddItem(resourceType, amountToTransfer);
                fromBackpack.RemoveItem(resourceType, amountToTransfer);
            }
        }
    }

    public void TransferSpecificResource(Backpack fromBackpack, Backpack toBackpack, BackpackItem item)
    {
        var resourceType = item.Item;
        var requiredAmount = item.Quantity;

        var availableResources = fromBackpack.GetDetailedItems();
        if (!availableResources.ContainsKey(resourceType) || availableResources[resourceType] <= 0)
            return;

        var availableAmount = availableResources[resourceType];

        var targetFreeSpace = toBackpack.GetFreeQuantity();

        var amountToTransfer = Mathf.Min(requiredAmount, availableAmount, targetFreeSpace);

        if (amountToTransfer > 0)
        {
            toBackpack.AddItem(resourceType, amountToTransfer);
            fromBackpack.RemoveItem(resourceType, amountToTransfer);
        }
    }

    public void RemoveSpecificResourcesFromTown(TownItem town, List<CraftingComponent> components)
    {
        foreach (var component in components)
        {
            var resourceType = component.BackpackItem;
            var remainingToRemove = component.Quantity;

            foreach (var building in town.Buildings)
            {
                if (building.Backpack == null || remainingToRemove <= 0)
                    continue;

                var availableResources = building.Backpack.GetDetailedItems();

                if (!availableResources.ContainsKey(resourceType) || availableResources[resourceType] <= 0)
                    continue;

                var availableAmount = availableResources[resourceType];

                var amountToRemove = Math.Min(availableAmount, remainingToRemove);

                building.Backpack.RemoveItem(resourceType, amountToRemove);

                remainingToRemove -= amountToRemove;

                if (remainingToRemove <= 0)
                    break;
            }
        }
    }

    public bool AddItemToTownHall(TownItem town, IBackpackItem item)
    {
        if (town == null || item == null || town.TownHall == null || town.TownHall.Backpack == null)
            return false;

        if (town.TownHall.Backpack.AddItem(item))
            return true;

        return false;
    }

    public bool AddItemToTown(TownItem town, IBackpackItem item)
    {
        if (town == null || item == null)
            return false;

        foreach (var building in town.Buildings)
        {
            if (building.Backpack == null)
                continue;

            if (building.Backpack.AddItem(item))
                return true;
        }

        return false;
    }
}
