using System.Collections.Generic;
using System.Linq;
using Selection.Interfaces;

namespace Selection
{
    public static class SelectorFactory
    {
        public static UnitItem GetUnitItem(ISelectable item)
        {
            if (item is UnitItem unit)
            {
                return unit;
            }
            return null;
        }

        public static BuildingItem GetBuildingItem(ISelectable item)
        {
            if (item is BuildingItem building)
            {
                return building;
            }
            return null;
        }

        public static SupplyItem GetSupplyItem(ISelectable item)
        {
            if (item is SupplyItem supply)
            {
                return supply;
            }
            return null;
        }

        public static List<UnitItem> GetUnitItems(IEnumerable<ISelectable> items)
        {
            return items
                .OfType<UnitItem>()
                .ToList();
        }

        public static List<BuildingItem> GetBuildingItems(IEnumerable<ISelectable> items)
        {
            return items
                .OfType<BuildingItem>()
                .ToList();
        }

        public static List<SupplyItem> GetSupplyItems(IEnumerable<ISelectable> items)
        {
            return items
                .OfType<SupplyItem>()
                .ToList();
        }

        public static List<UnitItem> GetUnitItems(HashSet<ISelectable> items)
        {
            return GetUnitItems(items.AsEnumerable());
        }

        public static List<BuildingItem> GetBuildingItems(HashSet<ISelectable> items)
        {
            return GetBuildingItems(items.AsEnumerable());
        }

        public static List<SupplyItem> GetSupplyItems(HashSet<ISelectable> items)
        {
            return GetSupplyItems(items.AsEnumerable());
        }

        public static HashSet<UnitItem> GetUnitItemsHashSet(IEnumerable<ISelectable> items)
        {
            return new HashSet<UnitItem>(
                items
                    .OfType<UnitItem>()
            );
        }

        public static HashSet<BuildingItem> GetBuildingItemsHashSet(IEnumerable<ISelectable> items)
        {
            return new HashSet<BuildingItem>(
                items
                    .OfType<BuildingItem>()
            );
        }

        public static HashSet<SupplyItem> GetSupplyItemsHashSet(IEnumerable<ISelectable> items)
        {
            return new HashSet<SupplyItem>(
                items
                    .OfType<SupplyItem>()
            );
        }
    }
}