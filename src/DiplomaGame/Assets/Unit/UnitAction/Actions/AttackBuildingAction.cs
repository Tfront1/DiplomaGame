using System;
using System.Collections;
using UnityEngine;

public class AttackBuildingAction : BaseUnitAction
{
    private BuildingItem _targetBuilding;

    private bool _movementCompleted = false;
    private bool _movementSuccess = false;

    public AttackBuildingAction(UnitItem unit, BuildingItem targetBuilding) : base(unit)
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
            Debug.Log("Can`t execute moving resources for building. No resources");
            return;
        }

        _idAction = Guid.NewGuid();
        CoroutineRunner.Instance.StartCoroutineWithId(_idAction, Attack());
    }

    private IEnumerator Attack()
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

        }
        else
        {
            CompleteAction();
        }

        _isSuccessAction = true;
        CompleteAction();
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
}
