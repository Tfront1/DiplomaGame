using System;

public interface IUnit
{
    float X { get; }
    float Y { get; }
    Guid Guid { get; }
    Guid GetGuid();
}
