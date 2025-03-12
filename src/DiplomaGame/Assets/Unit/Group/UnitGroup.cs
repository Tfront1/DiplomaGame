using System;
using System.Collections.Generic;
using System.Linq;

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
            GroupUnits.Add(unit);
            ChangeUnitSpriteRender(unit, false);
            unit.IsInGroup = true;
            unit.GroupId = Id;
            unit.SetPosition(UnitLeader.Coords);
        }
    }

    public void RemoveUnitFromGroup(UnitItem unit)
    {
        if (UnitLeader.Id == unit.Id)
        {
            if (CountGroupUnits > 0)
            {
                var newLeader = GroupUnits.First();
                ChangeGroupLeader(newLeader);
            }
            else
            {
                unit.IsInGroup = false;
                unit.GroupId = Guid.Empty;
                ChangeUnitSpriteRender(unit, true);

                GroupManager.Instance.RemoveGroup(Id);
                return;
            }
        }

        if (CountGroupUnits == 1)
        {
            unit.IsInGroup = false;
            unit.GroupId = Guid.Empty;
            ChangeUnitSpriteRender(unit, true);

            UnitLeader.IsInGroup = false;
            UnitLeader.GroupId = Guid.Empty;
            ChangeUnitSpriteRender(UnitLeader, true);

            GroupManager.Instance.RemoveGroup(Id);
            return;
        }

        if (GroupUnits.Remove(unit))
        {
            ChangeUnitSpriteRender(unit, true);
            unit.IsInGroup = false;
            unit.GroupId = Guid.Empty;
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
        ChangeUnitSpriteRender(newLeader, true);
        ChangeUnitSpriteRender(UnitLeader, false);
        GroupUnits.Add(UnitLeader);

        UnitLeader = newLeader;
    }

    private void ChangeUnitSpriteRender(UnitItem unit, bool enable)
    {
        var spriteRenderer = unit.SpriteRenderer;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = enable;
        }
    }
}