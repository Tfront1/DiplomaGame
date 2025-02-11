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
        var firstGrid = grids[0];
        foreach (var grid in grids)
        {
            if (grid.Width != firstGrid.Width || grid.Height != firstGrid.Height)
            {
                throw new ArgumentException("All grids must have the same dimensions");
            }
        }
    }
}
