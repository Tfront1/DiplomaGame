using System;
using Items.Resource.BackPack;
using Town;
using UnityEngine;

public class UnitItem : IUnit
{
    public float X => UnitGameObject.transform.position.x;
    public float Y => UnitGameObject.transform.position.y;
    public Guid Guid { get; set; }
    public Unit Unit { get; set; }
    public GameObject UnitGameObject { get; set; }
    public UnitStats Stats { get; }
    public UnitSkills Skills { get; }
    public Backpack UnitBackpack { get;}
    public event EventHandler<UnitDiedEventArgs> OnDied;
    public TownItem HomeTown { get; set; }
    public UnitItem(float x, float y, Guid guid, Unit unit, GameObject unitGameObject)
    {
        Guid = guid;
        Unit = unit;
        UnitGameObject = unitGameObject;
        SetPosition(new Vector2(x, y));

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

    public UnitItem(Vector2 position, Guid guid, Unit unit, GameObject unitGameObject)
    {
        Guid = guid;
        Unit = unit;
        UnitGameObject = unitGameObject;
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
    }

    public void Die()
    {
        TickRateSystem.Instance.OnTick -= Stats.Update;
        TickRateSystem.Instance.OnTick -= Skills.Update;

        OnUnitDied();

        UnityEngine.Object.Destroy(UnitGameObject);
    }

    protected virtual void OnUnitDied()
    {
        var args = new UnitDiedEventArgs(this);

        OnDied?.Invoke(this, args);
    }

    public Guid GetGuid()
    {
        return Guid;
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
