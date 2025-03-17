using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Items.Crafts;
using UnityEngine;

public class MoveResourcesForBuildingAction : BaseUnitAction
{
    private BuildingItem _targetBuilding;
    private BuildingOrder _buildingOrder;
    private bool _movementCompleted = false;
    private bool _movementSuccess = false;
    private List<CraftingComponent> _resourcesUnitHave;
    private List<CraftingComponent> _resourcesUnitToTake;
    
    public bool _interruptedByOrder = false;

    public MoveResourcesForBuildingAction(UnitItem unit, BuildingItem targetBuilding) : base(unit)
    {
        _targetBuilding = targetBuilding;
        _buildingOrder = targetBuilding.HomeTown.BuildingTownOrder.GetOrder(_targetBuilding);
    }

    public override bool CanExecute()
    {
        _resourcesUnitHave = _buildingOrder.CalculateResourcesUnitHave(_unit);
        _resourcesUnitToTake = _buildingOrder.CalculateResourcesForUnitToTake(_unit);
        if ((_resourcesUnitHave.Count == 0 && _resourcesUnitToTake.Count == 0) || _buildingOrder.GetRemainingResources().Count == 0)
        {
            return false;
        }
        return true;
    }

    public override void Execute()
    {
        if (!CanExecute())
        {
            Debug.Log("Can`t execute moving resources for building. No resources");
            return;
        }

        _idAction = Guid.NewGuid();
        CoroutineRunner.Instance.StartCoroutineWithId(_idAction, MoveResourcesForBuilding());
    }

    private IEnumerator MoveResourcesForBuilding()
    {
        List<BuildingItem> buildingsToTakeResources = new();

        if (_resourcesUnitToTake.Count != 0)
        {
            buildingsToTakeResources = GetClosestBuildingsWithResources(_resourcesUnitToTake);
        }

        if (IsStopped)
        {
            CompleteAction();
            yield break;
        }

        while (IsPaused)
        {
            yield return null;
        }

        // If unit have resources
        if (_resourcesUnitHave.Count > 0)
        {
            // If no buildings to take resources OR distance is closer to target building than to collect resources from buildings
            if (buildingsToTakeResources.Count == 0 ||
                ShouldGoDirectlyToTarget(buildingsToTakeResources, _resourcesUnitHave, _resourcesUnitToTake))
            {
                _targetBuilding.HomeTown.BuildingTownOrder.AssignUnitWithHisResourcesToOrder(_unit, _targetBuilding);

                yield return MoveToPoint(GridService.GetWorldPosition(_targetBuilding.X, _targetBuilding.Y));

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
                    _targetBuilding.AddBuildingMaterials(_unit);
                    Debug.Log($"All resources delivered Action {_unit.Backpack}");
                }
                else
                {
                    CompleteAction();
                }
            }
            else
            {
                _targetBuilding.HomeTown.BuildingTownOrder.AssignUnitToOrder(_unit, _targetBuilding);

                foreach (var building in buildingsToTakeResources)
                {
                    yield return MoveToPoint(GridService.GetWorldPosition(building.X, building.Y));

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
                        BackpackTransfer.Instance.TransferSpecificResources(building.Backpack, _unit.Backpack, _resourcesUnitToTake);
                    }
                    else
                    {
                        CompleteAction();
                    }
                }

                yield return MoveToPoint(GridService.GetWorldPosition(_targetBuilding.X, _targetBuilding.Y));

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
                    _targetBuilding.AddBuildingMaterials(_unit);
                    Debug.Log($"All resources delivered Action {_unit.Backpack}");
                }
                else
                {
                    CompleteAction();
                }
            }
        }
        else if (buildingsToTakeResources.Count > 0)
        {
            _targetBuilding.HomeTown.BuildingTownOrder.AssignUnitToOrder(_unit, _targetBuilding);

            foreach (var building in buildingsToTakeResources)
            {
                yield return MoveToPoint(GridService.GetWorldPosition(building.X, building.Y));

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
                    BackpackTransfer.Instance.TransferSpecificResources(building.Backpack, _unit.Backpack, _resourcesUnitToTake);
                }
                else
                {
                    CompleteAction();
                }
            }

            yield return MoveToPoint(GridService.GetWorldPosition(_targetBuilding.X, _targetBuilding.Y));

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
                _targetBuilding.AddBuildingMaterials(_unit);
                Debug.Log($"All resources delivered Unit: {_unit.Backpack}");
            }
            else
            {
                CompleteAction();
            }
        }
        else
        {
            CompleteAction();
        }

        _isSuccessAction = true;
        CompleteAction();
    }

    private void OnMovementComplete(IUnitAction action)
    {
        if (action is BaseUnitAction baseAction)
        {
            _movementSuccess = baseAction.IsSuccess;
        }
        _movementCompleted = true;
    }

    private List<BuildingItem> GetClosestBuildingsWithResources(List<CraftingComponent> resourcesToTake)
    {
        var result = new List<BuildingItem>();
        if (resourcesToTake.Count == 0)
        {
            return result;
        }

        var buildingsWithAllResources = _unit.HomeTown.Buildings
            .Where(b => b.Backpack != null && b.IsBuilt)
            .Where(b => resourcesToTake.All(r =>
                b.Backpack.HasResource(r.BackpackItem) &&
                b.Backpack.GetResourceQuantity(r.BackpackItem) >= r.Quantity))
            .OrderBy(b => Vector2.Distance(new Vector2(_unit.X, _unit.Y), GridService.GetWorldPosition(b.X, b.Y)))
            .ToList();

        if (buildingsWithAllResources.Count > 0)
        {
            var closestVault = buildingsWithAllResources.First();
            result.Add(closestVault);
            return result;
        }

        var visitedBuildings = new HashSet<BuildingItem>();

        while (resourcesToTake.Count > 0)
        {
            var bestBuilding = _unit.HomeTown.Buildings
                .Where(b => b.Backpack != null && b.IsBuilt && !visitedBuildings.Contains(b))
                .Where(b => resourcesToTake.Any(r =>
                    b.Backpack.HasResource(r.BackpackItem) &&
                    b.Backpack.GetResourceQuantity(r.BackpackItem) > 0))
                .OrderByDescending(b => resourcesToTake.Sum(r =>
                    b.Backpack.HasResource(r.BackpackItem) ?
                        Math.Min(b.Backpack.GetResourceQuantity(r.BackpackItem), r.Quantity) : 0))
                .ThenBy(b => Vector2.Distance(new Vector2(_unit.X, _unit.Y), new Vector2(b.X, b.Y)))
                .FirstOrDefault();

            if (bestBuilding == null) break;

            result.Add(bestBuilding);
            visitedBuildings.Add(bestBuilding);

            foreach (var resource in resourcesToTake.ToList())
            {
                if (bestBuilding.Backpack.HasResource(resource.BackpackItem))
                {
                    var available = bestBuilding.Backpack.GetResourceQuantity(resource.BackpackItem);
                    var toTake = Math.Min(available, resource.Quantity);

                    if (toTake > 0)
                    {
                        resource.Quantity -= toTake;
                        if (resource.Quantity <= 0)
                        {
                            resourcesToTake.Remove(resource);
                        }
                    }
                }
            }

            if (resourcesToTake.Count == 0)
            {
                break;
            }
        }

        return result;
    }

    private bool ShouldGoDirectlyToTarget(List<BuildingItem> resourceBuildings, List<CraftingComponent> currentResources, List<CraftingComponent> resourcesToTake)
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

        var distanceToTarget = Vector2.Distance(GridService.GetWorldPosition(_targetBuilding.X, _targetBuilding.Y), _unit.Coords);
        var distanceToResourceBuilding = Vector2.Distance(GridService.GetWorldPosition(resourceBuildings.First().X, resourceBuildings.First().Y), _unit.Coords);
        var distanceFromResourceToTarget = Vector2.Distance(GridService.GetWorldPosition(resourceBuildings.First().X, resourceBuildings.First().Y), GridService.GetWorldPosition(_targetBuilding.X, _targetBuilding.Y));
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

    public override void Cancel()
    {
        UnassignUnit();
        base.Cancel();
    }

    protected override void CompleteAction()
    {
        if (!IsSuccess)
        {
            UnassignUnit();
        }
        base.CompleteAction();
    }

    private void UnassignUnit()
    {
        if (!_interruptedByOrder && _buildingOrder.HasAssignedUnit(_unit))
        {
            _buildingOrder.UnassignUnit(_unit);
        }
    }
}
