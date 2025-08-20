using System.Collections.Generic;
using UnitAction;

namespace BuildingAction
{
    public class BringResourcesBuildingAction : BaseBuildingAction
    {
        public BringResourcesBuildingAction(List<UnitItem> unit) : base(unit)
        {
        }

        public override void Execute(BuildingItem target)
        {
            foreach (var unit in AssignedUnits)
            {
                if (!unit.Backpack.IsEmpty())
                {
                    var bringAction = new BringBackToBuildingResourcesAction(unit, target);
                    UnitActionManager.Instance.ExecuteImmediately(bringAction);
                }
            }
        }
    }
}
