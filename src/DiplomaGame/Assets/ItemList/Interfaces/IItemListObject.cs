using System;

public interface IItemListObject
{
    int X { get; }
    int Y { get; }
    Guid Id { get; }
    Guid GetGuid();
}
