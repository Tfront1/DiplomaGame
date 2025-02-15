using System.Linq;
using UnityEngine;
using System;

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

    /// <summary>
    /// Gets an object with the specified GUID from any item list where it exists.
    /// </summary>
    /// <param name="guid">The unique identifier of the object to find</param>
    /// <param name="itemLists">Collection of item lists to search in</param>
    /// <returns>Object with the specified GUID if found, null otherwise</returns>
    public static object GetObjectByGuid(Guid guid, params ITypedItemList[] itemLists)
    {
        foreach (var list in itemLists)
        {
            var obj = list.GetItemListObjectInterface(guid);
            if (obj != null)
                return obj;
        }
        return null;
    }
}
