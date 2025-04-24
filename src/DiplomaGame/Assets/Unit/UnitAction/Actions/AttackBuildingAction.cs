using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Items.Weapon;
using UnityEngine;

namespace UnitAction
{
    public class AttackBuildingAction : BaseUnitAction
    {
        private BuildingItem _targetBuilding;

        private bool _movementCompleted = false;
        private bool _movementSuccess = false;
        private WeaponElement weapon;
        private bool _attackWithMainWeapon = false;
        private bool _isMeleeWeapon = false;
        private Guid _pathGuid;
        private List<Vector2> _path = null;
        private bool _pathFound = false;

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

            yield return MoveToPoint(GridService.GetWorldPosition(_targetBuilding.CenterCoords.x, _targetBuilding.CenterCoords.y), true);

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

                    if (_unit.Stats.Stamina.CurrentValue < 10f)
                    {
                        CompleteAction();
                        yield break;
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
            if (considerWeaponRange && _unit != null && _targetBuilding != null)
            {
                var weapon = _attackWithMainWeapon
                    ? _unit.UnitEquipment.MainWeapon
                    : _unit.UnitEquipment.SecondaryWeapon;

                if (weapon != null)
                {
                    _pathGuid = Guid.NewGuid();
                    PathFinderManager.OnPathFound += OnPathFound;

                    PathFinderManager.RequestPath(_unit.Coords, targetPosition, _pathGuid, PathFinderManager.PathAction.MoveAnyway);

                    while (!_pathFound)
                    {
                        yield return null;
                    }

                    var attackRange = MapConfig.CellSize * weapon.AttackDistance * 0.9f;

                    var buildingWidth = _targetBuilding.Building.WidthCell * MapConfig.CellSize;
                    var buildingHeight = _targetBuilding.Building.HeightCell * MapConfig.CellSize;

                    if (_path == null || !IsInAttackRange(_path.Last(), targetPosition, buildingHeight, buildingWidth, attackRange))
                    {
                        Cancel();
                        yield break;
                    }

                    var moveAction = new MoveUnitAction(_unit, targetPosition, false, true);
                    moveAction.OnActionCompleted += OnMovementComplete;
                    moveAction.Execute();

                    var wasPaused = false;

                    var timerToCalculateDistance = 1f;
                    var time = Time.time;

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
                        }

                        if (Time.time - time > timerToCalculateDistance)
                        {
                            time = Time.time;
                            if (IsInAttackRange(_unit.Coords, targetPosition, buildingHeight, buildingWidth, attackRange))
                            {
                                moveAction.Cancel();
                                _movementSuccess = true;
                                moveAction.OnActionCompleted -= OnMovementComplete;
                                yield break;
                            }
                        }

                        yield return null;
                    }

                    moveAction.OnActionCompleted -= OnMovementComplete;
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

        private bool IsInAttackRange(Vector2 unitPosition, Vector2 buildingCenter, float buildingHeight, float buildingWidth, float attackRange)
        {
            var halfWidth = buildingWidth / 2;
            var halfHeight = buildingHeight / 2;

            var closestX = Math.Max(buildingCenter.x - halfWidth, Math.Min(unitPosition.x, buildingCenter.x + halfWidth));
            var closestY = Math.Max(buildingCenter.y - halfHeight, Math.Min(unitPosition.y, buildingCenter.y + halfHeight));

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

        private void OnBuildingDestroyed(object sender, BuildingItem.BuildingDestroyedEventArgs args)
        {
            Cancel();
        }
    }
}
