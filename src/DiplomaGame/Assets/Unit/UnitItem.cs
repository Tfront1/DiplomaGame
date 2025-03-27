using System;
using Items.Resource.BackPack;
using Selection.Interfaces;
using Town;
using UnityEngine;

public class UnitItem : MonoBehaviour, ISelectable, IUnit
{
    public Guid Id { get; set; }
    public Unit Unit { get; set; }
    public float X => UnitGameObject.transform.position.x;
    public float Y => UnitGameObject.transform.position.y;
    public float Z => UnitGameObject.transform.position.z;
    public Vector3 Coords => new(X, Y, Z);
    public GameObject UnitGameObject { get; set; }
    public SpriteRenderer SpriteRenderer { get; set; }
    public BoxCollider2D Collider { get; set; }

    //Gameplay
    public UnitStats Stats { get; set; }
    public UnitSkills Skills { get; set; }
    public Backpack Backpack { get; set; }
    public TownItem HomeTown { get; set; }
    public UnitEquipment UnitEquipment { get; set; }

    public Guid GroupId { get; set; } = Guid.Empty;
    public bool IsInGroup { get; set; } = false;
    public bool CanGroup { get; set; } = false;
    public UnitCounterDisplay DisplayGroupCounter { get; set; }

    public bool IsSelected { get; set; } = false;
    public GameObject SelectionIndicator { get; set; }

    public event EventHandler<UnitDiedEventArgs> OnDied;
    public event EventHandler<UnitUIToChangeEventArgs> UIToChange;

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

        SetPosition(position);
        SetupCollider();
        DisplayGroupCounter = UnitGameObject.AddComponent<UnitCounterDisplay>();

        SetupSelectionIndicator();
        InitializeStats();
        InitializeInventory();
    }

    public void SetPosition(Vector2 position)
    {
        var zPos = (MapConfig.MapHeight * MapConfig.CellSize - UnitGameObject.transform.position.y) * -0.001f;
        UnitGameObject.transform.position = new Vector3(position.x, position.y, zPos);

        UnitGroupManager.Instance.RequestPositionUpdate(this, UnitGameObject.transform.position);
    }

    public void SetPositionWithGroup(Vector2 position)
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

    public void OnSelect()
    {
        IsSelected = true;
        if (!IsInGroup)
        {
            SelectionIndicator.SetActive(true);
        }
        else
        {
            var group = GroupManager.Instance.GetGroup(GroupId);
            if (group != null && group.UnitLeader.Id == Id)
            {
                SelectionIndicator.SetActive(true);
            }
        }
    }

    public void OnDeselect()
    {
        IsSelected = false;
        SelectionIndicator.SetActive(false);
    }

    public void HideUnit()
    {
        SelectionIndicator.SetActive(false);
        SpriteRenderer.enabled = false;
        Collider.enabled = false;
    }

    public void ShowUnit()
    {
        if (IsSelected)
        {
            if (IsInGroup)
            {
                var group = GroupManager.Instance.GetGroup(GroupId);
                if (group != null && group.UnitLeader.Id == Id)
                {
                    SelectionIndicator.SetActive(true);
                }
            }
        }
        SpriteRenderer.enabled = true;
        Collider.enabled = true;
    }

    public void Die()
    {
        TickRateSystem.Instance.OnTick -= Stats.Update;
        TickRateSystem.Instance.OnTick -= Skills.Update;

        Backpack.BackpackChanged -= OnBackpackChanged;

        UnitGroupManager.Instance.RemoveUnitTracking(this);

        OnUnitDied();

        Destroy(UnitGameObject);
    }

    protected virtual void OnUnitDied()
    {
        var args = new UnitDiedEventArgs(this);

        OnDied?.Invoke(this, args);
    }

    public Guid GetId()
    {
        return Id;
    }

    private void SetupCollider()
    {
        var spriteSize = SpriteRenderer.sprite.rect.size / SpriteRenderer.sprite.pixelsPerUnit;
        Collider = UnitGameObject.AddComponent<BoxCollider2D>();
        Collider.size = spriteSize;
        Collider.offset = new Vector2(spriteSize.x / 2f, spriteSize.y / 2f);
        UnitGameObject.layer = LayerMask.NameToLayer("Units");
    }

    private void SetupSelectionIndicator()
    {
        if (SelectionIndicator == null)
        {
            var spriteSize = SpriteRenderer.sprite.rect.size / SpriteRenderer.sprite.pixelsPerUnit;
            SelectionIndicator = new GameObject("SelectionIndicator");
            SelectionIndicator.transform.SetParent(transform);
            SelectionIndicator.transform.localPosition = Vector3.zero;

            var indicatorRenderer = SelectionIndicator.AddComponent<SpriteRenderer>();
            indicatorRenderer.sprite = GetComponent<SpriteRenderer>().sprite;
            indicatorRenderer.color = new Color(0, 1, 0, 0.6f);
            indicatorRenderer.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder - 1;

            SelectionIndicator.transform.localScale = new Vector3(1.2f, 1.2f, 1);

            var offset = new Vector3(spriteSize.x * 0.5f, spriteSize.y * 0.5f, 0);
            var locPosX = -(offset * (1.2f - 1.0f)).x;
            var locPosY = -(offset * (1.2f - 1.0f)).y;
            SelectionIndicator.transform.localPosition = new Vector3(locPosX, locPosY, offset.z);
        }

        SelectionIndicator.SetActive(false);
    }

    private void InitializeStats()
    {
        // TODO: Add to config
        Stats = new UnitStats(
            health: 100f,
            maxHealth: 100f,
            armor: 0f,
            maxArmor: 50f,
            stamina: 100f,
            maxStamina: 100f,
            hunger: 100f,
            maxHunger: 100f
        );

        Skills = new UnitSkills();

        TickRateSystem.Instance.OnTick += Stats.Update;
        TickRateSystem.Instance.OnTick += Skills.Update;
    }

    private void InitializeInventory()
    {
        // TODO: Add to config
        Backpack = new Backpack(500);
        Backpack.BackpackChanged += OnBackpackChanged;
        UnitEquipment = new UnitEquipment(this);
    }

    private void OnBackpackChanged(object sender, Backpack.BackpackChangedEventArgs args)
    {
        var item = args.Item;
        var backpackChangeType = args.ChangeType;

        if (backpackChangeType != Backpack.BackpackChangeType.Added)
            return;

        UnitEquipment.TryAutoEquip(item, args.Quantity);
    }

    public class UnitDiedEventArgs : EventArgs
    {
        public UnitItem Unit { get; }

        public UnitDiedEventArgs(UnitItem unit)
        {
            Unit = unit;
        }
    }

    public class UnitUIToChangeEventArgs : EventArgs
    {
        public UnitItem Unit { get; }

        public UnitUIToChangeEventArgs(UnitItem unit)
        {
            Unit = unit;
        }
    }
}
