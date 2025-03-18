using System;

public interface IUnit
{
    float X { get; }
    float Y { get; }
    Guid Id { get; }
    Guid GetId();
}
