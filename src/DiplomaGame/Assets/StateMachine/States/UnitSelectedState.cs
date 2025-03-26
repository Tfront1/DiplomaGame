using System.Linq;
using Selection;
using UnityEngine;

namespace StateMachine.States
{
    public class UnitSelectedState : BaseState
    {
        public UnitSelectedState(InputStateMachine stateMachine): base(stateMachine)
        {

        }

        public override void Enter()
        {
            Debug.Log("Enter UnitSelected state");
            if (_stateMachine.SelectedItems.Count == 1)
            {
                var unit = _stateMachine.SelectedItems.First() as UnitItem;
                if (unit != null)
                {
                    UnitUIManager.Instance.ShowUnitInfo(unit);
                }
            }
            
        }

        public override void Exit()
        {
            Debug.Log("Exiting UnitSelected state");
            UnitUIManager.Instance.HideUnitInfo();
        }

        public override void HandleRightClick(Vector2 position)
        {
            var target = SelectorManager.Instance.GetSelectedItem(position);

            switch (target.Item2)
            {
                case SelectedStates.NothingSelect:

                    if (_stateMachine.SelectedItems.Count == 1)
                    {
                        var unitToMove = SelectorFactory.GetUnitItem(_stateMachine.SelectedItems.First());
                        if (unitToMove != null)
                        {
                            var moveAction = new MoveUnitAction(unitToMove, position);
                            UnitActionManager.Instance.ExecuteImmediately(moveAction);
                        }
                    }
                    else
                    {
                        var unitsToMove = SelectorFactory.GetUnitItems(_stateMachine.SelectedItems);
                        if (unitsToMove.Count == _stateMachine.SelectedItems.Count)
                        {
                            foreach (var unit in unitsToMove)
                            {
                                var moveGroupAction = new MoveGroupUnitAction(unit, position);
                                UnitActionManager.Instance.ExecuteImmediately(moveGroupAction);
                            }
                        }
                    }

                    Debug.Log("Units moving to position");
                    break;

                case SelectedStates.UnitSelect:

                    var selectedUnit = SelectorFactory.GetUnitItem(_stateMachine.SelectedItems.First());
                    var otherUnit = SelectorFactory.GetUnitItem(target.Item1.First());

                    if (selectedUnit.HomeTown.Id == otherUnit.HomeTown.Id)
                    {
                        if (_stateMachine.SelectedItems.Count == 1)
                        {
                            var unitToFollow = SelectorFactory.GetUnitItem(_stateMachine.SelectedItems.First());
                            if (unitToFollow != null)
                            {
                                var moveAction = new MoveUnitAction(unitToFollow, position);
                                UnitActionManager.Instance.ExecuteImmediately(moveAction);
                            }
                        }
                        else
                        {
                            var unitsToFollow = SelectorFactory.GetUnitItems(_stateMachine.SelectedItems);
                            if (unitsToFollow.Count == _stateMachine.SelectedItems.Count)
                            {
                                foreach (var unit in unitsToFollow)
                                {
                                    var moveGroupAction = new MoveGroupUnitAction(unit, position);
                                    UnitActionManager.Instance.ExecuteImmediately(moveGroupAction);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Attack other unit
                    }

                    Debug.Log("Units interacting with other unit");
                    break;

                case SelectedStates.BuildingSelect:

                    var unknownUnit = SelectorFactory.GetUnitItem(_stateMachine.SelectedItems.First());
                    var building = SelectorFactory.GetBuildingItem(target.Item1.First());

                    if (unknownUnit.HomeTown.Id == building.HomeTown.Id)
                    {
                        var unitsToBuild = SelectorFactory.GetUnitItems(_stateMachine.SelectedItems);
                        if (unitsToBuild.Count == _stateMachine.SelectedItems.Count)
                        {
                            if (!building.IsBuilt)
                            {
                                foreach (var unit in unitsToBuild)
                                {
                                    unknownUnit.HomeTown.BuildingTownOrder.AssignUnitToOrder(unit, building);
                                }
                            }
                            else
                            {
                                if (building.Backpack != null)
                                {
                                    foreach (var unit in unitsToBuild)
                                    {
                                        if (unit.Backpack.IsEmpty())
                                        {
                                            var moveGroupAction = new MoveGroupUnitAction(unit, position, true);
                                            UnitActionManager.Instance.ExecuteImmediately(moveGroupAction);
                                        }
                                        else
                                        {
                                            var bringBackResources = new BringBackResourcesAction(unit);
                                            UnitActionManager.Instance.ExecuteImmediately(bringBackResources);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var unit in unitsToBuild)
                                    {
                                        var moveGroupAction = new MoveGroupUnitAction(unit, position, true);
                                        UnitActionManager.Instance.ExecuteImmediately(moveGroupAction);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Attack enemy building...
                    }

                    Debug.Log("Units interacting with building");
                    break;

                case SelectedStates.SupplySelect:

                    var unitsToCollectResources = SelectorFactory.GetUnitItems(_stateMachine.SelectedItems);
                    var supplyToFarm = SelectorFactory.GetSupplyItem(target.Item1.First());

                    if (unitsToCollectResources.Count == _stateMachine.SelectedItems.Count)
                    {
                        foreach (var unit in unitsToCollectResources)
                        {
                            var collectResourcesAction = new CollectSupplyAction(unit, supplyToFarm);
                            UnitActionManager.Instance.ExecuteImmediately(collectResourcesAction);
                        }
                    }

                    Debug.Log("Units collecting supplies");
                    break;
            }
        }
    }
}