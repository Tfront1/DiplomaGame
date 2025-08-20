using System.Collections.Generic;
using Assets.Items.Interfaces;
using UnitAction;

namespace BuildingAction
{
    public class MoveAndEquipUnitAction : BaseBuildingAction
    {
        private IBackpackItem _itemToEquip;

        public MoveAndEquipUnitAction(List<UnitItem> unit, IBackpackItem itemToEquip) : base(unit)
        {
            _itemToEquip = itemToEquip;
        }

        public override void Execute(BuildingItem target)
        {
            foreach (var unit in AssignedUnits)
            {
                var moveAction = new EquipUnitAction(unit, target, _itemToEquip);
                UnitActionManager.Instance.ExecuteImmediately(moveAction);
            }
        }
    }
}