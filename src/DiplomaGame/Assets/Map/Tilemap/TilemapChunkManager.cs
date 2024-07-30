using System;
using System.Collections.Generic;
using UnityEngine;

public class TilemapChunkManager
{
    private static TilemapChunkManager _instance;

    public static int _chunkSize = 128;
    public static Dictionary<Vector3, TilemapData> _chunks = new();

    public static TilemapChunkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new TilemapChunkManager();
            }
            return _instance;
        }
    }

    /// <summary>
    /// Initializes the `TilemapChunkManager` by creating and setting up all map chunks.
    /// </summary>
    /// <remarks>
    /// This constructor calculates the number of chunks needed based on the map dimensions and chunk size. It then iterates
    /// through the grid of chunks, creating each chunk with appropriate dimensions and world position. Each chunk is initialized
    /// with its mesh and added to the `_chunks` dictionary using its coordinates as the key.
    /// </remarks>
    private TilemapChunkManager()
    {
        var mapWidth = MapConfig.MapWidth;
        var mapHeight = MapConfig.MapHeight;
        var mapStartPointX = MapConfig.MapStartPointX;
        var mapStartPointY = MapConfig.MapStartPointY;
        var cellSize = MapConfig.CellSize;

        var chunkCountX = (int)Math.Ceiling((float)mapWidth / _chunkSize);
        var chunkCountY = (int)Math.Ceiling((float)mapHeight / _chunkSize);

        for (var x = 0; x < chunkCountX; x++)
        {
            for (var y = 0; y < chunkCountY; y++)
            {
                var chunkCoords = new Vector3(x, y);

                var chunkWidth = (x == chunkCountX - 1) ? mapWidth % _chunkSize : _chunkSize;
                var chunkHeight = (y == chunkCountY - 1) ? mapHeight % _chunkSize : _chunkSize;

                chunkWidth = chunkWidth == 0 ? _chunkSize : chunkWidth;
                chunkHeight = chunkHeight == 0 ? _chunkSize : chunkHeight;

                var newChunk = new TilemapData();
                
                var chunkWorldPosition = new Vector3(
                    mapStartPointX + (x * _chunkSize) * cellSize,
                    mapStartPointY + (y * _chunkSize) * cellSize,
                    0);

                newChunk.InitialiseMesh(chunkWidth, chunkHeight, chunkWorldPosition);

                _chunks[chunkCoords] = newChunk;
            }
        }
    }

    /// <summary>
    /// Retrieves the `TilemapData` for the chunk containing the specified grid point position.
    /// </summary>
    /// <param name="gridPointPosition">The position of the grid point within the map.</param>
    /// <returns>The `TilemapData` for the chunk that contains the grid point position, or `null` if the chunk is not found.</returns>
    /// <remarks>
    /// This method calculates the chunk coordinates from the grid point position and fetches the corresponding `TilemapData` 
    /// from the `_chunks` dictionary. If no chunk exists for the calculated coordinates, it returns `null`.
    /// </remarks>
    public TilemapData GetTilemapDataAtChunkPoint(Vector3 gridPointPosition)
    {
        var chunkX = MathF.Floor(gridPointPosition.x / _chunkSize);
        var chunkY = MathF.Floor(gridPointPosition.y / _chunkSize);

        var chunkCoords = new Vector3(chunkX, chunkY);

        return _chunks.GetValueOrDefault(chunkCoords);
    }

    /// <summary>
    /// Calculates the local position within a chunk from a global position.
    /// </summary>
    /// <param name="globalPosition">The global position in the map.</param>
    /// <returns>The local position within the chunk corresponding to the global position.</returns>
    /// <remarks>
    /// This method computes the position within the chunk by taking the remainder of the global position coordinates
    /// divided by the chunk size. This provides the local coordinates relative to the chunk boundary.
    /// </remarks>
    public Vector3 GetLocalPositionInChunk(Vector3 globalPosition)
    {
        var localX = globalPosition.x % _chunkSize;
        var localY = globalPosition.y % _chunkSize;

        return new Vector3(localX, localY);
    }

    /// <summary>
    /// Calculates the index of a grid position within its chunk.
    /// </summary>
    /// <param name="gridPosition">The grid position whose index is to be calculated.</param>
    /// <returns>The index of the grid position within its chunk.</returns>
    /// <remarks>
    /// This method retrieves the `TilemapData` for the chunk containing the grid position, then calculates the index 
    /// within the chunk based on the position coordinates. The index is computed as `(chunkX * chunkHeight + chunkY)`, 
    /// where `chunkX` and `chunkY` are the local coordinates within the chunk and `chunkHeight` is the chunk's height.
    /// </remarks>
    public int GetGridPositionChunkIndex(Vector3 gridPosition)
    {
        var tilemap = GetTilemapDataAtChunkPoint(gridPosition);

        var chunkX = (int)gridPosition.x % tilemap._height;
        var chunkY = (int)gridPosition.y % tilemap._width;

        var chunkHeight = tilemap._height;

        return chunkX * chunkHeight + chunkY;
    }

}

