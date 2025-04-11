using System;
using System.Collections.Generic;
using Assets.Items.Interfaces;

namespace BuildingAction
{
    public static class BuildingActionFabric
    {
        public static BaseBuildingAction CreateAction(Type actionType, List<UnitItem> selectedUnits, IBackpackItem item = null)
        {
            if (actionType == typeof(MoveToBuildingAction))
                return new MoveToBuildingAction(selectedUnits);

            if (actionType == typeof(AttackBuildingAction))
                return new AttackBuildingAction(selectedUnits);

            if (actionType == typeof(BringResourcesBuildingAction))
                return new BringResourcesBuildingAction(selectedUnits);

            if (actionType == typeof(BuildStructureAction))
                return new BuildStructureAction(selectedUnits);

            if (actionType == typeof(CraftItemBuildingAction))
                return new CraftItemBuildingAction(selectedUnits);

            if (actionType == typeof(MoveAndEquipUnitAction))
                return new MoveAndEquipUnitAction(selectedUnits, item);

            throw new ArgumentException($"Unknown action type: {actionType.Name}", nameof(actionType));
        }
    }
}