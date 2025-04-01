using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Provides utility methods for creating and managing grid structures.
/// </summary>
public static class GridService
{
    /// <summary>
    /// Checks if an object with given size can be placed at the specified grid position.
    /// </summary>
    /// <param name="gridPosition">Target position on the grid</param>
    /// <param name="objectSize">Size of the object to place</param>
    /// <param name="grids">Collection of grids to validate against</param>
    /// <returns>True if the object can be placed, false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when grids have mismatched dimensions</exception>
    public static bool CanPlaceAtPosition(Vector2Int gridPosition, Vector2Int objectSize, params ITypedGrid[] grids)
    {
        ValidateGridDimensions(grids);
        return grids.All(g => g.IsAreaAvailable(gridPosition, objectSize));
    }

    /// <summary>
    /// Checks if an object with given size can be placed at the specified coordinates.
    /// </summary>
    /// <param name="x">X coordinate on the grid</param>
    /// <param name="y">Y coordinate on the grid</param>
    /// <param name="objectSize">Size of the object to place</param>
    /// <param name="grids">Collection of grids to validate against</param>
    /// <returns>True if the object can be placed, false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when grids have mismatched dimensions</exception>
    public static bool CanPlaceAtPosition(int x, int y, Vector2Int objectSize, params ITypedGrid[] grids)
    {
        ValidateGridDimensions(grids);
        return CanPlaceAtPosition(new Vector2Int(x, y), objectSize, grids);
    }

    /// <summary>
    /// Checks if a position on the grid is empty across all provided grids.
    /// </summary>
    /// <param name="gridPosition">Position to check</param>
    /// <param name="grids">Collection of grids to validate against</param>
    /// <returns>True if the position is empty on all grids, false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when grids have mismatched dimensions</exception>
    public static bool IsEmptyPosition(Vector2Int gridPosition, params ITypedGrid[] grids)
    {
        ValidateGridDimensions(grids);
        return !grids.Any(g => g.IsCellOccupied(gridPosition));
    }

    /// <summary>
    /// Checks if a position specified by coordinates is empty across all provided grids.
    /// </summary>
    /// <param name="x">X coordinate on the grid</param>
    /// <param name="y">Y coordinate on the grid</param>
    /// <param name="grids">Collection of grids to validate against</param>
    /// <returns>True if the position is empty on all grids, false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when grids have mismatched dimensions</exception>
    public static bool IsEmptyPosition(int x, int y, params ITypedGrid[] grids)
    {
        return IsEmptyPosition(new Vector2Int(x, y), grids);
    }

    /// <summary>
    /// Returns a reference to the grid matrix containing objects of the specified type.
    /// </summary>
    /// <param name="type">Type of objects to search for</param>
    /// <param name="grids">Collection of grids to search in</param>
    /// <returns>A reference to the grid matrix where the specified type is found</returns>
    /// <exception cref="ArgumentException">Thrown when grids have mismatched dimensions or when type is not found</exception>
    private static ITypedGrid GetTypeGrid(Type type, params ITypedGrid[] grids)
    {
        ValidateGridDimensions(grids);

        var targetGrid = grids.FirstOrDefault(g => g.GridObjectType == type);
        if (targetGrid == null)
            throw new ArgumentException($"Grid with type {type} not found");

        return targetGrid;
    }

    /// <summary>
    /// Gets an object at the given coordinates from any grid where it exists.
    /// </summary>
    /// <param name="x">X coordinate on the grid</param>
    /// <param name="y">Y coordinate on the grid</param>
    /// <param name="grids">Collection of grids to search in</param>
    /// <returns>Object at the specified position if found, null otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when grids have mismatched dimensions</exception>
    public static object GetObjectAtPosition(int x, int y, params ITypedGrid[] grids)
    {
        ValidateGridDimensions(grids);
        foreach (var grid in grids)
        {
            var obj = grid.GetGridObjectInterface(x, y);
            if (obj != null)
                return obj;
        }
        return null;
    }

    /// <summary>
    /// Gets an object at the given position from any grid where it exists.
    /// </summary>
    /// <param name="position">Position on the grid</param>
    /// <param name="grids">Collection of grids to search in</param>
    /// <returns>Object at the specified position if found, null otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when grids have mismatched dimensions</exception>
    public static object GetObjectAtPosition(Vector2Int position, params ITypedGrid[] grids)
    {
        return GetObjectAtPosition(position.x, position.y, grids);
    }

    /// <summary>
    /// Validates that all provided grids have the same dimensions.
    /// </summary>
    /// <param name="grids">Array of grids to validate</param>
    /// <exception cref="ArgumentException">Thrown when grids have different width or height</exception>
    /// <remarks>
    /// This method compares all grids against the first grid in the array.
    /// Both width and height must match exactly across all grids.
    /// </remarks>
    private static void ValidateGridDimensions(ITypedGrid[] grids)
    {
        if (grids.Length == 0)
        {
            return;
        }
        var firstGrid = grids[0];
        foreach (var grid in grids)
        {
            if (grid.Width != firstGrid.Width || grid.Height != firstGrid.Height)
            {
                throw new ArgumentException("All grids must have the same dimensions");
            }
        }
    }

    /// <summary>
    /// Converts grid coordinates to world position.
    /// </summary>
    /// <param name="x">X coordinate in the grid.</param>
    /// <param name="y">Y coordinate in the grid.</param>
    /// <returns>World position vector corresponding to the grid cell center.</returns>
    public static Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, y) * MapConfig.CellSize + new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY);
    }

    /// <summary>
    /// Converts grid coordinates to world position.
    /// </summary>
    /// <param name="x">X coordinate in the grid.</param>
    /// <param name="y">Y coordinate in the grid.</param>
    /// <returns>World position vector corresponding to the grid cell center.</returns>
    public static Vector3 GetWorldPosition(float x, float y)
    {
        return new Vector3(x, y) * MapConfig.CellSize + new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY);
    }

    /// <summary>
    /// Converts world position to grid coordinates.
    /// </summary>
    /// <param name="worldPosition">Position in world space.</param>
    /// <returns>Grid cell coordinates as Vector2Int.</returns>
    public static Vector2Int GetCellGridPosition(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt((worldPosition - new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY)).x / MapConfig.CellSize),
            Mathf.FloorToInt((worldPosition - new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY)).y / MapConfig.CellSize));
    }

    /// <summary>
    /// Checks if the world position is within the map boundaries.
    /// </summary>
    /// <param name="position">Position in world space to check.</param>
    /// <returns>True if the position is within map bounds, false otherwise.</returns>
    public static bool IsWorldPositionInMapBounds(Vector2 position)
    {
        var gridPosition = GetCellGridPosition(position);

        return gridPosition.x >= 0 &&
               gridPosition.x < MapConfig.MapWidth * MapConfig.CellSize + MapConfig.MapStartPointX &&
               gridPosition.y >= 0 &&
               gridPosition.y < MapConfig.MapHeight * MapConfig.CellSize + MapConfig.MapStartPointY;
    }
}
