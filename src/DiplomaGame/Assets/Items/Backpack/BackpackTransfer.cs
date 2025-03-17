using Assets.Items.Crafts;
using Items.Resource.BackPack;
using System.Collections.Generic;
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
                fromBackpack.RemoveResource(resourceType, amountToTransfer);
            }
        }
    }
}
