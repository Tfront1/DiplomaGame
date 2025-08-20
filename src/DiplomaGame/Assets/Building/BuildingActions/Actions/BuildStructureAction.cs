using System.Collections.Generic;

namespace BuildingAction
{
    public class BuildStructureAction : BaseBuildingAction
    {
        public BuildStructureAction(List<UnitItem> unit) : base(unit)
        {
        }

        public override void Execute(BuildingItem target)
        {
            foreach (var unit in AssignedUnits)
            {
                unit.HomeTown.BuildingTownOrder.AssignUnitToOrder(unit, target);
            }
        }
    }
}
