using System;
using System.Collections.Generic;
using GameUtilities.Utils;
using UnityEngine;

public class UnitGroupSystem
{
    private Dictionary<Guid, List<UnitItem>> _townUnits = new();

    private Dictionary<Guid, Dictionary<Vector2Int, List<UnitItem>>> _spatialGrid = new();
    
    //Grid with leaders and units without group
    private Dictionary<Guid, Dictionary<Vector2Int, List<UnitItem>>> _optimisedSpatialGrid = new();

    private const float _detectionRadius = 1.5f;

    public void RegisterUnit(UnitItem unit)
    {
        if (unit.HomeTown == null) return;

        var townId = unit.HomeTown.Id;

        InitializeTownIfNeeded(townId);

        if (!_townUnits[townId].Contains(unit))
        {
            _townUnits[townId].Add(unit);
            AddUnitToSpatialGrid(unit);
            AddUnitToOptimisedSpatialGrid(unit);
        }

        if (unit.IsInGroup && !Guid.Empty.Equals(unit.GroupId))
        {
            var group = GroupManager.Instance.GetGroup(unit.GroupId);
            if (group == null)
            {
                GroupManager.Instance.CreateGroup(unit.GroupId, unit);
            }
        }
    }

    public void UnregisterUnit(UnitItem unit)
    {
        if (unit.HomeTown == null) return;

        var townId = unit.HomeTown.Id;
        if (!_townUnits.ContainsKey(townId)) return;

        RemoveUnitFromSpatialGrid(unit, unit.Coords);
        RemoveUnitFromOptimisedSpatialGrid(unit, unit.Coords);
        _townUnits[townId].Remove(unit);

        if (unit.IsInGroup)
        {
            var group = GroupManager.Instance.GetGroup(unit.GroupId);
            if (group != null)
            {
                group.RemoveUnitFromGroup(unit);
            }
        }
    }

    public void UpdateUnitPosition(UnitItem unit, Vector2 oldPosition)
    {
        if (unit.HomeTown == null) return;

        var townId = unit.HomeTown.Id;
        if (!_townUnits.ContainsKey(townId))
        {
            RegisterUnit(unit);
        }

        RemoveUnitFromSpatialGrid(unit, oldPosition);
        RemoveUnitFromOptimisedSpatialGrid(unit, oldPosition);
        AddUnitToSpatialGrid(unit);
        AddUnitToOptimisedSpatialGrid(unit);

        if (!unit.CanGroup)
        {
            if (unit.IsInGroup)
            {
                var group = GroupManager.Instance.GetGroup(unit.GroupId);
                group.RemoveUnitFromGroup(unit);
            }
            return;
        }

        var nearbyUnits = FindNearbyUnits(unit, townId);

        ProcessUnitGrouping(unit, nearbyUnits);
    }

    private void InitializeTownIfNeeded(Guid townId)
    {
        if (!_townUnits.ContainsKey(townId))
        {
            _townUnits[townId] = new List<UnitItem>();
            if (!_spatialGrid.ContainsKey(townId))
            {
                _spatialGrid[townId] = new Dictionary<Vector2Int, List<UnitItem>>();
            }

            if (!_optimisedSpatialGrid.ContainsKey(townId))
            {
                _optimisedSpatialGrid[townId] = new Dictionary<Vector2Int, List<UnitItem>>();
            }
        }
    }

    private void AddUnitToSpatialGrid(UnitItem unit)
    {
        if (unit.HomeTown == null) return;

        var townId = unit.HomeTown.Id;
        var unitPosition = new Vector2(unit.X, unit.Y);
        var cell = GridService.GetCellGridPosition(unitPosition);
        
        if (!_spatialGrid[townId].ContainsKey(cell))
        {
            _spatialGrid[townId][cell] = new List<UnitItem>();
        }

        _spatialGrid[townId][cell].Add(unit);
    }

    private void AddUnitToOptimisedSpatialGrid(UnitItem unit)
    {
        if (unit.HomeTown == null) return;

        var townId = unit.HomeTown.Id;
        var unitPosition = new Vector2(unit.X, unit.Y);
        var cell = GridService.GetCellGridPosition(unitPosition);

        if (!_optimisedSpatialGrid[townId].ContainsKey(cell))
        {
            _optimisedSpatialGrid[townId][cell] = new List<UnitItem>();
        }

        var group = GroupManager.Instance.GetGroup(unit.GroupId);
        if (!unit.IsInGroup || group.UnitLeader.Id == unit.Id || unit.CanGroup)
        {
            _optimisedSpatialGrid[townId][cell].Add(unit);
        }
    }

    private void RemoveUnitFromSpatialGrid(UnitItem unit, Vector2 oldPosition)
    {
        if (unit.HomeTown == null) return;

        var townId = unit.HomeTown.Id;
        var oldCell = GridService.GetCellGridPosition(oldPosition);

        if (_spatialGrid[townId].ContainsKey(oldCell))
        {
            _spatialGrid[townId][oldCell].Remove(unit);

            if (_spatialGrid[townId][oldCell].Count == 0)
            {
                _spatialGrid[townId].Remove(oldCell);
            }
        }
    }
    
    private void RemoveUnitFromOptimisedSpatialGrid(UnitItem unit, Vector2 oldPosition)
    {
        if (unit.HomeTown == null) return;

        var townId = unit.HomeTown.Id;
        var oldCell = GridService.GetCellGridPosition(oldPosition);

        if (_optimisedSpatialGrid[townId].ContainsKey(oldCell))
        {
            _optimisedSpatialGrid[townId][oldCell].Remove(unit);

            if (_optimisedSpatialGrid[townId][oldCell].Count == 0)
            {
                _optimisedSpatialGrid[townId].Remove(oldCell);
            }
        }
    }

    private HashSet<UnitItem> FindNearbyUnits(UnitItem unit, Guid townId)
    {
        var nearbyUnits = new HashSet<UnitItem> { unit };
        var currentCell = GridService.GetCellGridPosition(new Vector3(unit.X, unit.Y, 0));

        for (var i = -1; i <= 1; i++)
        {
            for (var j = -1; j <= 1; j++)
            {
                var neighborCell = new Vector2Int(currentCell.x + i, currentCell.y + j);

                if (_optimisedSpatialGrid[townId].ContainsKey(neighborCell))
                {
                    foreach (var otherUnit in _optimisedSpatialGrid[townId][neighborCell])
                    {
                        if (otherUnit.Id == unit.Id) continue;

                        var distance = CalculateDistanceFromCenters(unit, otherUnit);

                        if (distance <= _detectionRadius)
                        {
                            nearbyUnits.Add(otherUnit);
                        }
                    }
                }
            }
        }

        if (nearbyUnits.Count <= 1)
        {
            return new HashSet<UnitItem>();
        }

        return nearbyUnits;
    }

    private void ProcessUnitGrouping(UnitItem unit, HashSet<UnitItem> nearbyUnits)
    {
        if (nearbyUnits.Count == 0 && unit.IsInGroup)
        {
            var group = GroupManager.Instance.GetGroup(unit.GroupId);
            group.RemoveUnitFromGroup(unit);
            return;
        }

        var nearbyLeaders = new List<UnitItem>();
        foreach (var nearby in nearbyUnits)
        {
            if (nearby.Id == unit.Id) continue;

            if (nearby.IsInGroup && nearby.GroupId != Guid.Empty)
            {
                var group = GroupManager.Instance.GetGroup(nearby.GroupId);
                if (group != null && group.UnitLeader.Id == nearby.Id)
                {
                    nearbyLeaders.Add(nearby);
                }
            }
        }

        if (nearbyLeaders.Count > 0)
        {
            ProcessWithLeader(unit, nearbyLeaders[0]);

            if (unit.IsInGroup)
            {
                var unitGroup = GroupManager.Instance.GetGroup(unit.GroupId);
                if (unitGroup != null && unitGroup.UnitLeader.Id == unit.Id)
                {
                    for (var i = 1; i < nearbyLeaders.Count; i++)
                    {
                        MergeGroups(nearbyLeaders[i].GroupId, unit.GroupId);
                    }
                }
            }
        }
        else
        {
            if (unit.IsInGroup)
            {
                var group = GroupManager.Instance.GetGroup(unit.GroupId);
                if (group != null && group.UnitLeader.Id == unit.Id)
                {
                    foreach (var nearby in nearbyUnits)
                    {
                        if (nearby.Id != unit.Id)
                        {
                            group.AddUnitToGroup(nearby);
                        }
                    }
                }
            }
            else
            {
                var groupId = Guid.NewGuid();
                var newGroup = GroupManager.Instance.CreateGroup(groupId, unit);

                foreach (var nearby in nearbyUnits)
                {
                    if (nearby.Id != unit.Id && !nearby.IsInGroup)
                    {
                        newGroup.AddUnitToGroup(nearby);
                    }
                }
            }
        }
    }

    private void ProcessWithLeader(UnitItem unit, UnitItem leader)
    {
        if (unit.IsInGroup)
        {
            var unitGroup = GroupManager.Instance.GetGroup(unit.GroupId);
            if (unitGroup != null && unitGroup.UnitLeader.Id == unit.Id)
            {
                MergeGroups(unit.GroupId, leader.GroupId);
            }
            else
            {
                var leaderGroup = GroupManager.Instance.GetGroup(leader.GroupId);
                if (leaderGroup != null)
                {
                    leaderGroup.AddUnitToGroup(unit);
                }
            }
        }
        else
        {
            var leaderGroup = GroupManager.Instance.GetGroup(leader.GroupId);
            if (leaderGroup != null)
            {
                leaderGroup.AddUnitToGroup(unit);
            }
        }
    }
    public void MergeGroups(Guid sourceGroupId, Guid targetGroupId)
    {
        var sourceGroup = GroupManager.Instance.GetGroup(sourceGroupId);
        var targetGroup = GroupManager.Instance.GetGroup(targetGroupId);

        if (sourceGroup == null || targetGroup == null)
        {
            return;
        }

        if (sourceGroup.CountGroupUnits > targetGroup.CountGroupUnits)
        {
            (sourceGroup, targetGroup) = (targetGroup, sourceGroup);
        }

        var unitsToMove = new List<UnitItem>(sourceGroup.GroupUnits);
        foreach (var unit in unitsToMove)
        {
            sourceGroup.RemoveUnitFromGroup(unit);
            targetGroup.AddUnitToGroup(unit);
        }

        if (sourceGroup.UnitLeader.Id != targetGroup.UnitLeader.Id)
        {
            var leader = sourceGroup.UnitLeader;
            targetGroup.AddUnitToGroup(leader);
        }
    }

    private float CalculateDistanceFromCenters(UnitItem firstUnit, UnitItem secondUnit)
    {
        var firstSprite = firstUnit.SpriteRenderer;
        var secondSprite = secondUnit.SpriteRenderer;

        var firstPos = new Vector2(firstUnit.X, firstUnit.Y);
        var secondPos = new Vector2(secondUnit.X, secondUnit.Y);

        if (firstSprite != null)
        {
            Vector2 firstSize = firstSprite.bounds.size;
            firstPos += new Vector2(firstSize.x / 2f, firstSize.y / 2f);
        }

        if (secondSprite != null)
        {
            var secondSize = secondSprite.bounds.size;
            secondPos += new Vector2(secondSize.x / 2f, secondSize.y / 2f);
        }

        return UtilsClass.CalculateDistance(firstPos, secondPos);
    }

    
}