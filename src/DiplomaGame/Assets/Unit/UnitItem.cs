using System;
using System.Collections.Generic;
using System.Linq;
using Items.Resource.BackPack;
using Town;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitItem : MonoBehaviour, IUnit
{
    public Guid Id { get; set; }
    public Unit Unit { get; set; }
    public float X => UnitGameObject.transform.position.x;
    public float Y => UnitGameObject.transform.position.y;
    public float Z => UnitGameObject.transform.position.z;
    public Vector3 Coords => new(X, Y, Z);
    public GameObject UnitGameObject { get; set; }
    public SpriteRenderer SpriteRenderer { get; set; }

    //Gameplay
    public UnitStats Stats { get; set; }
    public UnitSkills Skills { get; set; }
    public Backpack UnitBackpack { get; set; }
    public event EventHandler<UnitDiedEventArgs> OnDied;
    public TownItem HomeTown { get; set; }
    public Guid GroupId { get; set; } = Guid.Empty;
    public bool IsInGroup { get; set; } = false;

    public static UnitItem Create(Vector2 position, Guid guid, Unit unit, GameObject unitGameObject, TownItem townItem)
    {
        var unitItem = unitGameObject.AddComponent<UnitItem>();
        unitItem.Initialize(position, guid, unit, townItem);
        return unitItem;
    }

    public void Initialize(Vector2 position, Guid guid, Unit unit, TownItem townItem)
    {
        Id = guid;
        Unit = unit;
        UnitGameObject = gameObject;
        HomeTown = townItem;

        SpriteRenderer = UnitGameObject.GetComponent<SpriteRenderer>();
        UnitGameObject.AddComponent<UnitCounterDisplay>();
        SetPosition(position);

        //ToDo: Config for stats
        Stats = new UnitStats(
            health: 100f,
            maxHealth: 100f,
            armor: 10f,
            maxArmor: 50f,
            stamina: 100f,
            maxStamina: 100f,
            hunger: 100f,
            maxHunger: 100f
        );

        Skills = new UnitSkills();
        TickRateSystem.Instance.OnTick += Stats.Update;
        TickRateSystem.Instance.OnTick += Skills.Update;

        //ToDo: Add config to unit
        UnitBackpack = new Backpack(1000);
    }

    public void SetPosition(Vector2 position)
    {
        var zPos = (MapConfig.MapHeight * MapConfig.CellSize - UnitGameObject.transform.position.y) * -0.001f;
        UnitGameObject.transform.position = new Vector3(position.x, position.y, zPos);

        if (!IsInGroup)
        {
            UnitGroupManager.Instance.RequestPositionUpdate(this, UnitGameObject.transform.position);
        }
        else
        {
            var group = GroupManager.Instance.GetGroup(GroupId);
            if (group != null && group.UnitLeader.Id == Id)
            {
                UnitGroupManager.Instance.RequestPositionUpdate(this, UnitGameObject.transform.position);
            }
        }

    }

    public List<UnitItem> GetGroupList()
    {
        List<UnitItem> units = null;
        var group = GroupManager.Instance.GetGroup(GroupId);

        if (IsInGroup && group != null && group.UnitLeader.Id == Id)
        {
            units = group.GroupUnits.ToList();
        }

        return units;
    }

    public void Die()
    {
        TickRateSystem.Instance.OnTick -= Stats.Update;
        TickRateSystem.Instance.OnTick -= Skills.Update;

        UnitGroupManager.Instance.RemoveUnitTracking(this);

        OnUnitDied();

        Destroy(UnitGameObject);
    }

    protected virtual void OnUnitDied()
    {
        var args = new UnitDiedEventArgs(this);

        OnDied?.Invoke(this, args);
    }

    public Guid GetGuid()
    {
        return Id;
    }

    public class UnitDiedEventArgs : EventArgs
    {
        public UnitItem Unit { get; }

        public UnitDiedEventArgs(UnitItem unit)
        {
            Unit = unit;
        }
    }
}
