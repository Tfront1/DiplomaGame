using System;
using System.Collections.Generic;
using System.Linq;
using GameUtilities.Utils;
using UnityEngine;


/// <summary>
/// System that manages collision detection and grouping of units within towns.
/// </summary>
public class UnitCollisionSystem
{
    // Dictionary mapping town IDs to their units
    private Dictionary<Guid, List<UnitItem>> _townUnits = new();
    
    // Dictionary mapping group IDs to sets of units
    private Dictionary<Guid, HashSet<UnitItem>> _unitGroups = new();

    // Spatial grid for efficient proximity detection
    private Dictionary<Guid, Dictionary<Vector2Int, List<UnitItem>>> _spatialGrid = new();

    // Map from unit to its group ID (for group representatives)
    private Dictionary<UnitItem, Guid> _groupRepresentatives = new();

    // Map from group ID to its representative unit
    private Dictionary<Guid, UnitItem> _groupRepresentativesByGroups = new();

    // Maximum distance for units to be considered in the same group
    private const float _detectionRadius = 4f;
    
    /// <summary>
    /// Registers a unit with the collision system
    /// </summary>
    public void RegisterUnit(UnitItem unit)
    {
        if (unit.HomeTown == null) return;

        var townId = unit.HomeTown.Id;

        InitializeTownIfNeeded(townId);

        if (!_townUnits[townId].Contains(unit))
        {
            _townUnits[townId].Add(unit);
            AddUnitToSpatialGrid(unit);
        }
    }

    /// <summary>
    /// Unregisters a unit from the collision system
    /// </summary>
    public void UnregisterUnit(UnitItem unit)
    {
        if (unit.HomeTown == null) return;

        var townId = unit.HomeTown.Id;
        if (!_townUnits.ContainsKey(townId)) return;

        RemoveUnitFromSpatialGrid(unit, new Vector2(unit.X, unit.Y));
        _townUnits[townId].Remove(unit);

        RemoveUnitFromAllGroups(unit);
    }

    /// <summary>
    /// Updates a unit's position and recalculates group associations
    /// </summary>
    public void UpdateUnitPosition(UnitItem unit, Vector2 oldPosition, Vector2 newPosition)
    {
        if (unit.HomeTown == null) return;

        var townId = unit.HomeTown.Id;
        if (!_townUnits.ContainsKey(townId))
        {
            RegisterUnit(unit);
        }

        // Update unit position in spatial grid
        RemoveUnitFromSpatialGrid(unit, oldPosition);
        AddUnitToSpatialGrid(unit);

        // Find all units near the current unit using the grid
        var nearbyUnits = FindNearbyUnits(unit, townId);

        // Process unit grouping
        ProcessUnitGrouping(unit, nearbyUnits);
    }


    /// <summary>
    /// Initializes town data structures if they don't exist
    /// </summary>
    private void InitializeTownIfNeeded(Guid townId)
    {
        if (!_townUnits.ContainsKey(townId))
        {
            _townUnits[townId] = new List<UnitItem>();
            if (!_spatialGrid.ContainsKey(townId))
            {
                _spatialGrid[townId] = new Dictionary<Vector2Int, List<UnitItem>>();
            }
        }
    }

    /// <summary>
    /// Adds a unit to the spatial grid for efficient collision detection
    /// </summary>
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

    /// <summary>
    /// Removes a unit from the spatial grid
    /// </summary>
    private void RemoveUnitFromSpatialGrid(UnitItem unit, Vector2 oldPosition)
    {
        if (unit.HomeTown == null) return;

        var townId = unit.HomeTown.Id;
        var oldPositionVector2 = new Vector2(oldPosition.x, oldPosition.y);
        var oldCell = GridService.GetCellGridPosition(oldPositionVector2);

        if (_spatialGrid[townId].ContainsKey(oldCell))
        {
            _spatialGrid[townId][oldCell].Remove(unit);

            if (_spatialGrid[townId][oldCell].Count == 0)
            {
                _spatialGrid[townId].Remove(oldCell);
            }
        }
    }

    /// <summary>
    /// Removes a unit from all groups it belongs to
    /// </summary>
    private void RemoveUnitFromAllGroups(UnitItem unit)
    {
        foreach (var groupId in _unitGroups.Keys.ToList())
        {
            if (_unitGroups[groupId].Contains(unit))
            {
                RemoveUnitFromGroup(unit, groupId);
            }
        }
    }

    /// <summary>
    /// Finds all units near a specified unit using the spatial grid
    /// </summary>
    private HashSet<UnitItem> FindNearbyUnits(UnitItem unit, Guid townId)
    {
        var nearbyUnits = new HashSet<UnitItem> { unit };
        var currentCell = GridService.GetCellGridPosition(new Vector3(unit.X, unit.Y, 0));

        // Check the current cell and neighboring cells for units
        for (var i = -1; i <= 1; i++)
        {
            for (var j = -1; j <= 1; j++)
            {
                var neighborCell = new Vector2Int(currentCell.x + i, currentCell.y + j);

                if (_spatialGrid[townId].ContainsKey(neighborCell))
                {
                    foreach (var otherUnit in _spatialGrid[townId][neighborCell])
                    {
                        if (otherUnit.Id == unit.Id) continue;

                        var distance = UtilsClass.CalculateDistance(unit.Coords, otherUnit.Coords);

                        if (distance <= _detectionRadius)
                        {
                            nearbyUnits.Add(otherUnit);
                        }
                    }
                }
            }
        }

        return nearbyUnits;
    }

    /// <summary>
    /// Processes unit grouping based on nearby units
    /// </summary>
    private void ProcessUnitGrouping(UnitItem unit, HashSet<UnitItem> nearbyUnits)
    {
        // Check if the unit is already in a group
        var isInGroup = _groupRepresentatives.TryGetValue(unit, out var unitGroupId);

        if (isInGroup)
        {
            ProcessExistingGroupMember(unit, unitGroupId, nearbyUnits);
        }
        else if (nearbyUnits.Count > 1)
        {
            ProcessNewGroupCandidate(unit, nearbyUnits);
        }
        else
        {
            // Unit is alone and not in a group
            HideUnitCounter(unit);
        }
    }

    /// <summary>
    /// Processes a unit that's already part of a group
    /// </summary>
    private void ProcessExistingGroupMember(UnitItem unit, Guid groupId, HashSet<UnitItem> nearbyUnits)
    {
        var isGroupLeader = _groupRepresentativesByGroups[groupId].Id == unit.Id;

        if (isGroupLeader)
        {
            // Unit is the group leader, add all nearby units to this group
            foreach (var nearbyUnit in nearbyUnits)
            {
                AddUnitToGroup(nearbyUnit, groupId);
            }
        }
        else
        {
            // Unit is not the leader but is in a group
            var leader = _groupRepresentativesByGroups[groupId];

            foreach (var nearbyUnit in nearbyUnits)
            {
                var distanceToLeader = UtilsClass.CalculateDistance(leader.Coords, nearbyUnit.Coords);

                if (distanceToLeader <= _detectionRadius)
                {
                    AddUnitToGroup(nearbyUnit, groupId);
                }
            }
        }

        UpdateGroupCounter(groupId);
    }

    /// <summary>
    /// Adds a unit to a specific group, handling any previous group memberships
    /// </summary>
    private void AddUnitToGroup(UnitItem unit, Guid groupId)
    {
        // Check if unit is in another group
        foreach (var gId in _unitGroups.Keys.ToList())
        {
            if (gId != groupId && _unitGroups[gId].Contains(unit))
            {
                RemoveUnitFromGroup(unit, gId);
                break;
            }
        }

        // Add unit to the specified group
        _unitGroups[groupId].Add(unit);
    }

    /// <summary>
    /// Processes a unit that's not in any group but has nearby units
    /// </summary>
    private void ProcessNewGroupCandidate(UnitItem unit, HashSet<UnitItem> nearbyUnits)
    {
        // Check if any nearby units are in a group
        var existingGroupId = FindExistingGroupForNearbyUnits(unit, nearbyUnits);

        if (existingGroupId.HasValue)
        {
            _unitGroups[existingGroupId.Value].Add(unit);
            UpdateGroupCounter(existingGroupId.Value);
        }
        else
        {
            CreateNewGroup(unit, nearbyUnits);
        }
    }

    /// <summary>
    /// Finds if any nearby units are in an existing group
    /// </summary>
    private Guid? FindExistingGroupForNearbyUnits(UnitItem unit, HashSet<UnitItem> nearbyUnits)
    {
        foreach (var nearbyUnit in nearbyUnits)
        {
            foreach (var groupId in _unitGroups.Keys)
            {
                if (_unitGroups[groupId].Contains(nearbyUnit))
                {
                    var leader = _groupRepresentativesByGroups[groupId];
                    var distanceToLeader = UtilsClass.CalculateDistance(leader.Coords, unit.Coords);

                    if (distanceToLeader <= _detectionRadius)
                    {
                        return groupId;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a new group with the given units
    /// </summary>
    private void CreateNewGroup(UnitItem leader, HashSet<UnitItem> groupUnits)
    {
        var groupId = Guid.NewGuid();
        _unitGroups[groupId] = new HashSet<UnitItem>(groupUnits);

        // Set the leader as the representative for the group
        _groupRepresentativesByGroups[groupId] = leader;
        _groupRepresentatives[leader] = groupId;

        UpdateGroupCounter(groupId);
    }

    /// <summary>
    /// Removes a unit from a specific group
    /// </summary>
    private void RemoveUnitFromGroup(UnitItem unit, Guid groupId)
    {
        var isRepresentative = _groupRepresentatives.TryGetValue(unit, out var representativeGroupId) &&
                               representativeGroupId == groupId;

        // Remove unit from the group
        _unitGroups[groupId].Remove(unit);

        // If removing the representative and there are still units in the group
        if (isRepresentative && _unitGroups[groupId].Count > 0)
        {
            UpdateGroupRepresentative(unit, groupId);
        }

        HandleGroupAfterUnitRemoval(groupId);
    }

    /// <summary>
    /// Updates the group representative when the current one is removed
    /// </summary>
    private void UpdateGroupRepresentative(UnitItem oldRepresentative, Guid groupId)
    {
        _groupRepresentatives.Remove(oldRepresentative);

        // Choose a new representative
        var newRepresentative = _unitGroups[groupId].OrderBy(u => u.Id.ToString()).First();

        // Update representative records
        _groupRepresentatives[newRepresentative] = groupId;
        _groupRepresentativesByGroups[groupId] = newRepresentative;
    }

    /// <summary>
    /// Handles group state after a unit is removed
    /// </summary>
    private void HandleGroupAfterUnitRemoval(Guid groupId)
    {
        if (_unitGroups[groupId].Count > 1)
        {
            UpdateGroupCounter(groupId);
        }
        else if (_unitGroups[groupId].Count == 1)
        {
            DisbandSingleUnitGroup(groupId);
        }
        else
        {
            // Group is empty, remove it
            _groupRepresentativesByGroups.Remove(groupId);
            _unitGroups.Remove(groupId);
        }
    }

    /// <summary>
    /// Disbands a group that has only one unit left
    /// </summary>
    private void DisbandSingleUnitGroup(Guid groupId)
    {
        // Remove representative record
        if (_groupRepresentativesByGroups.ContainsKey(groupId))
        {
            _groupRepresentativesByGroups.Remove(groupId);
        }

        // Clear counter for the last unit
        var lastUnit = _unitGroups[groupId].First();
        if (_groupRepresentatives.ContainsKey(lastUnit))
        {
            _groupRepresentatives.Remove(lastUnit);
        }

        HideUnitCounter(lastUnit);

        // Remove the group
        _unitGroups.Remove(groupId);
    }

    /// <summary>
    /// Hides the counter display for a unit
    /// </summary>
    private void HideUnitCounter(UnitItem unit)
    {
        var counterDisplay = unit.GetComponent<UnitCounterDisplay>();
        if (counterDisplay != null)
        {
            counterDisplay.UpdateCount(0);
        }
    }

    /// <summary>
    /// Updates the group counter display on the representative unit
    /// </summary>
    private void UpdateGroupCounter(Guid groupId)
    {
        if (!_unitGroups.ContainsKey(groupId))
            return;

        var count = _unitGroups[groupId].Count;
        var representative = _groupRepresentativesByGroups[groupId];

        // Update counter on representative
        var counterDisplay = representative.GetComponent<UnitCounterDisplay>();
        if (counterDisplay != null)
        {
            counterDisplay.UpdateCount(count);
        }

        // Hide counters on all other units in the group
        foreach (var unit in _unitGroups[groupId])
        {
            if (unit.Id != representative.Id)
            {
                HideUnitCounter(unit);
            }
        }
    }

}
