using System.Collections.Generic;
using UnityEngine;

public static class UnitRegistry
{
    public static List<UnitItem> UnitList = new();
    public static Dictionary<Vector2Int, List<UnitItem>> UnitsByGridCell = new();
    private static object _lock = new();

    private static Dictionary<UnitItem, float> _lastUpdateTime = new();
    private static Dictionary<UnitItem, Vector2> _pendingUpdates = new();
    private static float _updateInterval = 1.0f;

    public delegate void UnitEventHandler(UnitItem unitItem);
    public static event UnitEventHandler OnUnitAdded;
    public static event UnitEventHandler OnUnitRemoved;
    public static event UnitEventHandler OnUnitChangedCell;

    public static void AddUnit(UnitItem unitItem)
    {
        lock (_lock)
        {
            if (unitItem != null && !UnitList.Contains(unitItem))
            {
                UnitList.Add(unitItem);

                var gridCell = GridService.GetCellGridPosition(unitItem.Coords);
                AddUnitToGridCell(unitItem, gridCell);

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

                var gridCell = GridService.GetCellGridPosition(unitItem.Coords);
                RemoveUnitFromGridCell(unitItem, gridCell);

                OnUnitRemoved?.Invoke(unitItem);
            }
        }
    }

    public static void UpdateUnitGridCell(UnitItem unitItem, Vector2 newPosition)
    {
        if (unitItem == null) return;

        lock (_lock)
        {
            var currentTime = Time.time;

            if (!_lastUpdateTime.ContainsKey(unitItem) ||
                (currentTime - _lastUpdateTime[unitItem]) >= _updateInterval)
            {
                ProcessUnitUpdate(unitItem, newPosition);
                _lastUpdateTime[unitItem] = currentTime;

                if (_pendingUpdates.ContainsKey(unitItem))
                {
                    _pendingUpdates.Remove(unitItem);
                }
            }
            else
            {
                _pendingUpdates[unitItem] = newPosition;
            }
        }
    }

    private static void ProcessUnitUpdate(UnitItem unitItem, Vector2 newPosition)
    {
        var newGridCell = GridService.GetCellGridPosition(newPosition);
        var oldGridCell = GridService.GetCellGridPosition(unitItem.Coords);

        if (newGridCell != oldGridCell)
        {
            RemoveUnitFromGridCell(unitItem, oldGridCell);
            AddUnitToGridCell(unitItem, newGridCell);
            OnUnitChangedCell?.Invoke(unitItem);
        }
    }

    public static void ProcessPendingUpdates()
    {
        lock (_lock)
        {
            var currentTime = Time.time;
            var unitsToProcess = new List<UnitItem>();

            foreach (var unitEntry in _pendingUpdates)
            {
                if (!_lastUpdateTime.ContainsKey(unitEntry.Key) ||
                    (currentTime - _lastUpdateTime[unitEntry.Key]) >= _updateInterval)
                {
                    unitsToProcess.Add(unitEntry.Key);
                }
            }

            foreach (var unitItem in unitsToProcess)
            {
                if (_pendingUpdates.TryGetValue(unitItem, out var newPosition))
                {
                    ProcessUnitUpdate(unitItem, newPosition);
                    _lastUpdateTime[unitItem] = currentTime;
                    _pendingUpdates.Remove(unitItem);
                }
            }
        }
    }

    private static void AddUnitToGridCell(UnitItem unitItem, Vector2Int gridCell)
    {
        if (!UnitsByGridCell.TryGetValue(gridCell, out var unitList))
        {
            unitList = new List<UnitItem>();
            UnitsByGridCell[gridCell] = unitList;
        }

        if (!unitList.Contains(unitItem))
        {
            unitList.Add(unitItem);
        }
    }

    private static void RemoveUnitFromGridCell(UnitItem unitItem, Vector2Int gridCell)
    {
        if (UnitsByGridCell.TryGetValue(gridCell, out var unitList))
        {
            unitList.Remove(unitItem);

            if (unitList.Count == 0)
            {
                UnitsByGridCell.Remove(gridCell);
            }
        }
    }

    public static List<UnitItem> GetUnitsInRadius(Vector2Int center, int radiusCells)
    {
        var result = new List<UnitItem>();

        lock (_lock)
        {
            for (var x = center.x - radiusCells; x <= center.x + radiusCells; x++)
            {
                for (var y = center.y - radiusCells; y <= center.y + radiusCells; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (UnitsByGridCell.TryGetValue(cell, out var units))
                    {
                        result.AddRange(units);
                    }
                }
            }
        }

        return result;
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
