using System;
using UnityEngine;
using System.Collections;
using System.Linq;
using Items.Resource.BackPack;

namespace UnitAction
{
    public class CollectSupplyAction : BaseUnitAction
    {
        private SupplyItem _supply;
        private bool _movementCompleted = false;
        private bool _movementSuccess = false;
        private bool _broughtResourcesCompleted = false;
        private bool _broughtResourcesSuccess = false;

        public CollectSupplyAction(UnitItem unit, SupplyItem supply) : base(unit)
        {
            _supply = supply;
        }

        public override bool CanExecute()
        {
            if (_unit.Backpack.IsFull() || _supply.Backpack.IsEmpty())
            {
                return false;
            }

            return true;
        }

        public override void Execute()
        {
            if (!CanExecute())
            {
                Debug.Log("Can`t execute collect resources. Unit backpack is full or supply don`t have any resource to collect");
                CompleteAction();
                return;
            }

            _supply.OnDestroyed += OnSupplyDestroyed;

            _idAction = Guid.NewGuid();
            CoroutineRunner.Instance.StartCoroutineWithId(_idAction, CollectSupply());
        }

        private IEnumerator CollectSupply()
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

            yield return MoveToPoint(GridService.GetWorldPosition(_supply.X, _supply.Y));

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
                var resourcesToCollect = _supply.Backpack.GetDetailedItems();
                var unitFarmSkill = _unit.Skills.GetSkill<FarmingSkill>();
                var resourcesCountToCollectPerTick = 20 * unitFarmSkill.FarmBonus;

                var resource = new BackpackItem(resourcesToCollect.First().Key, (int)resourcesCountToCollectPerTick);

                _unit.Skills.SetActiveSkill(typeof(FarmingSkill));

                _unit.Stats.Stamina.StartGetTired(0.1f);

                while (!_unit.Backpack.IsFull() && !_supply.Backpack.IsEmpty())
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

                        _unit.Skills.SetActiveSkill(typeof(FarmingSkill));
                    }

                    if (_unit.Stats.Stamina.CurrentValue < 10f)
                    {
                        CompleteAction();
                        yield break;
                    }

                    _supply.CollectResources(_unit, resource);

                    yield return new WaitForSeconds(1.0f);
                }

                _unit.Skills.ResetActiveSkill();
                _supply.OnDestroyed -= OnSupplyDestroyed;

                yield return BringBackResources(_unit);

                if (IsStopped)
                {
                    CompleteAction();
                    yield break;
                }

                while (IsPaused)
                {
                    yield return null;
                }

                if (!_broughtResourcesSuccess)
                {
                    CompleteAction();
                }
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

        protected override void CompleteAction()
        {
            _unit.Stats.Stamina.StopGetTired();
            _supply.OnDestroyed -= OnSupplyDestroyed;
            base.CompleteAction();
        }

        private void OnResourcesBroughtBack(IUnitAction action)
        {
            if (action is BaseUnitAction baseAction)
            {
                _broughtResourcesSuccess = baseAction.IsSuccess;
            }
            _broughtResourcesCompleted = true;
        }

        private IEnumerator BringBackResources(UnitItem unit)
        {
            if (IsStopped)
            {
                yield break;
            }

            var bringAction = new BringBackResourcesAction(unit);
            bringAction.OnActionCompleted += OnResourcesBroughtBack;
            bringAction.Execute();

            var wasPaused = false;

            while (!_broughtResourcesCompleted)
            {
                if (IsStopped)
                {
                    bringAction.Cancel();
                    bringAction.OnActionCompleted -= OnResourcesBroughtBack;
                    yield break;
                }

                if (IsPaused)
                {
                    if (!wasPaused)
                    {
                        bringAction.Pause();
                        wasPaused = true;
                    }
                    yield return null;
                }
                else
                {
                    if (wasPaused)
                    {
                        bringAction.Resume();
                        wasPaused = false;
                    }
                    yield return null;
                }
            }

            bringAction.OnActionCompleted -= OnResourcesBroughtBack;
        }

        private void OnSupplyDestroyed(object sender, SupplyItem.SupplyDestroyedEventArgs args)
        {
            Cancel();
        }
    }
}
