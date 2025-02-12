using System;

public class TypedItemListAdapter<T> : ITypedItemList where T : IItemListObject
{
    public readonly ItemList<T> _list;
    
    public TypedItemListAdapter(ItemList<T> list)
    {
        _list = list;
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
}
