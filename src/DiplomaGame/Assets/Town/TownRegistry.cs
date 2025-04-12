using System.Collections.Generic;
using UnityEngine;

namespace Town
{
    public static class TownRegistry
    {
        public static TownItem UserTown { get; private set; }
        public static List<TownItem> TownList = new();

        private static object _lock = new();
        private static GameObject _townSpawnerFolder = null;

        public static GameObject TownSpawnerFolder
        {
            get
            {
                lock (_lock)
                {
                    if (_townSpawnerFolder == null)
                    {
                        _townSpawnerFolder = new GameObject("TownSpawnerFolder");
                    }
                    return _townSpawnerFolder;
                }
            }
        }

        public static void AddTown(TownItem townItem)
        {
            lock (_lock)
            {
                if (townItem != null && !TownList.Contains(townItem))
                {
                    TownList.Add(townItem);
                    if (townItem.IsUnitControlTown)
                    {
                        UserTown = townItem;
                    }
                }
            }
        }

        public static void RemoveTown(TownItem townItem)
        {
            lock (_lock)
            {
                if (townItem != null && TownList.Contains(townItem))
                {
                    TownList.Remove(townItem);
                }
            }
        }
    }
}