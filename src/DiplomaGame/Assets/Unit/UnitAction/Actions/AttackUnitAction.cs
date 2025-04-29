using System;
using System.Collections;
using Assets.Items.Weapon;
using UnityEngine;

namespace UnitAction
{
    public class AttackUnitAction : BaseUnitAction
    {
        private UnitItem _targetUnit;

        private bool _movementCompleted = false;
        private bool _movementSuccess = false;
        private WeaponElement weapon;
        private bool _attackWithMainWeapon = false;
        private bool _isMeleeWeapon = false;

        public AttackUnitAction(UnitItem unit, UnitItem targetUnit, bool attackWithMainWeapon = true) : base(unit)
        {
            _targetUnit = targetUnit;

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
            if (_unit.Stats.Stamina.CurrentValue < 10f)
            {
                return false;
            }
            return true;
        }

        public override void Execute()
        {
            if (!CanExecute())
            {
                Debug.Log("Can`t execute attack unit action.");
                CompleteAction();
                return;
            }

            _targetUnit.OnDied += OnUnitDied;

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

            yield return MoveToPoint(true);

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
                var attackDistance = MapConfig.CellSize * weapon.AttackDistance * 0.9f;

                if (_isMeleeWeapon)
                {
                    _unit.Skills.SetActiveSkill<SwordsmanshipSkill>();
                }
                else
                {
                    _unit.Skills.SetActiveSkill<ArcherySkill>();
                }

                while (!_targetUnit.IsDestroyed)
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

                        if (_isMeleeWeapon)
                        {
                            _unit.Skills.SetActiveSkill<SwordsmanshipSkill>();
                        }
                        else
                        {
                            _unit.Skills.SetActiveSkill<ArcherySkill>();
                        }
                    }

                    if (!IsInAttackRange(_unit.CenterCoords, _targetUnit.CenterCoords, attackDistance))
                    {
                        _unit.Skills.ResetActiveSkill();
                        yield return MoveToPoint(true);
                        if (!_movementSuccess || IsStopped)
                        {
                            CompleteAction();
                            yield break;
                        }

                        if (_isMeleeWeapon)
                        {
                            _unit.Skills.SetActiveSkill<SwordsmanshipSkill>();
                        }
                        else
                        {
                            _unit.Skills.SetActiveSkill<ArcherySkill>();
                        }
                    }

                    if (_unit.Stats.Stamina.CurrentValue < 10f)
                    {
                        CompleteAction();
                        yield break;
                    }

                    if (Time.time >= lastAttackTime + attackCooldown)
                    {
                        _unit.AttackUnit(_targetUnit, _attackWithMainWeapon);

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
            _targetUnit.OnDied -= OnUnitDied;
            base.CompleteAction();
        }

        private IEnumerator MoveToPoint(bool considerWeaponRange = false)
        {
            if (considerWeaponRange && _unit != null && _targetUnit != null)
            {
                var weapon = _attackWithMainWeapon
                    ? _unit.UnitEquipment.MainWeapon
                    : _unit.UnitEquipment.SecondaryWeapon;

                if (weapon != null)
                {
                    _movementCompleted = false;
                    _movementSuccess = false;

                    var followAction = new FollowEnemyAction(_unit, _targetUnit, _attackWithMainWeapon);
                    followAction.OnActionCompleted += OnMovementComplete;
                    followAction.Execute();

                    var wasPaused = false;

                    while (!_movementCompleted)
                    {
                        if (IsStopped)
                        {
                            followAction.Cancel();
                            followAction.OnActionCompleted -= OnMovementComplete;
                            yield break;
                        }

                        if (IsPaused)
                        {
                            if (!wasPaused)
                            {
                                followAction.Pause();
                                wasPaused = true;
                            }
                            yield return null;
                        }
                        else
                        {
                            if (wasPaused)
                            {
                                followAction.Resume();
                                wasPaused = false;
                            }
                        }

                        yield return null;
                    }

                    followAction.OnActionCompleted -= OnMovementComplete;
                }
                else
                {
                    _movementCompleted = false;
                    _movementSuccess = false;
                }
            }
            else
            {
                _movementCompleted = false;
                _movementSuccess = false;
            }
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

        private void OnUnitDied(object sender, UnitItem.UnitDiedEventArgs args)
        {
            Cancel();
        }
    }
}
