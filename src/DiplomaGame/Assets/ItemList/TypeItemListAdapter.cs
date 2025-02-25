using System;

public class TypedItemListAdapter<T> : ITypedItemListAdapter, ITypedItemList where T : IItemListObject
{
    public readonly ItemList<T> _list;

    public delegate void ItemEventHandler(IItemListObject item);
    public event ItemEventHandler ItemAdded;
    public event ItemEventHandler ItemUpdated;
    public event ItemEventHandler ItemRemoved;

    public TypedItemListAdapter(ItemList<T> list)
    {
        _list = list;
        SubscribeToEvents();
    }
    public Type ItemListObjectType => typeof(T);

    public IItemListObject GetItemListObjectInterface(Guid guid)
    {
        return _list.GetItemListObjectInterface(guid);
    }

    public IItemListObject GetItemListObjectInterface(int x, int y)
    {
        return _list.GetItemListObjectInterface(x, y);
    }

    /// <summary>
    /// Subscribes to the events of the underlying ItemList
    /// </summary>
    private void SubscribeToEvents()
    {
        // Assuming ItemList<T> has these events, you'll need to add them to your ItemList<T> implementation
        _list.ItemAdded += OnItemAdded;
        _list.ItemUpdated += OnItemUpdated;
        _list.ItemRemoved += OnItemRemoved;
    }

    /// <summary>
    /// Unsubscribes from the events of the underlying ItemList
    /// </summary>
    public void UnsubscribeFromEvents()
    {
        _list.ItemAdded -= OnItemAdded;
        _list.ItemUpdated -= OnItemUpdated;
        _list.ItemRemoved -= OnItemRemoved;
    }

    private void OnItemAdded(T item)
    {
        ItemAdded?.Invoke(item);
    }

    private void OnItemUpdated(T item)
    {
        ItemUpdated?.Invoke(item);
    }

    private void OnItemRemoved(T item)
    {
        ItemRemoved?.Invoke(item);
    }
}
