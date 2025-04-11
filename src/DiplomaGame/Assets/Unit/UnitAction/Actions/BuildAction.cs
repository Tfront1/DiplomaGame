using System;
using System.Collections;
using UnityEngine;

namespace UnitAction
{
    public class BuildAction : BaseUnitAction
    {
        private BuildingItem _targetBuilding;
        public bool InterruptedByOrder { get; set; } = false;

        private float _fixedDeltaTime = 0.5f;
        private bool _movementCompleted = false;
        private bool _movementSuccess = false;

        public BuildAction(UnitItem unit, BuildingItem targetBuilding) : base(unit)
        {
            _targetBuilding = targetBuilding;
        }

        public override bool CanExecute()
        {
            return true;
        }

        public override void Execute()
        {
            if (!CanExecute())
            {
                Debug.Log("Can`t execute building.");
                return;
            }

            _targetBuilding.OnDestroyed += OnBuildingDestroyed;

            _idAction = Guid.NewGuid();
            CoroutineRunner.Instance.StartCoroutineWithId(_idAction, Build());
        }

        private IEnumerator Build()
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

            var buildingSkill = _unit.Skills.GetSkill<BuildingSkill>();
            if (buildingSkill != null)
            {
                _fixedDeltaTime *= buildingSkill.BuildSpeedBonus;
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
                while (!_targetBuilding.IsAllDelivered)
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

                    yield return null;
                }

                _unit.Skills.SetActiveSkill(typeof(BuildingSkill));

                while (!_targetBuilding.UpdateBuildingProgress(_fixedDeltaTime, _unit))
                {
                    if (IsStopped)
                    {
                        _unit.Skills.ResetActiveSkill();
                        CompleteAction();
                        yield break;
                    }

                    if (IsPaused)
                    {
                        _unit.Skills.ResetActiveSkill();

                        while (IsPaused)
                        {
                            yield return null;
                        }

                        _unit.Skills.SetActiveSkill(typeof(BuildingSkill));
                    }

                    yield return new WaitForSeconds(0.5f);
                }

                _unit.Skills.ResetActiveSkill();
            }
            else
            {
                CompleteAction();
            }

            _isSuccessAction = true;
            CompleteAction();
        }

        public override void Cancel()
        {
            UnassignUnit();
            base.Cancel();
        }

        protected override void CompleteAction()
        {
            _targetBuilding.OnDestroyed -= OnBuildingDestroyed;

            if (!InterruptedByOrder)
            {
                UnassignUnit();
            }
            else if (!IsSuccess)
            {
                UnassignUnit();
            }
            base.CompleteAction();
        }

        private void UnassignUnit()
        {
            var buildingOrder = _targetBuilding.HomeTown.BuildingTownOrder.GetOrder(_targetBuilding);
            if (!InterruptedByOrder && buildingOrder.HasAssignedUnit(_unit))
            {
                buildingOrder.UnassignUnit(_unit, false);
            }
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

        private void OnMovementComplete(IUnitAction action)
        {
            if (action is BaseUnitAction baseAction)
            {
                _movementSuccess = baseAction.IsSuccess;
            }
            _movementCompleted = true;
        }

        private void OnBuildingDestroyed(object sender, BuildingItem.BuildingDestroyedEventArgs args)
        {
            Cancel();
        }
    }
}
