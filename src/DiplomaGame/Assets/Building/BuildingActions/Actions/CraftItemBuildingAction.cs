using System.Collections.Generic;

namespace BuildingAction
{
    public class CraftItemBuildingAction : BaseBuildingAction
    {
        public CraftItemBuildingAction(List<UnitItem> unit) : base(unit)
        {
        }

        public override void Execute(BuildingItem target)
        {
            foreach (var unit in AssignedUnits)
            {
                target.BuildingCraftingSystem?.AssignUnitToCraft(unit);
            }
        }
    }
}
