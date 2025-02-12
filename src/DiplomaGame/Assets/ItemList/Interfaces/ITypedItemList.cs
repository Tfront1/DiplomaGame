using System;

public interface ITypedItemList : IBaseItemList
{
    IItemListObject GetItemListObjectInterface(Guid guid);

    IItemListObject GetItemListObjectInterface(int x, int y);
}
