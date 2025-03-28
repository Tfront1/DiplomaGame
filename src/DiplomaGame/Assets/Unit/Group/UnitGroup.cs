using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitGroup
{
    public Guid Id { get; set; }
    public UnitItem UnitLeader { get; set; }
    public HashSet<UnitItem> GroupUnits { get; set; }
    public int CountGroupUnits => GroupUnits.Count;

    public UnitGroup(Guid id, UnitItem unitLeader)
    {
        UnitLeader = unitLeader;
        Id = id;
        GroupUnits = new HashSet<UnitItem>();
        unitLeader.GroupId = id;
        unitLeader.IsInGroup = true;
    }

    public void AddUnitToGroup(UnitItem unit)
    {
        if (UnitLeader.Id != unit.Id)
        {
            if (!GroupUnits.Contains(unit))
            {
                GroupUnits.Add(unit);
                unit.HideUnit();
                unit.IsInGroup = true;
                unit.GroupId = Id;
                unit.SetPosition(UnitLeader.Coords);
                UpdateGroupCounter();
            }
        }
    }

    public void RemoveUnitFromGroup(UnitItem unit)
    {
        if (UnitLeader.Id == unit.Id)
        {
            if (CountGroupUnits > 0)
            {
                var newLeader = GroupUnits.First();
                var oldLeader = UnitLeader;
                ChangeGroupLeader(newLeader);
                GroupUnits.Remove(oldLeader);
                oldLeader.IsInGroup = false;
                oldLeader.GroupId = Guid.Empty;
                oldLeader.ShowUnit();
                UpdateGroupCounter();
                return;
            }
            else
            {
                unit.IsInGroup = false;
                unit.GroupId = Guid.Empty;
                unit.ShowUnit();
                HideUnitCounter(unit);
                GroupManager.Instance.RemoveGroup(Id);
                return;
            }
        }
        if (CountGroupUnits == 1 && GroupUnits.Contains(unit))
        {
            unit.IsInGroup = false;
            unit.GroupId = Guid.Empty;
            unit.ShowUnit();
            HideUnitCounter(unit);

            UnitLeader.IsInGroup = false;
            UnitLeader.GroupId = Guid.Empty;
            UnitLeader.ShowUnit();
            HideUnitCounter(UnitLeader);
            GroupManager.Instance.RemoveGroup(Id);
            return;
        }
        if (GroupUnits.Remove(unit))
        {
            unit.IsInGroup = false;
            unit.GroupId = Guid.Empty;
            unit.ShowUnit();
            HideUnitCounter(unit);
            UpdateGroupCounter();
        }
    }

    public bool Contains(UnitItem unit)
    {
        return GroupUnits.Contains(unit);
    }

    private void ChangeGroupLeader(UnitItem newLeader)
    {
        if (newLeader == null || newLeader == UnitLeader)
            return;

        GroupUnits.Remove(newLeader);
        newLeader.ShowUnit();
        UnitLeader.HideUnit();
        GroupUnits.Add(UnitLeader);
        HideUnitCounter(UnitLeader);
        UnitLeader = newLeader;

        UpdateGroupCounter();
    }

    private void UpdateGroupCounter()
    {
        if (UnitLeader == null) return;

        var counterDisplay = UnitLeader.DisplayGroupCounter;
        if (counterDisplay != null)
        {
            counterDisplay.UpdateCount(CountGroupUnits + 1);
        }

        foreach (var unit in GroupUnits)
        {
            HideUnitCounter(unit);
        }
    }

    private void HideUnitCounter(UnitItem unit)
    {
        var counterDisplay = unit.DisplayGroupCounter;
        if (counterDisplay != null)
        {
            counterDisplay.UpdateCount(0);
        }
    }
}