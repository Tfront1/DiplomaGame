using System.Collections.Generic;

namespace Town
{
    public static class TownRegistry
    {
        public static List<TownItem> TownList = new();

        public static void AddTown(TownItem townItem)
        {
            if (townItem != null)
            {
                TownList.Add(townItem);
            }
        }

        public static void RemoveTown(TownItem townItem)
        {
            if (townItem != null && TownList.Contains(townItem))
            {
                TownList.Remove(townItem);
            }
        }
    }
}