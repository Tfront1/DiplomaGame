using System;
using System.Collections;
using Assets.Items.Interfaces;
using Items.Resource.BackPack;
using UnityEngine;

namespace UnitAction
{
    public class EquipUnitAction : BaseUnitAction
    {
        private bool _movementCompleted = false;
        private bool _movementSuccess = false;

        private BuildingItem _building;
        private IBackpackItem _itemToEquip;

        public EquipUnitAction(UnitItem unit, BuildingItem building, IBackpackItem item) : base(unit)
        {
            _building = building;
            _itemToEquip = item;
        }

        public override bool CanExecute()
        {
            var isElementExists = false;

            isElementExists = WeaponConfig.WeaponElements.Find(item => item.Equals(_itemToEquip)) != null;

            if (!isElementExists)
                isElementExists = ArmorConfig.ArmorElements.Find(item => item.Equals(_itemToEquip)) != null;

            if (!isElementExists)
                isElementExists = AmmunitionConfig.AmmunitionElements.Find(item => item.Equals(_itemToEquip)) != null;

            var isItemInBackpack = false;
            if (_building.Backpack != null)
            {
                isItemInBackpack = _building.Backpack.GetAllItems().Find(x => x.Item.Equals(_itemToEquip)) != null;
            }

            return isElementExists && isItemInBackpack && !_unit.Backpack.IsFull();
        }

        public override void Execute()
        {
            if (!CanExecute())
            {
                Debug.Log("Can`t execute equipment.");
                return;
            }

            _idAction = Guid.NewGuid();
            CoroutineRunner.Instance.StartCoroutineWithId(_idAction, Equip());
        }

        private IEnumerator Equip()
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

            yield return MoveToPoint(GridService.GetWorldPosition(_building.CenterCoords.x, _building.CenterCoords.y));

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

                BackpackTransfer.Instance.TransferSpecificResource(_building.Backpack, _unit.Backpack,
                    new BackpackItem(_itemToEquip));

                _unit.UnitEquipment.TryAutoEquip(_itemToEquip);
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
}