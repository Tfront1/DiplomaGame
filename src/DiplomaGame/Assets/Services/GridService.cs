using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Provides utility methods for creating and managing grid structures.
/// </summary>
public static class GridService
{
    /// <summary>
    /// Creates a boolean grid that shows which cells are available across multiple input grids,
    /// taking into account a specified object size.
    /// </summary>
    /// <param name="objectSize">The size of the object to check for placement (width and height in cells)</param>
    /// <param name="grids">Array of grids to check for availability. All grids must have the same dimensions</param>
    /// <returns>A boolean grid where true indicates an area is available for placement of the specified object size</returns>
    /// <exception cref="ArgumentException">Thrown when no grids are provided or when grids have different dimensions</exception>
    /// <remarks>
    /// The resulting grid will have the same dimensions as the input grids.
    /// A cell in the resulting grid will be true only if an object of the specified size
    /// can be placed at that position without overlapping any occupied cells in any of the input grids.
    /// </remarks>
    public static MapGrid<bool> CreateAvailabilityGrid(Vector2Int objectSize, params ITypedGrid[] grids)
    {
        if (grids == null || grids.Length == 0)
            throw new ArgumentException("No grids provided");

        var firstGrid = grids[0];
        ValidateGridDimensions(grids);

        return new MapGrid<bool>(
            firstGrid.Width,
            firstGrid.Height,
            firstGrid.CellSize,
            firstGrid.OriginPosition,
            (grid, x, y) => grids.All(g => g.IsAreaAvailable(new Vector2Int(x, y), objectSize))
        );
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

