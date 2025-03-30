using System;
using System.Collections;
using Assets.Items.Weapon;
using UnityEngine;

public class AttackBuildingAction : BaseUnitAction
{
    private BuildingItem _targetBuilding;

    private bool _movementCompleted = false;
    private bool _movementSuccess = false;
    private WeaponElement weapon;
    private bool _attackWithMainWeapon = false;
    private bool _isMeleeWeapon = false;
    
    public AttackBuildingAction(UnitItem unit, BuildingItem targetBuilding, bool attackWithMainWeapon = true) : base(unit)
    {
        _targetBuilding = targetBuilding;

        if (attackWithMainWeapon)
        {
            weapon = unit.UnitEquipment.MainWeapon;
        }
        else
        {
            weapon = unit.UnitEquipment.SecondaryWeapon;
        }

        _attackWithMainWeapon = attackWithMainWeapon;
        _isMeleeWeapon = attackWithMainWeapon
            ? unit.UnitEquipment.MainWeapon.Ammunition == null
            : unit.UnitEquipment.SecondaryWeapon.Ammunition == null;
    }

    public override bool CanExecute()
    {
        return true;
    }

    public override void Execute()
    {
        if (!CanExecute())
        {
            Debug.Log("Can`t execute attack building action.");
            return;
        }

        _targetBuilding.OnDestroyed += OnBuildingDestroyed;

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

        yield return MoveToPoint(GridService.GetWorldPosition(_targetBuilding.X, _targetBuilding.Y), true);

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
            var attackCooldown = weapon.CoolDown;
            var lastAttackTime = 0f;

            if (_isMeleeWeapon)
            {
                _unit.Skills.SetActiveSkill<SwordsmanshipSkill>();
            }
            else
            {
                _unit.Skills.SetActiveSkill<ArcherySkill>();
            }

            while (!_targetBuilding.IsDestroyed)
            {
                if (IsStopped)
                {
                    _unit.Skills.ResetActiveSkill();
                    CompleteAction();
                    yield break;
                }

                while (IsPaused)
                {
                    yield return null;
                }

                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    _unit.AttackBuilding(_targetBuilding, _attackWithMainWeapon);

                    lastAttackTime = Time.time;
                }

                yield return null;
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

    protected override void CompleteAction()
    {
        _targetBuilding.OnDestroyed -= OnBuildingDestroyed;
        base.CompleteAction();
    }

    private IEnumerator MoveToPoint(Vector2 targetPosition, bool considerWeaponRange = false)
    {
        targetPosition.x += MapConfig.CellSize / 2;
        targetPosition.y += MapConfig.CellSize / 2;

        if (considerWeaponRange && _unit != null && _targetBuilding != null)
        {
            var weapon = _attackWithMainWeapon
                ? _unit.UnitEquipment.MainWeapon
                : _unit.UnitEquipment.SecondaryWeapon;

            if (weapon != null)
            {
                var unitPosition = new Vector2(_unit.transform.position.x, _unit.transform.position.y);

                var directionToTarget = (targetPosition - unitPosition).normalized;

                var safeDistance = MapConfig.CellSize * weapon.AttackDistance * 0.9f;

                safeDistance = Mathf.Max(safeDistance, 0f);

                targetPosition -= directionToTarget * safeDistance;

                var currentDistance = Vector2.Distance(unitPosition, targetPosition);
                if (currentDistance <= safeDistance)
                {
                    _movementCompleted = true;
                    _movementSuccess = true;
                    yield break;
                }
            }
        }

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
