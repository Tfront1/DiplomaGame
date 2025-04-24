using System;
using System.Collections;
using GameUtilities.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnitAction
{
    public class MoveUnitAction : BaseUnitAction
    {
        private Vector2 _targetPosition;

        private float _moveSpeed;
        private bool _hasLowSpeed = false;

        private bool _toRecalculatePath = false;
        private bool _moveCloseToObject;
        private bool _partialPath = false;

        public MoveUnitAction(UnitItem unit, Vector2 targetPosition, bool moveCloseToObject = false, bool partialPath = false) : base(unit)
        {
            _targetPosition = targetPosition;
            _moveSpeed = _unit.Unit.Speed;
            _moveCloseToObject = moveCloseToObject;
            _partialPath = partialPath;
        }

        public override bool CanExecute()
        {
            if (_unit.Stats.Stamina.CurrentValue < 0.1f)
            {
                return false;
            }
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

            if (_partialPath)
            {
                action = PathFinderManager.PathAction.MoveAnyway;
            }

            _unit.CanGroup = true;

            PathFinderManager.RequestPath(
                _unit.CenterCoords,
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
                _unit.Stats.Stamina.StartGetTired(0.1f);
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
                    _toRecalculatePath = false;

                    var currentPosition = _unit.CenterCoords;
                    var destination = pathCopy.Last();

                    List<Vector2> remainingPath = null;
                    if (currentPathIndex < path.Count)
                    {
                        remainingPath = path.GetRange(currentPathIndex, path.Count - currentPathIndex);
                    }

                    path = PathFinder.Instance.RefindPath(remainingPath, currentPosition, destination, _moveCloseToObject, _partialPath);

                    pathCopy = new List<Vector2>(path.Count);
                    pathCopy.AddRange(path.Select(point => new Vector2(point.x, point.y)));
                    currentPathIndex = 1;

                    if (pathCopy.Count <= 1)
                    {
                        Debug.Log($"[{_unit.Unit.Name}] Cannot find a path to {_targetPosition}");
                        CompleteAction();
                        yield break;
                    }
                    continue;
                }

                var nextPoint = pathCopy[currentPathIndex];
                var targetPosition = new Vector3(nextPoint.x, nextPoint.y, 0);
                var startPosition = _unit.CenterCoords;
                var journeyLength = Vector3.Distance(startPosition, targetPosition);

                if (journeyLength < 0.1f)
                {
                    currentPathIndex++;
                    continue;
                }

                _unit.UnitMoveDirection = ((Vector2)targetPosition - startPosition).normalized;

                _unit.State = UnitState.Move;
                UnitManager.UpdateAnimation(_unit, true, 5f);

                var distanceTraveled = 0f;

                while (true)
                {
                    if (IsPaused)
                    {
                        yield return new WaitUntil(() => !IsPaused);
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

                    if (!_hasLowSpeed && _unit.Stats.Stamina.CurrentValue < 5.0f)
                    {
                        _moveSpeed /= 2;
                        _hasLowSpeed = true;
                    }
                    else if (_unit.Stats.Stamina.CurrentValue < 0.1f)
                    {
                        CompleteAction();
                        yield break;
                    }

                    distanceTraveled += _moveSpeed * Time.deltaTime;
                    var fractionOfJourney = Mathf.Clamp01(distanceTraveled / journeyLength);

                    _unit.SetPosition(Vector3.Lerp(startPosition, targetPosition, fractionOfJourney));

                    if (fractionOfJourney >= 0.99f)
                    {
                        _unit.SetPosition(targetPosition);
                        break;
                    }

                    yield return null;
                }

                if (!_toRecalculatePath)
                {
                    currentPathIndex++;
                }
            }
            _isSuccessAction = true;
            CompleteAction();
        }

        protected override void CompleteAction()
        {
            _unit.UnitMoveDirection = new Vector2();

            _unit.State = UnitState.Idle;
            UnitManager.UpdateAnimation(_unit);

            ItemListRegistry.ItemChanged -= RefindPathOnItemChanged;
            _unit.CanGroup = true;

            _unit.Stats.Stamina.StopGetTired();

            base.CompleteAction();
        }
    }
}
