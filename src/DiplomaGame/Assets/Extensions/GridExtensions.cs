using System;
using UnityEngine;

public static class GridExtensions
{
    /// <summary>
    /// Checks if a cell at the specified grid coordinates is occupied by any grid object.
    /// </summary>
    /// <param name="grid">The grid to check</param>
    /// <param name="x">X coordinate in grid space</param>
    /// <param name="y">Y coordinate in grid space</param>
    /// <returns>True if the cell is occupied or out of grid bounds, false otherwise</returns>
    public static bool IsCellOccupied(this ITypedGrid grid, int x, int y)
    {
        if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height)
            return true;

        var gridObject = grid.GetGridObjectInterface(x, y);
        return gridObject != null && gridObject.GetGuid() != Guid.Empty;
    }

    /// <summary>
    /// Checks if a cell at the specified Vector2Int position is occupied.
    /// </summary>
    /// <param name="grid">The grid to check</param>
    /// <param name="position">Grid position as Vector2Int</param>
    /// <returns>True if the cell is occupied or out of grid bounds, false otherwise</returns>
    public static bool IsCellOccupied(this ITypedGrid grid, Vector2Int position)
    {
        return grid.IsCellOccupied(position.x, position.y);
    }

    /// <summary>
    /// Checks if a cell at the specified world position is occupied.
    /// Converts world coordinates to grid coordinates before checking.
    /// </summary>
    /// <param name="grid">The grid to check</param>
    /// <param name="worldPosition">Position in world space</param>
    /// <returns>True if the cell is occupied or out of grid bounds, false otherwise</returns>
    public static bool IsCellOccupied(this ITypedGrid grid, Vector3 worldPosition)
    {
        var gridPosition = GetCellGridPosition(grid, worldPosition);
        return grid.IsCellOccupied(gridPosition);
    }

    /// <summary>
    /// Checks if a rectangular area starting at the specified position is available (not occupied).
    /// </summary>
    /// <param name="grid">The grid to check</param>
    /// <param name="position">Starting position of the area in grid coordinates</param>
    /// <param name="size">Size of the area to check (width and height)</param>
    /// <returns>True if the entire area is available, false if any part is occupied or out of bounds</returns>
    public static bool IsAreaAvailable(this ITypedGrid grid, Vector2Int position, Vector2Int size)
    {
        if (position.x < 0 || position.y < 0 ||
            position.x + size.x > grid.Width ||
            position.y + size.y > grid.Height)
        {
            return false;
        }

        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int y = position.y; y < position.y + size.y; y++)
            {
                if (grid.IsCellOccupied(x, y))
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Checks if a rectangular area starting at the specified world position is available.
    /// Converts world position to grid coordinates before checking.
    /// </summary>
    /// <param name="grid">The grid to check</param>
    /// <param name="worldPosition">Starting position in world space</param>
    /// <param name="size">Size of the area to check (width and height)</param>
    /// <returns>True if the entire area is available, false if any part is occupied or out of bounds</returns>
    public static bool IsAreaAvailable(this ITypedGrid grid, Vector3 worldPosition, Vector2Int size)
    {
        var gridPosition = GetCellGridPosition(grid, worldPosition);
        return grid.IsAreaAvailable(gridPosition, size);
    }

    /// <summary>
    /// Converts the grid to a boolean occupancy grid where false represents occupied cells
    /// and true represents available cells.
    /// </summary>
    /// <param name="grid">The grid to convert</param>
    /// <returns>A new MapGrid{bool} representing the occupancy state of each cell</returns>
    public static MapGrid<bool> ToOccupancyGrid(this ITypedGrid grid)
    {
        return new MapGrid<bool>(
            grid.Width,
            grid.Height,
            grid.CellSize,
            grid.OriginPosition,
            (_, x, y) => !grid.IsCellOccupied(x, y)
        );
    }

    /// <summary>
    /// Converts a world position to grid coordinates.
    /// </summary>
    /// <param name="grid">The grid for conversion</param>
    /// <param name="worldPosition">Position in world space</param>
    /// <returns>Grid coordinates as Vector2Int</returns>
    private static Vector2Int GetCellGridPosition(ITypedGrid grid, Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt((worldPosition - grid.OriginPosition).x / grid.CellSize),
            Mathf.FloorToInt((worldPosition - grid.OriginPosition).y / grid.CellSize)
        );
    }
}


