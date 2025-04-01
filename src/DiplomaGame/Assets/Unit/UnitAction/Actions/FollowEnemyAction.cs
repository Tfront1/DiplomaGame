using Assets.Items.Weapon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnitItem;

public class FollowEnemyAction : BaseUnitAction
{
    private UnitItem _targetUnit;
    private bool _movementCompleted = false;
    public bool _movementSuccess = false;
    private WeaponElement _weapon;
    private Guid _pathGuid;
    private List<Vector2> _path = null;
    private bool _pathFound = false;

    private float _attackDistance;

    private bool _hasValidPath = false;
    private MoveUnitAction _currentMoveAction = null;
    private bool _enemyMovedDuringAction = true;
    public FollowEnemyAction(UnitItem unit, UnitItem targetUnit, bool attackWithMainWeapon = true) : base(unit)
    {
        _targetUnit = targetUnit;

        _weapon = attackWithMainWeapon
            ? unit.UnitEquipment.MainWeapon
            : unit.UnitEquipment.SecondaryWeapon;

        if (_weapon != null)
        {
            _attackDistance = MapConfig.CellSize * _weapon.AttackDistance * 0.9f;
        }
    }

    public override bool CanExecute()
    {
        return _unit != null && _targetUnit != null && !_targetUnit.IsDestroyed && _weapon != null;
    }

    public override void Execute()
    {
        if (!CanExecute())
        {
            Debug.Log("Can't execute follow enemy action.");
            CompleteAction();
            return;
        }

        _targetUnit.OnPositionChanged += OnEnemyPositionChanged;
        _targetUnit.OnDied += OnEnemyDead;

        _idAction = Guid.NewGuid();
        CoroutineRunner.Instance.StartCoroutineWithId(_idAction, FollowEnemy());
    }

    private IEnumerator FollowEnemy()
    {
        while (!IsStopped && _targetUnit != null && !_targetUnit.IsDestroyed)
        {
            if (IsPaused)
            {
                yield return null;
                continue;
            }

            var needRecalculate = ShouldRecalculatePath();

            if (needRecalculate)
            {
                _enemyMovedDuringAction = false;

                if (_currentMoveAction != null)
                {
                    _currentMoveAction.Cancel();
                    _currentMoveAction.OnActionCompleted -= OnMovementComplete;
                    _currentMoveAction = null;
                }

                yield return CalculatePath();

                if (_path != null && _path.Count > 0)
                {
                    _hasValidPath = true;
                    yield return MoveAlongPath();
                }
                else
                {
                    _hasValidPath = false;
                }
            }

            if (_movementSuccess)
            {
                break;
            }

            yield return null;
        }

        _isSuccessAction = true;
        CompleteAction();
    }

    private bool ShouldRecalculatePath()
    {
        return _enemyMovedDuringAction;
    }

    private IEnumerator CalculatePath()
    {
        _pathFound = false;

        Vector2 startPosition;
        Vector2 targetPosition = _targetUnit.Coords;

        if (_hasValidPath && _path != null && _path.Count >= 2)
        {
            startPosition = _path[_path.Count - 2];

            if (Vector2.Distance(_unit.Coords, startPosition) > Vector2.Distance(_unit.Coords, targetPosition))
            {
                startPosition = _unit.Coords;
            }
        }
        else
        {
            startPosition = _unit.Coords;
        }

        _pathGuid = Guid.NewGuid();
        PathFinderManager.OnPathFound += OnPathFound;
        PathFinderManager.RequestPath(startPosition, targetPosition, _pathGuid, PathFinderManager.PathAction.MoveAnyway);

        while (!_pathFound)
        {
            yield return null;
        }

        if (!_pathFound)
        {
            PathFinderManager.OnPathFound -= OnPathFound;
            Debug.LogWarning("Path finding timeout!");
            yield break;
        }

        if (startPosition != (Vector2)_unit.Coords && _path != null && _path.Count > 0)
        {
            var combinedPath = new List<Vector2>();

            var segmentEndIndex = -1;
            for (var i = 0; i < _path.Count; i++)
            {
                if (Vector2.Distance(_path[i], startPosition) < 0.1f)
                {
                    segmentEndIndex = i;
                    break;
                }
            }

            if (segmentEndIndex >= 0)
            {
                for (var i = 0; i <= segmentEndIndex; i++)
                {
                    combinedPath.Add(_path[i]);
                }

                for (var i = 1; i < _path.Count; i++)
                {
                    combinedPath.Add(_path[i]);
                }

                _path = combinedPath;
            }
        }
    }

    private IEnumerator MoveAlongPath()
    {
        if (_path == null || _path.Count == 0) yield break;

        var targetPosition = GetOptimalTargetPoint();

        _movementCompleted = false;
        _movementSuccess = false;

        _currentMoveAction = new MoveUnitAction(_unit, targetPosition, false, true);
        _currentMoveAction.OnActionCompleted += OnMovementComplete;
        _currentMoveAction.Execute();

        var wasPaused = false;

        while (!_movementCompleted)
        {
            if (IsStopped)
            {
                _currentMoveAction.Cancel();
                _currentMoveAction.OnActionCompleted -= OnMovementComplete;
                yield break;
            }

            if (IsPaused)
            {
                if (!wasPaused)
                {
                    _currentMoveAction.Pause();
                    wasPaused = true;
                }
                yield return null;
            }
            else
            {
                if (wasPaused)
                {
                    _currentMoveAction.Resume();
                    wasPaused = false;
                }
            }

            if (IsInAttackRange(_unit.CenterCoords, _targetUnit.CenterCoords, _attackDistance))
            {
                _currentMoveAction.Cancel();
                _movementSuccess = true;
                _currentMoveAction.OnActionCompleted -= OnMovementComplete;
                yield break;
            }

            var needRecalculate = ShouldRecalculatePath();

            if (needRecalculate)
            {
                _enemyMovedDuringAction = false;

                if (_currentMoveAction != null)
                {
                    _currentMoveAction.Cancel();
                    _currentMoveAction.OnActionCompleted -= OnMovementComplete;
                    _currentMoveAction = null;
                }

                yield return CalculatePath();

                if (_path != null && _path.Count > 0)
                {
                    _hasValidPath = true;
                    targetPosition = GetOptimalTargetPoint();

                    _movementCompleted = false;
                    _movementSuccess = false;

                    _currentMoveAction = new MoveUnitAction(_unit, targetPosition, false, true);
                    _currentMoveAction.OnActionCompleted += OnMovementComplete;
                    _currentMoveAction.Execute();
                }
                else
                {
                    _hasValidPath = false;
                    yield break;
                }
            }

            yield return null;
        }

        if (_currentMoveAction != null)
        {
            _currentMoveAction.OnActionCompleted -= OnMovementComplete;
            _currentMoveAction = null;
        }
    }

    private Vector2 GetOptimalTargetPoint()
    {
        if (_path.Count <= 2)
            return _path.Last();

        for (var i = _path.Count - 1; i >= 0; i--)
        {
            if (IsInAttackRange(_path[i], _targetUnit.CenterCoords, _attackDistance))
            {
                return _path[i];
            }
        }

        return _path.Last();
    }

    private bool IsInAttackRange(Vector2 unitPosition, Vector2 targetUnitPosition, float attackRange)
    {
        var unitSize = _targetUnit.SpriteRenderer.sprite.rect.size / _targetUnit.SpriteRenderer.sprite.pixelsPerUnit;

        var closestX = Math.Max(targetUnitPosition.x, Math.Min(unitPosition.x, targetUnitPosition.x + unitSize.x));
        var closestY = Math.Max(targetUnitPosition.y, Math.Min(unitPosition.y, targetUnitPosition.y + unitSize.y));

        var closestPoint = new Vector2(closestX, closestY);

        var distance = Vector2.Distance(unitPosition, closestPoint);

        return distance <= attackRange;
    }

    private void OnMovementComplete(IUnitAction action)
    {
        if (action is BaseUnitAction baseAction)
        {
            _movementSuccess = baseAction.IsSuccess;
        }
        _movementCompleted = true;
    }

    private void OnPathFound(Guid id, List<Vector2> path)
    {
        if (id != _pathGuid) return;

        PathFinderManager.OnPathFound -= OnPathFound;
        _pathFound = true;
        _path = path;
    }

    private void OnEnemyPositionChanged(object sender, UnitPositionChangedArgs args)
    {
        _enemyMovedDuringAction = true;
    }

    private void OnEnemyDead(object sender, UnitDiedEventArgs args)
    {
        Cancel();
    }

    protected override void CompleteAction()
    {
        if (_targetUnit != null)
        {
            _targetUnit.OnPositionChanged -= OnEnemyPositionChanged;
            _targetUnit.OnDied -= OnEnemyDead;
        }

        base.CompleteAction();
    }

    public override void Cancel()
    {
        PathFinderManager.OnPathFound -= OnPathFound;
        base.Cancel();
    }
}