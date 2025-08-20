using System.Collections.Generic;
using System.Linq;
using GameUtilities.Utils;
using Selection;
using Selection.Interfaces;
using Town;
using UnitAction;
using Unity.VisualScripting;
using UnityEngine;

namespace StateMachine.States
{
    public class UnitSelectedState : BaseState
    {
        private UnitItem _unit;
        private List<UnitItem> _units = new();

        public UnitSelectedState(InputStateMachine stateMachine): base(stateMachine)
        {

        }

        public override void Enter()
        {
            Debug.Log("Enter UnitSelected state");
            if (_stateMachine.SelectedItems.Count == 1)
            {
                _unit = _stateMachine.SelectedItems.First() as UnitItem;
                if (_unit != null)
                {
                    _unit.UIToChange += UnitUIManager.Instance.UpdateUnitInfo;
                    _unit.OnDied += OnUnitDied;
                    UnitUIManager.Instance.ShowUnitInfo(_unit);

                    UnitListUIManager.Instance.HideUnitList();
                }
            }
            else
            {
                _units = SelectorFactory.GetUnitItems(_stateMachine.SelectedItems);

                var removedUnits = _units.Where(x => x.HomeTown.Id != TownRegistry.UserTown.Id).ToList();
                removedUnits.ForEach(x => _stateMachine.DeselectItem(x));

                _units.RemoveAll(x => x.HomeTown.Id != TownRegistry.UserTown.Id); _stateMachine.SelectedItems.Clear();
                _units.ForEach(x => _stateMachine.SelectedItems.Add(x));

                UnitListUIManager.Instance.UpdateUnitList(_units);
                UnitListUIManager.Instance.ShowUnitList();

                UnitUIManager.Instance.HideUnitInfo();

                foreach (var unit in _units)
                {
                    unit.OnDied += OnUnitDied;
                    unit.UIToChange += UnitListUIManager.Instance.UpdateUnitInfo;
                }
            }
        }

        public override void Exit()
        {
            if (_unit != null && !_unit.IsDestroyed())
            {
                _unit.UIToChange -= UnitUIManager.Instance.UpdateUnitInfo;
                _unit.OnDied -= OnUnitDied;
            }

            foreach (var unit in _units)
            {
                if (unit != null && !unit.IsDestroyed())
                {
                    unit.OnDied -= OnUnitDied;
                }
            }

            _units.Clear();
            _unit = null;

            Debug.Log("Exiting UnitSelected state");
            UnitUIManager.Instance.HideUnitInfo();
            UnitListUIManager.Instance.HideUnitList();
        }

        public override void HandleRightClick(Vector2 position)
        {
            var selectedUnits = _stateMachine.SelectedItems.Count > 0
                ? SelectorFactory.GetUnitItems(_stateMachine.SelectedItems)
                : new List<UnitItem>();
            var isAllUnits = selectedUnits.Count == _stateMachine.SelectedItems.Count;
            var singleUnit = _stateMachine.SelectedItems.Count == 1
                ? SelectorFactory.GetUnitItem(_stateMachine.SelectedItems.First())
                : null;

            if (selectedUnits.Count == 0 || (singleUnit != null && singleUnit.HomeTown.Id != TownRegistry.UserTown.Id))
            {
                return;
            }

            if (singleUnit != null)
            {
                if (singleUnit.HomeTown.Id != TownRegistry.UserTown.Id)
                {
                    return;
                }

                if (singleUnit.IsInGroup)
                {
                    var group = GroupManager.Instance.GetGroup(singleUnit.GroupId);
                    if (group != null)
                    {
                        group.RemoveUnitFromGroup(singleUnit);
                    }
                }
            }

            foreach (var unit in selectedUnits)
            {
                if (unit.IsInGroup)
                {
                    var group = GroupManager.Instance.GetGroup(unit.GroupId);
                    if (group != null)
                    {
                        var unitsInGroup = group.GroupUnits;

                        var allGroupUnitsSelected = unitsInGroup.All(groupUnit =>
                            selectedUnits.Any(selectedUnit => selectedUnit.Id == groupUnit.Id));

                        if (!allGroupUnitsSelected)
                        {
                            group.RemoveUnitFromGroup(unit);
                        }
                    }
                }
            }

            if (selectedUnits.Count > 1)
            {
                singleUnit = selectedUnits.First();
            }

            var target = SelectorManager.Instance.GetSelectedItem(position);

            switch (target.Item2)
            {
                case SelectedStates.NothingSelect:

                    if (_stateMachine.SelectedItems.Count == 1)
                    {
                        
                        var moveAction = new MoveUnitAction(singleUnit, position);
                        UnitActionManager.Instance.ExecuteImmediately(moveAction);
                        
                    }
                    else
                    {
                        if (isAllUnits)
                        {
                            foreach (var unit in selectedUnits)
                            {
                                var moveGroupAction = new MoveGroupUnitAction(unit, position);
                                UnitActionManager.Instance.ExecuteImmediately(moveGroupAction);
                            }
                        }
                    }

                    Debug.Log("Units moving to position");
                    break;

                case SelectedStates.UnitSelect:

                    var selectedUnit = singleUnit;
                    var otherUnit = SelectorFactory.GetUnitItem(target.Item1.First());

                    if (selectedUnit.HomeTown.Id == otherUnit.HomeTown.Id)
                    {
                        if (_stateMachine.SelectedItems.Count == 1)
                        {
                            var moveAction = new MoveUnitAction(singleUnit, position);
                            UnitActionManager.Instance.ExecuteImmediately(moveAction);
                        }
                        else
                        {
                            if (isAllUnits)
                            {
                                foreach (var unit in selectedUnits)
                                {
                                    var moveGroupAction = new MoveGroupUnitAction(unit, position);
                                    UnitActionManager.Instance.ExecuteImmediately(moveGroupAction);
                                }
                            }
                        }
                    }
                    else
                    {
                        var targetUnits = SelectorFactory.GetUnitItems(target.Item1).ToList();
                        var randomSelectedUnits = selectedUnits.OrderBy(_ => Random.value).ToList();

                        foreach (var unit in randomSelectedUnits)
                        {
                            if (targetUnits.Count == 0) break;

                            var randomTarget = targetUnits[Random.Range(0, targetUnits.Count)];
                            var attackUnitAction = new AttackUnitAction(unit, randomTarget, true);
                            UnitActionManager.Instance.ExecuteImmediately(attackUnitAction);
                        }
                    }

                    Debug.Log("Units interacting with other unit");
                    break;

                case SelectedStates.BuildingSelect:

                    var building = SelectorFactory.GetBuildingItem(target.Item1.First());
                    building.BuildingController.OpenActionPanel(selectedUnits);
                        
                    Debug.Log("Units interacting with building");
                    break;

                case SelectedStates.SupplySelect:

                    var supplyToFarm = SelectorFactory.GetSupplyItem(target.Item1.First());

                    if (isAllUnits)
                    {
                        foreach (var unit in selectedUnits)
                        {
                            var collectResourcesAction = new CollectSupplyAction(unit, supplyToFarm);
                            UnitActionManager.Instance.ExecuteImmediately(collectResourcesAction);
                        }
                    }

                    Debug.Log("Units collecting supplies");
                    break;
            }
        }

        private void OnUnitDied(object sender, UnitItem.UnitDiedEventArgs args)
        {
            if (_unit == null && (_units == null || _units.Count == 0))
            {
                Exit();
                return;
            }

            var unit = args.Unit;
            _stateMachine.SelectedItems.Remove(unit);
            _units.Remove(unit);

            unit.OnDied -= OnUnitDied;
            unit.UIToChange -= UnitUIManager.Instance.UpdateUnitInfo;

            if (_stateMachine.SelectedItems.Count == 0)
            {
                _unit = null;
                Exit();
            }
            else if (_stateMachine.SelectedItems.Count == 1)
            {
                UnitListUIManager.Instance.HideUnitList();

                _unit = _stateMachine.SelectedItems.First() as UnitItem;
                if (_unit != null)
                {
                    Exit();
                    _stateMachine.ChangeState(SelectedStates.UnitSelect);
                }
            }
            else
            {
                UnitListUIManager.Instance.RemoveDeadUnit(unit);
            }
        }

        public override void HandleLeftClick(Vector2 position)
        {
            if (!_stateMachine.IsShiftHold)
            {
                base.HandleLeftClick(position);
            }
            else
            {
                var items = _stateMachine.SelectedItems;
                var selectedItems = SelectorManager.Instance.GetSelectedItem(position);

                if (selectedItems.Item2 != SelectedStates.UnitSelect)
                {
                    base.HandleLeftClick(position);
                }
                else
                {
                    var itemsToAdd = new HashSet<ISelectable>();

                    foreach (var item in selectedItems.Item1)
                    {
                        if (items.Contains(item))
                        {
                            _stateMachine.DeselectItem(item);
                        }
                        else
                        {
                            itemsToAdd.Add(item);
                        }
                    }

                    _stateMachine.HighlightSelectedItems(itemsToAdd);
                    _stateMachine.SelectedItems.AddRange(itemsToAdd);
                    _stateMachine.ChangeState(SelectedStates.UnitSelect);
                }
            }
        }

        public override void HandleLeftHoldEnd(Vector2 position)
        {
            if (!_stateMachine.IsShiftHold)
            {
                base.HandleLeftHoldEnd(position);
            }
            else
            {
                if (_stateMachine.SelectionAreaVisual != null)
                {
                    _stateMachine.SelectionAreaVisual.SetActive(false);
                }

                if (UtilsClass.CalculateDistance(position, _stateMachine.StartSelectPosition) > 1)
                {
                    var items = _stateMachine.SelectedItems;
                    var selectedItems = SelectorManager.Instance.GetSelectedArea(_stateMachine.StartSelectPosition,
                        position, _stateMachine.IsSelectingUnitsOnly);

                    var itemsToAdd = new HashSet<ISelectable>();

                    foreach (var item in selectedItems.Item1)
                    {
                        if (items.Contains(item))
                        {
                            _stateMachine.DeselectItem(item);
                        }
                        else
                        {
                            itemsToAdd.Add(item);
                        }
                    }

                    _stateMachine.HighlightSelectedItems(itemsToAdd);
                    _stateMachine.SelectedItems.AddRange(itemsToAdd);
                    _stateMachine.ChangeState(SelectedStates.UnitSelect);
                }
            }
        }
    }
}
