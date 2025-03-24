using System;
using System.Collections;
using GameUtilities.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveUnitAction : BaseUnitAction
{
    private Vector2 _targetPosition;
    private float _moveSpeed;
    private bool _toRecalculatePath = false;
    private bool _moveCloseToObject;

    public MoveUnitAction(UnitItem unit, Vector2 targetPosition, bool moveCloseToObject = false) : base(unit)
    {
        _targetPosition = targetPosition;
        _moveSpeed = _unit.Unit.Speed;
        _moveCloseToObject = moveCloseToObject;
    }

    public override bool CanExecute()
    {
        //ToDo: Add cases when unit can`t move
        return true;
    }

    public override void Execute()
    {
        if (!CanExecute())
        {
            Debug.LogWarning($"Unit {_unit.Unit.Name} can't move to position {_targetPosition}");
            return;
        }

        _idAction = Guid.NewGuid();

        PathFinderManager.OnPathFound += OnPathFound;

        var action = PathFinderManager.PathAction.Move;
        if (_moveCloseToObject)
        {
            action = PathFinderManager.PathAction.MoveClose;
        }

        if (_unit.IsInGroup)
        {
            var group = GroupManager.Instance.GetGroup(_unit.GroupId);
            group.RemoveUnitFromGroup(_unit);
        }

        _unit.CanGroup = true;

        PathFinderManager.RequestPath(
            _unit.Coords,
            _targetPosition,
            _idAction,
            action
        );
    }

    private void OnPathFound(Guid id, List<Vector2> movePath)
    {
        if (id != _idAction) return;

        PathFinderManager.OnPathFound -= OnPathFound;


        if (movePath != null && movePath.Count > 0)
        {
            if (_moveCloseToObject && movePath.Count > 0)
            {
                _targetPosition = movePath.Last();
            }

            ItemListRegistry.ItemChanged += RefindPathOnItemChanged;
            CoroutineRunner.Instance.StartCoroutineWithId(_idAction, MoveAlongPath(movePath));
        }
        else
        {
            UtilsClass.CreateWorldTextPopup(
                "No path found!",
                UtilsClass.GetMouseWorldPosition(),
                Color.red,
                2f
            );
            CompleteAction();
        }
    }

    private void RefindPathOnItemChanged(Type type, ItemListRegistry.ItemAction action, IItemListObject item)
    {
        _toRecalculatePath = true;
    }

    private IEnumerator MoveAlongPath(List<Vector2> path)
    {
        var currentPathIndex = 0;
        var pathCopy = new List<Vector2>(path.Count);
        pathCopy.AddRange(path.Select(point => new Vector2(point.x, point.y)));

        while (currentPathIndex < pathCopy.Count)
        {
            while (IsPaused)
            {
                yield return null;
            }

            if (IsStopped)
            {
                CompleteAction();
                yield break;
            }

            if (_toRecalculatePath)
            {
                Vector2 currentPosition = _unit.UnitGameObject.transform.position;
                var destination = pathCopy.Last();

                List<Vector2> remainingPath = null;
                if (currentPathIndex < path.Count)
                {
                    remainingPath = path.GetRange(currentPathIndex, path.Count - currentPathIndex);
                }

                path = PathFinder.Instance.RefindPath(remainingPath, currentPosition, destination);

                _toRecalculatePath = false;
                pathCopy = new List<Vector2>(path.Count);
                pathCopy.AddRange(path.Select(point => new Vector2(point.x, point.y)));
                currentPathIndex = 0;

                if (pathCopy.Count <= 1)
                {
                    Debug.Log($"Unit {_unit.Unit.Name} cannot find path to {_targetPosition}");
                    CompleteAction();
                    yield break;
                }
                continue;
            }

            var nextPoint = pathCopy[currentPathIndex];
            var targetPosition = new Vector3(nextPoint.x, nextPoint.y, 0);
            var startPosition = _unit.UnitGameObject.transform.position;
            var journeyLength = Vector3.Distance(startPosition, targetPosition);

            if (journeyLength < 0.1f)
            {
                currentPathIndex++;
                continue;
            }

            var startTime = Time.time;
            var pausedTime = 0f;

            while (Vector3.Distance(_unit.UnitGameObject.transform.position, targetPosition) > 0.01f)
            {
                if (IsPaused)
                {
                    var timeWhenPaused = Time.time;
                    yield return new WaitUntil(() => !IsPaused);
                    pausedTime += (Time.time - timeWhenPaused);
                }

                if (IsStopped)
                {
                    CompleteAction();
                    yield break;
                }

                if (_toRecalculatePath)
                {
                    break;
                }

                var distanceCovered = (Time.time - startTime - pausedTime) * _moveSpeed;
                var fractionOfJourney = Mathf.Clamp01(distanceCovered / journeyLength);

                _unit.SetPosition(Vector3.Lerp(startPosition, targetPosition, fractionOfJourney));

                if (fractionOfJourney >= 0.99f)
                {
                    _unit.SetPosition(targetPosition);
                    break;
                }

                yield return null;
            }

            if (_toRecalculatePath)
            {
                continue;
            }

            currentPathIndex++;
        }
        _isSuccessAction = true;
        CompleteAction();
    }

    protected override void CompleteAction()
    {
        ItemListRegistry.ItemChanged -= RefindPathOnItemChanged;
        _unit.CanGroup = true;
        base.CompleteAction();
    }
}
