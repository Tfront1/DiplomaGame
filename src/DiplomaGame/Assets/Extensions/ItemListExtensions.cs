using UnityEngine;

/// <summary>
/// Extension methods for ITypedItemList to handle item positioning and area checks
/// </summary>
public static class ItemListExtensions
{
    /// <summary>
    /// Checks if an item exists at specified coordinates
    /// </summary>
    /// <param name="itemList">The item list to check</param>
    /// <param name="x">X coordinate to check</param>
    /// <param name="y">Y coordinate to check</param>
    /// <returns>True if an item exists at specified coordinates, false otherwise</returns>
    public static bool IsItemOnCellExists(this ITypedItemList itemList, int x, int y)
    {
        var itemListObject = itemList.GetItemListObjectInterface(x, y);
        return itemListObject != null;
    }

    /// <summary>
    /// Checks if an item exists at specified Vector2Int position
    /// </summary>
    /// <param name="itemList">The item list to check</param>
    /// <param name="position">Position to check as Vector2Int</param>
    /// <returns>True if an item exists at specified position, false otherwise</returns>
    public static bool IsItemOnCellExists(this ITypedItemList itemList, Vector2Int position)
    {
        return itemList.IsItemOnCellExists(position.x, position.y);
    }

    /// <summary>
    /// Checks if a rectangular area is free from any items
    /// </summary>
    /// <param name="itemList">The item list to check</param>
    /// <param name="position">Starting position of the area</param>
    /// <param name="size">Size of the area to check</param>
    /// <returns>True if the entire area is free, false if any item exists within the area</returns>
    public static bool IsAreaAvailable(this ITypedItemList itemList, Vector2Int position, Vector2Int size)
    {
        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int y = position.y; y < position.y + size.y; y++)
            {
                if (itemList.IsItemOnCellExists(x, y))
                {
                    return false;
                }
            }
        }
        return true;
    }
}