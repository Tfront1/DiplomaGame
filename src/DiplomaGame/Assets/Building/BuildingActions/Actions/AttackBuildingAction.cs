using System.Collections.Generic;

namespace BuildingAction
{
    public class AttackBuildingAction : BaseBuildingAction
    {
        public AttackBuildingAction(List<UnitItem> unit) : base(unit)
        {
        }

        public override void Execute(BuildingItem target)
        {
            foreach (var unit in AssignedUnits)
            {
                var moveAction = new UnitAction.AttackBuildingAction(unit, target);
                UnitActionManager.Instance.ExecuteImmediately(moveAction);
            }
        }
    }
}
