using Assets.Items.Crafts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BringBackResourcesAction : BaseUnitAction
{
    private List<(BuildingItem Building, List<CraftingComponent> ResourcesToBring)> _buildingsToGo;
    private bool _movementCompleted = false;
    private bool _movementSuccess = false;

    public BringBackResourcesAction(UnitItem unit) : base(unit)
    {
        _buildingsToGo = GetBuildingsToBringResources(unit);
    }

    public override bool CanExecute()
    {
        if (_unit.Backpack.IsEmpty() || _buildingsToGo.Count == 0)
        {
            return false;
        }

        return true;
    }

    public override void Execute()
    {
        if (!CanExecute())
        {
            Debug.Log("Can`t execute bring back resources. Unit don`t have any resources or storages is full");
            return;
        }

        _idAction = Guid.NewGuid();
        CoroutineRunner.Instance.StartCoroutineWithId(_idAction, BringBackResources());
    }
    
    private IEnumerator BringBackResources()
    {
        if (IsStopped)
        {
            CompleteAction();
            yield break;
        }

        while (IsPaused)
        {
            yield return null;
        }

        foreach (var item in _buildingsToGo)
        {
            yield return MoveToPoint(GridService.GetWorldPosition(item.Building.X, item.Building.Y));

            if (IsStopped)
            {
                CompleteAction();
                yield break;
            }

            while (IsPaused)
            {
                yield return null;
            }

            if (_movementSuccess)
            {
                BackpackTransfer.Instance.TransferSpecificResources(_unit.Backpack, item.Building.Backpack,
                    item.ResourcesToBring);
            }
            else
            {
                CompleteAction();
            }
        }

        _isSuccessAction = true;
        CompleteAction();
    }

    private List<(BuildingItem Building, List<CraftingComponent> ResourcesToBring)> GetBuildingsToBringResources(UnitItem unit)
    {
        var result = new List<(BuildingItem Building, List<CraftingComponent> ResourcesToBring)>();
        var unitBackpack = unit.Backpack;
        var unitTown = unit.HomeTown;
        var resourcesToBring = new List<CraftingComponent>();

        foreach (var resourceType in unitBackpack.GetDetailedItems())
        {
            var quantity = unitBackpack.GetResourceQuantity(resourceType.Key);
            if (quantity > 0)
            {
                resourcesToBring.Add(new CraftingComponent(resourceType.Key, quantity));
            }
        }

        if (resourcesToBring.Count == 0)
        {
            return result;
        }

        if (unitTown.TownHall != null && unitTown.TownHall.IsBuilt && unitTown.TownHall.Backpack != null)
        {
            var townHall = unitTown.TownHall;

            if (townHall.Backpack.CurrentCapacity + unitBackpack.CurrentCapacity <= townHall.Backpack.MaxCapacity)
            {
                result.Add((townHall, new List<CraftingComponent>(resourcesToBring)));
                return result;
            }

            var availableSpace = townHall.Backpack.MaxCapacity - townHall.Backpack.CurrentCapacity;
            if (availableSpace > 0)
            {
                var resourcesForTownHall = new List<CraftingComponent>();
                var remainingResources = new List<CraftingComponent>();
                var currentAvailableSpace = availableSpace;

                foreach (var resource in resourcesToBring)
                {
                    var toStoreQuantity = Math.Min(resource.Quantity, currentAvailableSpace);
                    if (toStoreQuantity > 0)
                    {
                        resourcesForTownHall.Add(new CraftingComponent(resource.BackpackItem, toStoreQuantity));
                        var remaining = resource.Quantity - toStoreQuantity;
                        currentAvailableSpace -= toStoreQuantity;

                        if (remaining > 0)
                        {
                            remainingResources.Add(new CraftingComponent(resource.BackpackItem, remaining));
                        }

                        if (currentAvailableSpace <= 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        remainingResources.Add(new CraftingComponent(resource.BackpackItem, resource.Quantity));
                    }
                }

                if (resourcesForTownHall.Count > 0)
                {
                    result.Add((townHall, resourcesForTownHall));
                }

                if (remainingResources.Count == 0)
                {
                    return result;
                }

                resourcesToBring = remainingResources;
            }
        }

        var buildingsWithEnoughSpace = unitTown.Buildings
            .Where(building => building != unitTown.TownHall)
            .Where(building => building.Backpack != null && building.IsBuilt)
            .Where(building => building.Backpack.CurrentCapacity + unitBackpack.CurrentCapacity <= building.Backpack.MaxCapacity)
            .OrderBy(building => Vector2.Distance(new Vector2(unit.X, unit.Y), GridService.GetWorldPosition(building.X, building.Y)))
            .ToList();

        if (buildingsWithEnoughSpace.Count > 0)
        {
            result.Add((buildingsWithEnoughSpace.First(), new List<CraftingComponent>(resourcesToBring)));
            return result;
        }

        var remainingResources2 = resourcesToBring.Select(r =>
            new CraftingComponent(r.BackpackItem, r.Quantity))
            .ToList();

        var visitedBuildings = new HashSet<BuildingItem>();

        if (unitTown.TownHall != null && result.Any(r => r.Building == unitTown.TownHall))
        {
            visitedBuildings.Add(unitTown.TownHall);
        }

        while (remainingResources2.Count > 0)
        {
            var bestBuilding = unitTown.Buildings
                .Where(building => building.Backpack != null && building.IsBuilt && !visitedBuildings.Contains(building))
                .Where(building => building.Backpack.MaxCapacity > building.Backpack.CurrentCapacity)
                .OrderBy(building => Vector2.Distance(new Vector2(unit.X, unit.Y), GridService.GetWorldPosition(building.X, building.Y)))
                .FirstOrDefault();

            if (bestBuilding == null) break;

            visitedBuildings.Add(bestBuilding);
            var availableSpace = bestBuilding.Backpack.MaxCapacity - bestBuilding.Backpack.CurrentCapacity;
            var resourcesForThisBuilding = new List<CraftingComponent>();
            var currentAvailableSpace = availableSpace;

            foreach (var resource in remainingResources2.ToList())
            {
                var toStoreQuantity = Math.Min(resource.Quantity, currentAvailableSpace);
                if (toStoreQuantity > 0)
                {
                    resourcesForThisBuilding.Add(new CraftingComponent(resource.BackpackItem, toStoreQuantity));
                    resource.Quantity -= toStoreQuantity;
                    currentAvailableSpace -= toStoreQuantity;

                    if (resource.Quantity <= 0)
                    {
                        remainingResources2.Remove(resource);
                    }

                    if (currentAvailableSpace <= 0)
                    {
                        break;
                    }
                }
            }

            if (resourcesForThisBuilding.Count > 0)
            {
                result.Add((bestBuilding, resourcesForThisBuilding));
            }
        }

        return result;
    }
    private void OnMovementComplete(IUnitAction action)
    {
        if (action is BaseUnitAction baseAction)
        {
            _movementSuccess = baseAction.IsSuccess;
        }
        _movementCompleted = true;
    }

    private IEnumerator MoveToPoint(Vector2 targetPosition)
    {
        targetPosition.x += MapConfig.CellSize / 2;
        targetPosition.y += MapConfig.CellSize / 2;

        _movementCompleted = false;
        _movementSuccess = false;

        if (IsStopped)
        {
            yield break;
        }

        var moveAction = new MoveUnitAction(_unit, targetPosition, true);
        moveAction.OnActionCompleted += OnMovementComplete;
        moveAction.Execute();

        var wasPaused = false;

        while (!_movementCompleted)
        {
            if (IsStopped)
            {
                moveAction.Cancel();
                moveAction.OnActionCompleted -= OnMovementComplete;
                yield break;
            }

            if (IsPaused)
            {
                if (!wasPaused)
                {
                    moveAction.Pause();
                    wasPaused = true;
                }
                yield return null;
            }
            else
            {
                if (wasPaused)
                {
                    moveAction.Resume();
                    wasPaused = false;
                }
                yield return null;
            }
        }

        moveAction.OnActionCompleted -= OnMovementComplete;
    }
}
