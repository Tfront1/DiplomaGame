using System;
using UnityEngine;

public class TypedGridAdapter<T> : ITypedGrid where T : IGridObject
{
    public readonly MapGrid<T> _grid;

    public TypedGridAdapter(MapGrid<T> grid)
    {
        _grid = grid;
    }

    public int Width => _grid.Width;
    public int Height => _grid.Height;
    public float CellSize => _grid.CellSize;
    public Vector3 OriginPosition => _grid.OriginPosition;
    public Type GridObjectType => typeof(T);

    public IGridObject GetGridObjectInterface(int x, int y)
    {
        return _grid.GetGridObject(x, y);
    }
}