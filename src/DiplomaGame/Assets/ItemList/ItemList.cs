using System;
using System.Collections.Generic;
using System.Linq;

public class ItemList<T> : ITypedItemList where T : IItemListObject
{
    private readonly Dictionary<Guid, T> _items = new();

    public delegate void ItemEventHandler(T item);

    public event ItemEventHandler ItemAdded;
    public event ItemEventHandler ItemUpdated;
    public event ItemEventHandler ItemRemoved;

    public Type ItemListObjectType => typeof(T);

    public void Add(T value)
    {
        _items.Add(value.Id, value);
        ItemAdded?.Invoke(value);
    }

    public IItemListObject GetItemListObjectInterface(Guid guid)
    {
        if (_items.TryGetValue(guid, out T value))
        {
            return value;
        }

        return null;
    }

    public IItemListObject GetItemListObjectInterface(int x, int y)
    {
        var item = _items.Values.FirstOrDefault(item => item.X == x && item.Y == y);
        return item;
    }

    public T GetValue(Guid guid)
    {
        if (_items.TryGetValue(guid, out T value))
        {
            return value;
        }
        return default;
    }

    public bool TryGetValue(Guid guid, out T value)
    {
        return _items.TryGetValue(guid, out value);
    }

    public bool Remove(Guid guid)
    {
        if (_items.TryGetValue(guid, out T value))
        {
            bool result = _items.Remove(guid);
            if (result)
            {
                ItemRemoved?.Invoke(value);
            }
            return result;
        }
        return false;
    }

    public void Clear()
    {
        var itemsToRemove = _items.Values.ToList();

        _items.Clear();

        foreach (var item in itemsToRemove)
        {
            ItemRemoved?.Invoke(item);
        }
    }

    public void Update(T value)
    {
        if (_items.ContainsKey(value.Id))
        {
            _items[value.Id] = value;
            ItemUpdated?.Invoke(value);
        }
        else
        {
            Add(value);
        }
    }

    public void AddOrUpdate(T value)
    {
        if (_items.ContainsKey(value.Id))
        {
            _items[value.Id] = value;
            ItemUpdated?.Invoke(value);
        }
        else
        {
            _items.Add(value.Id, value);
            ItemAdded?.Invoke(value);
        }
    }

    public int Count => _items.Count;
    public IEnumerable<Guid> Keys => _items.Keys;
    public IEnumerable<T> Values => _items.Values;
}