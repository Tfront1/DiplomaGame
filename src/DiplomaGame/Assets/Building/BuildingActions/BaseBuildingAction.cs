using System.Collections.Generic;

namespace BuildingAction
{
    public abstract class BaseBuildingAction
    {
        public List<UnitItem> AssignedUnits { get; protected set; }

        protected BaseBuildingAction(List<UnitItem> unit)
        {
            AssignedUnits = unit;
        }

        public abstract void Execute(BuildingItem target);
    }
}
