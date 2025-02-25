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
    /// Event triggered when an item list is registered, updated, or removed
    /// </summary>
    /// <param name="type">Type of the affected item list</param>
    /// <param name="action">Action performed on the list</param>
    /// <param name="list">The affected item list</param>
    public delegate void ItemListChangedEventHandler(Type type, ItemListAction action, ITypedItemList list);

    /// <summary>
    /// Event triggered when an item list is registered, updated, or removed
    /// </summary>
    public static event ItemListChangedEventHandler ItemListChanged;

    /// <summary>
    /// Event triggered when an item within a list is added, updated, or removed
    /// </summary>
    /// <param name="type">Type of the item list</param>
    /// <param name="action">Action performed on the item</param>
    /// <param name="item">The affected item</param>
    public delegate void ItemChangedEventHandler(Type type, ItemAction action, IItemListObject item);

    /// <summary>
    /// Event triggered when an item within a list is added, updated, or removed
    /// </summary>
    public static event ItemChangedEventHandler ItemChanged;

    /// <summary>
    /// Enum defining possible actions performed on an item list
    /// </summary>
    public enum ItemListAction
    {
        Registered,
        Updated,
        Removed
    }

    /// <summary>
    /// Enum defining possible actions performed on an item
    /// </summary>
    public enum ItemAction
    {
        Added,
        Updated,
        Removed
    }

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

        var typedList = new TypedItemListAdapter<T>(list);
        _itemLists[type] = typedList;

        // Subscribe to the list's events
        SubscribeToListEvents(type, typedList);

        // Notify about the list registration
        ItemListChanged?.Invoke(type, ItemListAction.Registered, typedList);
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

        // Unsubscribe from old list's events
        if (_itemLists[type] is TypedItemListAdapter<T> oldAdapter)
        {
            UnsubscribeFromListEvents(type, oldAdapter);
        }

        var typedList = new TypedItemListAdapter<T>(newList);
        _itemLists[type] = typedList;

        // Subscribe to the new list's events
        SubscribeToListEvents(type, typedList);

        // Notify about the list update
        ItemListChanged?.Invoke(type, ItemListAction.Updated, typedList);

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
        var isNewList = !_itemLists.ContainsKey(type);

        if (isNewList)
        {
            RegisterList(newList);
            return true;
        }

        // Unsubscribe from old list's events
        if (_itemLists[type] is TypedItemListAdapter<T> oldAdapter)
        {
            UnsubscribeFromListEvents(type, oldAdapter);
        }

        var typedList = new TypedItemListAdapter<T>(newList);
        _itemLists[type] = typedList;

        // Subscribe to the new list's events
        SubscribeToListEvents(type, typedList);

        // Notify about the list update
        ItemListChanged?.Invoke(type, ItemListAction.Updated, typedList);

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
            var list = _itemLists[type];

            // Unsubscribe from list's events
            if (list is TypedItemListAdapter<T> adapter)
            {
                UnsubscribeFromListEvents(type, adapter);
            }

            _itemLists.Remove(type);

            // Notify about the list removal
            ItemListChanged?.Invoke(type, ItemListAction.Removed, list);
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
        // Unsubscribe from all lists' events
        foreach (var pair in _itemLists)
        {
            if (pair.Value is ITypedItemListAdapter adapter)
            {
                adapter.UnsubscribeFromEvents();
            }

            // Notify about each list removal
            ItemListChanged?.Invoke(pair.Key, ItemListAction.Removed, pair.Value);
        }

        _itemLists.Clear();
    }

    /// <summary>
    /// Subscribes to the events of a typed list adapter
    /// </summary>
    private static void SubscribeToListEvents<T>(Type type, TypedItemListAdapter<T> adapter) where T : IItemListObject
    {
        adapter.ItemAdded += (item) => ItemChanged?.Invoke(type, ItemAction.Added, item);
        adapter.ItemUpdated += (item) => ItemChanged?.Invoke(type, ItemAction.Updated, item);
        adapter.ItemRemoved += (item) => ItemChanged?.Invoke(type, ItemAction.Removed, item);
    }

    /// <summary>
    /// Unsubscribes from the events of a typed list adapter
    /// </summary>
    private static void UnsubscribeFromListEvents<T>(Type type, TypedItemListAdapter<T> adapter) where T : IItemListObject
    {
        adapter.UnsubscribeFromEvents();
    }
}
