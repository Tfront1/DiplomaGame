using System;
using Items.Resource.BackPack;
using Items.Resource;
using Selection.Interfaces;
using Supplies;
using UnityEngine;

public class SupplyItem : MonoBehaviour, IItemListObject, ISelectable
{
    public Guid Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public Vector2Int Coords => new(X, Y);
    public Vector2 CenterCoords => new(X + Supply.WidthCell / 2.0f, Y + Supply.HeightCell / 2.0f);
    public Supply Supply { get; set; }
    public GameObject SupplyGameObject { get; set; }
    public SpriteRenderer SpriteRenderer { get; set; }
    public BoxCollider2D Collider { get; set; }

    // Gameplay
    public Backpack Backpack { get; set; }
    public ResourceElement ResourceElement { get; set; }

    public bool IsSelected { get; set; } = false;
    public GameObject SelectionIndicator { get; set; }

    public event EventHandler<SupplyUIToChangeEventArgs> UIToChange;
    public event EventHandler<SupplyDestroyedEventArgs> OnDestroyed;

    private bool _isDestroyed = false;
    private readonly object _resourceLock = new();

    public static SupplyItem Create(Vector2Int position, Guid guid, Supply supply,
                                   GameObject supplyGameObject, Backpack backpack)
    {
        var supplyItem = supplyGameObject.AddComponent<SupplyItem>();
        supplyItem.Initialize(position, guid, supply, supplyGameObject, backpack);
        return supplyItem;
    }

    public void Initialize(Vector2Int position, Guid guid, Supply supply,
                          GameObject supplyGameObject, Backpack backpack)
    {
        SetBasicProperties(position, guid, supply, supplyGameObject);
        SetupComponents();
        CreateSelectionIndicator();
        SetupResourceSystem(backpack, supply);
    }

    private void SetBasicProperties(Vector2Int position, Guid guid, Supply supply, GameObject supplyGameObject)
    {
        X = position.x;
        Y = position.y;
        Id = guid;
        Supply = supply;
        SupplyGameObject = supplyGameObject;
    }

    private void SetupComponents()
    {
        SpriteRenderer = SupplyGameObject.GetComponent<SpriteRenderer>();
        Collider = SupplyGameObject.GetComponent<BoxCollider2D>();
    }

    private void CreateSelectionIndicator()
    {
        if (SelectionIndicator == null)
        {
            SelectionIndicator = new GameObject("SelectionIndicator");
            SelectionIndicator.transform.SetParent(transform);
            SelectionIndicator.transform.localPosition = Vector3.zero;

            var indicatorRenderer = SelectionIndicator.AddComponent<SpriteRenderer>();
            indicatorRenderer.sprite = GetComponent<SpriteRenderer>().sprite;
            indicatorRenderer.color = new Color(0, 1, 0, 0.6f);
            indicatorRenderer.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder - 1;
            SelectionIndicator.transform.localScale = new Vector3(1.2f, 1.2f, 1);
        }

        SelectionIndicator.SetActive(false);
    }

    private void SetupResourceSystem(Backpack backpack, Supply supply)
    {
        Backpack = backpack;
        ResourceElement = ResourcesConfig.ResourceElements.Find(x => x.Id == supply.ResourceId);

        Backpack.FillWithSingleItem(ResourceElement);
    }

    public void CollectResources(UnitItem unit, BackpackItem toCollect)
    {
        lock (_resourceLock)
        {
            BackpackTransfer.Instance.TransferSpecificResource(Backpack, unit.Backpack, toCollect);
            UIToChange?.Invoke(this, new SupplyUIToChangeEventArgs(this));

            if (Backpack.IsEmpty())
            {
                SupplyManager.RemoveSupply(Coords);
            }
        }
    }

    public void OnSelect()
    {
        IsSelected = true;
        SelectionIndicator.SetActive(true);
    }

    public void OnDeselect()
    {
        IsSelected = false;
        SelectionIndicator.SetActive(false);
    }

    public void Destroy()
    {
        if (!_isDestroyed)
        {
            Backpack?.Clear();

            OnDestroyed?.Invoke(this, new SupplyDestroyedEventArgs(this));
            Destroy(SupplyGameObject);

            _isDestroyed = true;
        }
    }

    public Guid GetId()
    {
        return Id;
    }

    public class SupplyUIToChangeEventArgs : EventArgs
    {
        public SupplyItem Supply { get; }

        public SupplyUIToChangeEventArgs(SupplyItem supply)
        {
            Supply = supply;
        }
    }

    public class SupplyDestroyedEventArgs : EventArgs
    {
        public SupplyItem Supply { get; }

        public SupplyDestroyedEventArgs(SupplyItem supply)
        {
            Supply = supply;
        }
    }
}
