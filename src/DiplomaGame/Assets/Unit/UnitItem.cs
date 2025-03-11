using System;
using Items.Resource.BackPack;
using Town;
using UnityEngine;

public class UnitItem : MonoBehaviour, IUnit
{
    public Guid Id { get; set; }
    public Unit Unit { get; set; }
    public float X => UnitGameObject.transform.position.x;
    public float Y => UnitGameObject.transform.position.y;
    public Vector2 Coords => new(X, Y);
    public GameObject UnitGameObject { get; set; }

    //Gameplay
    public UnitStats Stats { get; set; }
    public UnitSkills Skills { get; set; }
    public Backpack UnitBackpack { get; set; }
    public event EventHandler<UnitDiedEventArgs> OnDied;
    public TownItem HomeTown { get; set; }

    private bool _toUpdateGroups = true;

    public static UnitItem Create(Vector2 position, Guid guid, Unit unit, GameObject unitGameObject)
    {
        var unitItem = unitGameObject.AddComponent<UnitItem>();
        unitItem.Initialize(position, guid, unit);
        return unitItem;
    }

    public void Initialize(Vector2 position, Guid guid, Unit unit)
    {
        Id = guid;
        Unit = unit;
        UnitGameObject = gameObject;

        gameObject.AddComponent<UnitCounterDisplay>();
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
        if (_toUpdateGroups)
        {
            UnitCollisionManager.Instance.RequestPositionUpdate(this, UnitGameObject.transform.position, position);
        }
        UnitGameObject.transform.position = new Vector3(position.x, position.y, zPos);
    }

    public void Die()
    {
        TickRateSystem.Instance.OnTick -= Stats.Update;
        TickRateSystem.Instance.OnTick -= Skills.Update;

        UnitCollisionManager.Instance.RemoveUnitTracking(this);

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
