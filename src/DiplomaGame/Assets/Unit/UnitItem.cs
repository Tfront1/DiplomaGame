using System;
using UnityEngine;

public class UnitItem : IUnit
{
    public float X => UnitGameObject.transform.position.x;
    public float Y => UnitGameObject.transform.position.y;
    public Guid Guid { get; set; }
    public Unit Unit { get; set; }
    public GameObject UnitGameObject { get; set; }
    public IUnitStats Stats { get; private set; }

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

        TickRateSystem.Instance.OnTick += Stats.Update;
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

        TickRateSystem.Instance.OnTick += Stats.Update;
    }

    public void SetPosition(Vector2 position)
    {
        var zPos = (MapConfig.MapHeight * MapConfig.CellSize - UnitGameObject.transform.position.y) * -0.001f;
        UnitGameObject.transform.position = new Vector3(position.x, position.y, zPos);
    }

    public Guid GetGuid()
    {
        return Guid;
    }
}
