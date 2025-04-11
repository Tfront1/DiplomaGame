using System;
using System.Collections;
using UnityEngine;

namespace UnitAction
{
    public class CraftAction : BaseUnitAction
    {
        private BuildingCraftingSystem _crafting;
        public bool InterruptedByCraft { get; set; } = false;

        private float _fixedDeltaTime = 0.5f;
        private bool _movementCompleted = false;
        private bool _movementSuccess = false;

        public CraftAction(UnitItem unit, BuildingCraftingSystem crafting) : base(unit)
        {
            _crafting = crafting;
        }

        public override bool CanExecute()
        {
            return true;
        }

        public override void Execute()
        {
            if (!CanExecute())
            {
                Debug.Log("Can`t execute crafting.");
                return;
            }

            _idAction = Guid.NewGuid();
            CoroutineRunner.Instance.StartCoroutineWithId(_idAction, Craft());
        }

        private IEnumerator Craft()
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

            var smithingSkill = _unit.Skills.GetSkill<SmithingSkill>();
            if (smithingSkill != null)
            {
                _fixedDeltaTime *= smithingSkill.CraftSpeedBonus;
            }

            yield return MoveToPoint(GridService.GetWorldPosition(_crafting.Building.X, _crafting.Building.Y));

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
                _unit.Skills.SetActiveSkill(typeof(SmithingSkill));

                while (_crafting.IsCrafting)
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

                        _unit.Skills.SetActiveSkill(typeof(SmithingSkill));
                    }

                    _crafting.UpdateCraftingProgress(_fixedDeltaTime);

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
            if (!InterruptedByCraft)
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
            if (!InterruptedByCraft && _crafting.HasAssignedUnit(_unit))
            {
                _crafting.UnassignUnitFromCraft(_unit);
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
    }

}
