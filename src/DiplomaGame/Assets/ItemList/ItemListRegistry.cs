using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides a centralized registry for managing and accessing different types of item lists.
/// Acts as a singleton storage for all item lists instances in the game.
/// </summary>
public class ItemListRegistry
{
    /// <summary>
    /// Stores type-list pairs for all registered object types
    /// </summary>
    private static readonly Dictionary<Type, ITypedItemList> _itemLists = new();
    
    /// <summary>
    /// Registers a new list of objects of the specified type. If a list for this type already exists,
    /// it will be overwritten with a warning
    /// </summary>
    /// <typeparam name="T">Type of objects in the list</typeparam>
    /// <param name="list">List of objects to register</param>
    public static void RegisterList<T>(ItemList<T> list) where T : IItemListObject
    {
        var type = typeof(T);
        if (_itemLists.ContainsKey(type))
        {
            //Debug.LogWarning($"List for type {type.Name} is already registered. Overwriting existing list.");
        }

        _itemLists[type] = new TypedItemListAdapter<T>(list);
    }

    /// <summary>
    /// Returns the registered list of objects of the specified type.
    /// Returns null with a warning if the list is not found
    /// </summary>
    /// <typeparam name="T">Type of objects to search for</typeparam>
    /// <returns>List of objects or null</returns>
    public static ItemList<T> GetList<T>() where T : IItemListObject
    {
        var type = typeof(T);
        if (!_itemLists.ContainsKey(type))
        {
            Debug.LogWarning($"No list registered for type {type.Name}");
            return null;
        }

        return (_itemLists[type] as TypedItemListAdapter<T>)?._list;
    }

    /// <summary>
    /// Updates an existing list of objects. Returns false if the list for the specified type
    /// was not previously registered
    /// </summary>
    /// <typeparam name="T">Type of objects in the list</typeparam>
    /// <param name="newList">New list of objects</param>
    /// <returns>Update operation result</returns>
    public static bool UpdateList<T>(ItemList<T> newList) where T : IItemListObject
    {
        var type = typeof(T);
        if (!_itemLists.ContainsKey(type))
        {
            Debug.LogWarning($"Cannot update list: No list registered for type {type.Name}");
            return false;
        }

        _itemLists[type] = new TypedItemListAdapter<T>(newList);
        return true;
    }

    /// <summary>
    /// Updates an existing list or registers a new one if the list for the specified type
    /// hasn't been registered yet
    /// </summary>
    /// <typeparam name="T">Type of objects in the list</typeparam>
    /// <param name="newList">List of objects</param>
    /// <returns>Always returns true</returns>
    public static bool UpsertList<T>(ItemList<T> newList) where T : IItemListObject
    {
        var type = typeof(T);
        if (!_itemLists.ContainsKey(type))
        {
            RegisterList(newList);
            return true;
        }
        
        _itemLists[type] = new TypedItemListAdapter<T>(newList);
        return true;
    }

    /// <summary>
    /// Returns a read-only dictionary of all registered lists
    /// </summary>
    public static IReadOnlyDictionary<Type, ITypedItemList> GetAllLists()
    {
        return _itemLists;
    }

    /// <summary>
    /// Returns a collection of all registered lists without their type information
    /// </summary>
    public static IEnumerable<ITypedItemList> GetAllListsItemsList()
    {
        return _itemLists.Values;
    }

    /// <summary>
    /// Removes the registered list of the specified type if it exists
    /// </summary>
    /// <typeparam name="T">Type of objects in the list to remove</typeparam>
    public static void UnregisterList<T>() where T : IItemListObject
    {
        var type = typeof(T);
        if (_itemLists.ContainsKey(type))
        {
            _itemLists.Remove(type);
        }
    }

    /// <summary>
    /// Checks if there is a registered list for the specified type
    /// </summary>
    /// <typeparam name="T">Type of objects to check</typeparam>
    /// <returns>true if the list is registered</returns>
    public static bool HasList<T>() where T : IItemListObject
    {
        return _itemLists.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Clears the registry by removing all registered lists
    /// </summary>
    public static void ClearAllLists()
    {
        _itemLists.Clear();
    }
}
