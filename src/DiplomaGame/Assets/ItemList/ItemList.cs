using System;
using System.Collections.Generic;
using System.Linq;

public class ItemList<T> : ITypedItemList where T : IItemListObject
{
    private readonly Dictionary<Guid, T> _items = new();
    
    public Type ItemListObjectType => typeof(T);

    public void Add(T value)
    {
        _items.Add(value.Guid, value);
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
        return _items.Remove(guid);
    }

    public void Clear()
    {
        _items.Clear();
    }

    public int Count => _items.Count;

    public IEnumerable<Guid> Keys => _items.Keys;
    public IEnumerable<T> Values => _items.Values;
}