using System.Collections.Generic;
using UnitAction;

namespace BuildingAction
{
    public class MoveToBuildingAction : BaseBuildingAction
    {
        public MoveToBuildingAction(List<UnitItem> unit) : base(unit)
        {
        }

        public override void Execute(BuildingItem target)
        {
            foreach (var unit in AssignedUnits)
            {
                var moveAction = new MoveUnitAction(unit, GridService.GetWorldPosition(target.CenterCoords.x, target.CenterCoords.y), true);
                UnitActionManager.Instance.ExecuteImmediately(moveAction);
            }
        }
    }
}
