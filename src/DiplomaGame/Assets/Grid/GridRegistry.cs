using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides a centralized registry for managing and accessing different types of grids.
/// Acts as a singleton storage for all grid instances in the game.
/// </summary>
public static class GridRegistry
{
    /// <summary>
    /// Internal storage for registered grids. The key is the type of grid object,
    /// and the value is the corresponding typed grid instance.
    /// </summary>
    private static readonly Dictionary<Type, ITypedGrid> _grids = new Dictionary<Type, ITypedGrid>();

    /// <summary>
    /// Registers a new grid in the registry or updates an existing one.
    /// </summary>
    /// <typeparam name="T">The type of objects stored in the grid. Must implement IGridObject.</typeparam>
    /// <param name="grid">The grid instance to register</param>
    /// <remarks>
    /// If a grid for the specified type already exists, it will be overwritten with a warning message.
    /// The grid is wrapped in a TypedGridAdapter to maintain type safety.
    /// </remarks>
    public static void RegisterGrid<T>(MapGrid<T> grid) where T : IGridObject
    {
        var type = typeof(T);
        if (_grids.ContainsKey(type))
        {
            Debug.LogWarning($"Grid for type {type.Name} is already registered. Overwriting existing grid.");
        }
        _grids[type] = new TypedGridAdapter<T>(grid);
    }

    /// <summary>
    /// Retrieves a registered grid for the specified type.
    /// </summary>
    /// <typeparam name="T">The type of objects stored in the grid. Must implement IGridObject.</typeparam>
    /// <returns>The registered grid instance, or null if no grid is registered for the specified type</returns>
    /// <remarks>
    /// Logs a warning if no grid is found for the specified type.
    /// </remarks>
    public static MapGrid<T> GetGrid<T>() where T : IGridObject
    {
        var type = typeof(T);
        if (!_grids.ContainsKey(type))
        {
            Debug.LogWarning($"No grid registered for type {type.Name}");
            return null;
        }
        return (_grids[type] as TypedGridAdapter<T>)?._grid;
    }

    /// <summary>
    /// Updates an existing grid in the registry with a new instance.
    /// </summary>
    /// <typeparam name="T">The type of objects stored in the grid. Must implement IGridObject.</typeparam>
    /// <param name="newGrid">The new grid instance to replace the existing one</param>
    /// <returns>True if the grid was successfully updated, false if no grid was found for the specified type</returns>
    /// <remarks>
    /// Logs a warning if no grid is found for the specified type.
    /// </remarks>
    public static bool UpdateGrid<T>(MapGrid<T> newGrid) where T : IGridObject
    {
        var type = typeof(T);
        if (!_grids.ContainsKey(type))
        {
            Debug.LogWarning($"Cannot update grid: No grid registered for type {type.Name}");
            return false;
        }
        _grids[type] = new TypedGridAdapter<T>(newGrid);
        return true;
    }

    /// <summary>
    /// Gets a read-only dictionary of all registered grids.
    /// </summary>
    /// <returns>A dictionary where the key is the grid object type and the value is the corresponding grid instance</returns>
    public static IReadOnlyDictionary<Type, ITypedGrid> GetAllGrids()
    {
        return _grids;
    }

    /// <summary>
    /// Gets a collection of all registered grid instances.
    /// </summary>
    /// <returns>An enumerable collection of all grid instances</returns>
    public static IEnumerable<ITypedGrid> GetAllGridsList()
    {
        return _grids.Values;
    }

    /// <summary>
    /// Removes a grid from the registry.
    /// </summary>
    /// <typeparam name="T">The type of objects stored in the grid to remove. Must implement IGridObject.</typeparam>
    /// <remarks>
    /// If no grid is registered for the specified type, this method does nothing.
    /// </remarks>
    public static void UnregisterGrid<T>() where T : IGridObject
    {
        var type = typeof(T);
        if (_grids.ContainsKey(type))
        {
            _grids.Remove(type);
        }
    }

    /// <summary>
    /// Checks if a grid is registered for the specified type.
    /// </summary>
    /// <typeparam name="T">The type of objects stored in the grid. Must implement IGridObject.</typeparam>
    /// <returns>True if a grid is registered for the specified type, false otherwise</returns>
    public static bool HasGrid<T>() where T : IGridObject
    {
        return _grids.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Removes all registered grids from the registry.
    /// </summary>
    /// <remarks>
    /// Use with caution as this will remove all grid registrations.
    /// Make sure to re-register any necessary grids after calling this method.
    /// </remarks>
    public static void ClearAllGrids()
    {
        _grids.Clear();
    }
}