using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Items.Crafts;
using UnityEngine;

namespace UnitAction
{
    public class TransportResourcesForBuildingAction : BaseUnitAction
    {
        private BuildingItem _targetBuilding;
        private BuildingOrder _buildingOrder;
        private bool _movementCompleted = false;
        private bool _movementSuccess = false;
        private List<(BuildingItem, List<CraftingComponent>)> _buildingsToTakeResources;

        public bool InterruptedByOrder { get; set; } = false;

        public TransportResourcesForBuildingAction(UnitItem unit, BuildingItem targetBuilding, List<(BuildingItem, List<CraftingComponent>)> buildingsToTakeResources = null) : base(unit)
        {
            _targetBuilding = targetBuilding;
            _buildingOrder = targetBuilding.HomeTown.BuildingTownOrder.GetOrder(_targetBuilding);
            _buildingsToTakeResources = buildingsToTakeResources;
        }

        public override bool CanExecute()
        {
            return true;
        }

        public override void Execute()
        {
            if (!CanExecute())
            {
                Debug.Log("Can`t execute moving resources for building. No resources");
                return;
            }

            _targetBuilding.OnDestroyed += OnBuildingDestroyed;

            _idAction = Guid.NewGuid();
            CoroutineRunner.Instance.StartCoroutineWithId(_idAction, MoveResourcesForBuilding());
        }

        private IEnumerator MoveResourcesForBuilding()
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

            if (_buildingsToTakeResources != null)
            {
                foreach (var building in _buildingsToTakeResources)
                {
                    yield return MoveToPoint(GridService.GetWorldPosition(building.Item1.X, building.Item1.Y));

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
                        BackpackTransfer.Instance.TransferSpecificResources(building.Item1.Backpack, _unit.Backpack,
                            building.Item2);
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
                    if (!_targetBuilding.IsBuilt && _buildingOrder != null)
                    {
                        _targetBuilding.AddBuildingMaterials(_unit);
                    }
                    else if (_targetBuilding.BuildingCraftingSystem != null)
                    {
                        _targetBuilding.BuildingCraftingSystem.AddCraftingMaterials(_unit);
                    }
                }
                else
                {
                    CompleteAction();
                }
            }
            else
            {
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
                    if (!_targetBuilding.IsBuilt && _buildingOrder != null)
                    {
                        _targetBuilding.AddBuildingMaterials(_unit);
                    }
                    else if (_targetBuilding.BuildingCraftingSystem != null)
                    {
                        _targetBuilding.BuildingCraftingSystem.AddCraftingMaterials(_unit);
                    }
                }
                else
                {
                    CompleteAction();
                }
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
            _targetBuilding.OnDestroyed -= OnBuildingDestroyed;
            if (!IsSuccess)
            {
                UnassignUnit();
            }
            base.CompleteAction();
        }

        private void UnassignUnit()
        {
            if (!_targetBuilding.IsBuilt && _buildingOrder != null)
            {
                if (!InterruptedByOrder && _buildingOrder.HasAssignedUnit(_unit))
                {
                    _buildingOrder.UnassignUnit(_unit, false);
                }
            }
            else if (_targetBuilding.BuildingCraftingSystem != null)
            {
                if (!InterruptedByOrder && _targetBuilding.BuildingCraftingSystem.HasAssignedUnit(_unit))
                {
                    _targetBuilding.BuildingCraftingSystem.UnassignUnitFromCraft(_unit, false);
                }
            }
        }

        private void OnBuildingDestroyed(object sender, BuildingItem.BuildingDestroyedEventArgs args)
        {
            Cancel();
        }
    }
}
