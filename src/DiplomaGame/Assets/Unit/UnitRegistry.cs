using System.Collections.Generic;
using UnityEngine;

public static class UnitRegistry
{
    public static List<UnitItem> UnitList = new();
    private static object _lock = new();

    public delegate void UnitEventHandler(UnitItem unitItem);
    public static event UnitEventHandler OnUnitAdded;
    public static event UnitEventHandler OnUnitRemoved;

    public static void AddUnit(UnitItem unitItem)
    {
        lock (_lock)
        {
            if (unitItem != null && !UnitList.Contains(unitItem))
            {
                UnitList.Add(unitItem);
                OnUnitAdded?.Invoke(unitItem);
            }
        }
    }

    public static void RemoveUnit(UnitItem unitItem)
    {
        lock (_lock)
        {
            if (unitItem != null && UnitList.Contains(unitItem))
            {
                UnitList.Remove(unitItem);
                OnUnitRemoved?.Invoke(unitItem);
            }
        }
    }

    public static bool IsUnitInCellRadius(Vector2Int center, int radiusCells, UnitItem unit)
    {
        var unitCell = GridService.GetCellGridPosition(unit.Coords);

        var dx = unitCell.x - center.x;
        var dy = unitCell.y - center.y;
        var squareDistance = dx * dx + dy * dy;

        return squareDistance <= radiusCells * radiusCells;
    }
}
