using System;
using System.Collections;
using UnityEngine;

namespace UnitAction
{
    public class BringBackToBuildingResourcesAction : BaseUnitAction
    {
        private bool _movementCompleted = false;
        private bool _movementSuccess = false;
        private BuildingItem _targetBuilding;

        public BringBackToBuildingResourcesAction(UnitItem unit, BuildingItem building) : base(unit)
        {
            _targetBuilding = building;
        }

        public override bool CanExecute()
        {
            if (_unit.Backpack.IsEmpty() || _targetBuilding.Backpack == null)
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
                CompleteAction();
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


            if (_targetBuilding.IsDestroyed)
            {
                CompleteAction();
                yield break;
            }

            yield return MoveToPoint(GridService.GetWorldPosition(_targetBuilding.CenterCoords.x, _targetBuilding.CenterCoords.y));

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
                BackpackTransfer.Instance.TransferAllResources(_unit.Backpack, _targetBuilding.Backpack);
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
}
