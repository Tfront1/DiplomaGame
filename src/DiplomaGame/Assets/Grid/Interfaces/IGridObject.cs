using System;

public interface IGridObject
{
    int X { get; }
    int Y { get; }
    Guid Guid { get; }
    Guid GetGuid();
}
