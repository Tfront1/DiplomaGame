using System.Linq;
using UnityEngine;

/// <summary>
/// Provides utility methods for creating and managing item list structures.
/// </summary>
public static class ItemListService
{
    /// <summary>
    /// Checks if an object with given size can be added to item list position.
    /// </summary>
    /// <param name="gridPosition">Target position on the grid</param>
    /// <param name="objectSize">Size of the object to place</param>
    /// <param name="itemLists">Collection of item lists to validate against</param>
    /// <returns>True if the object can be placed, false otherwise</returns>
    public static bool CanPlaceAtPosition(Vector2Int gridPosition, Vector2Int objectSize, params ITypedItemList[] itemLists)
    {
        return itemLists.All(g => g.IsAreaAvailable(gridPosition, objectSize));
    }
}
