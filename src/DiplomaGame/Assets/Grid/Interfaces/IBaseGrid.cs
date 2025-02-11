using System;
using UnityEngine;

public interface IBaseGrid
{
    int Width { get; }
    int Height { get; }
    float CellSize { get; }
    Vector3 OriginPosition { get; }
    Type GridObjectType { get; }
}