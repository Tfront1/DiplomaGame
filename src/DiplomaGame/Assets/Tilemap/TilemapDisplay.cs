using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TilemapDisplay
{
    private readonly Dictionary<Vector3, bool> _drawnChunks = new();

    /// <summary>
    /// Displays the map on the provided tilemap by iterating through the 2D array and setting corresponding tile sprites.
    /// Uses a cache dictionary to optimize sprite lookup performance.
    /// </summary>
    /// <param name="map">A 2D array representing the map with texture IDs.</param>
    /// <param name="tilemap">The Tilemap on which the map will be displayed.</param>
    /// <remarks>
    /// This method first creates a cache of sprite IDs to their corresponding sprites,
    /// then uses this cache while iterating through the map to avoid repeated searches.
    /// The cache is created once at the start of the method and reused for all tile placements.
    /// </remarks>
    public static void DisplayMap(int[,] map, Tilemap tilemap)
    {
        var _tilemapChunkManager = TilemapChunkManager.Instance;

        var spriteCache = _tilemapChunkManager
            .GetTilemapDataAtChunkPoint(new Vector3(0, 0))
            ._tilemapSprites
            .ToDictionary(sprite => sprite._id, sprite => sprite);

        for (var i = 0; i < map.GetLength(0); i++)
        {
            for (var j = 0; j < map.GetLength(1); j++)
            {
                var textureId = map[i, j];
                if (spriteCache.TryGetValue(textureId, out var tilemapSprite))
                {
                    var textureCord = new Vector3(
                        MapConfig.MapStartPointX + i * MapConfig.CellSize,
                        MapConfig.MapStartPointY + j * MapConfig.CellSize
                    );
                    tilemap.SetTilemapSprite(textureCord, tilemapSprite);
                }
                else
                {
                    Debug.LogWarning($"Sprite with ID {textureId} not found in the tilemap data");
                }
            }
        }
    }

    private const int BUFFER_CHUNKS = 1;

    public IEnumerator DisplayVisibleChunksCoroutine(int[,] map, Tilemap tilemap, Vector3 bottomLeft, Vector3 topRight)
    {
        var startChunkX = Mathf.Max(
            Mathf.FloorToInt((bottomLeft.x - MapConfig.MapStartPointX) /
                (TilemapChunkManager._chunkSize * MapConfig.CellSize)) - BUFFER_CHUNKS,
            0
        );
        var startChunkY = Mathf.Max(
            Mathf.FloorToInt((bottomLeft.y - MapConfig.MapStartPointY) /
                (TilemapChunkManager._chunkSize * MapConfig.CellSize)) - BUFFER_CHUNKS,
            0
        );
        var maxChunksX = Mathf.CeilToInt(MapConfig.MapWidth / (float)TilemapChunkManager._chunkSize);
        var maxChunksY = Mathf.CeilToInt(MapConfig.MapHeight / (float)TilemapChunkManager._chunkSize);
        var endChunkX = Mathf.Min(
            Mathf.CeilToInt((topRight.x - MapConfig.MapStartPointX) /
                (TilemapChunkManager._chunkSize * MapConfig.CellSize)) + BUFFER_CHUNKS,
            maxChunksX - 1
        );
        var endChunkY = Mathf.Min(
            Mathf.CeilToInt((topRight.y - MapConfig.MapStartPointY) /
                (TilemapChunkManager._chunkSize * MapConfig.CellSize)) + BUFFER_CHUNKS,
            maxChunksY - 1
        );

        var _tilemapChunkManager = TilemapChunkManager.Instance;
        var spriteCache = _tilemapChunkManager
            .GetTilemapDataAtChunkPoint(new Vector3(startChunkX, startChunkY))
            ._tilemapSprites
            .ToDictionary(sprite => sprite._id, sprite => sprite);

        const int chunksPerFrame = 1;
        var processedChunks = 0;

        for (var chunkX = startChunkX; chunkX <= endChunkX; chunkX++)
        {
            for (var chunkY = startChunkY; chunkY <= endChunkY; chunkY++)
            {
                var chunkPosition = new Vector3(chunkX, chunkY, 0);
                if (!_drawnChunks.ContainsKey(chunkPosition))
                {
                    DisplayChunk(map, tilemap, spriteCache, chunkX, chunkY);
                    _drawnChunks.Add(chunkPosition, true);

                    processedChunks++;
                    if (processedChunks >= chunksPerFrame)
                    {
                        processedChunks = 0;
                        yield return null;
                    }
                }
            }
        }
    }

    public IEnumerator DisplayVisibleChunksCoroutine(int[,] map, Tilemap tilemap, Camera camera)
    {
        var bottomLeft = camera.ViewportToWorldPoint(new Vector3(0, 0, 0));
        var topRight = camera.ViewportToWorldPoint(new Vector3(1, 1, 0));
        yield return CoroutineRunner.Instance.StartCoroutine(DisplayVisibleChunksCoroutine(map, tilemap, bottomLeft, topRight));
    }

    private void DisplayChunk(int[,] map, Tilemap tilemap, Dictionary<int, TilemapSprite> spriteCache,
        int chunkX, int chunkY)
    {
        var startX = chunkX * TilemapChunkManager._chunkSize;
        var startY = chunkY * TilemapChunkManager._chunkSize;
        var endX = startX + TilemapChunkManager._chunkSize;
        var endY = startY + TilemapChunkManager._chunkSize;

        startX = Mathf.Max(0, startX);
        startY = Mathf.Max(0, startY);
        endX = Mathf.Min(map.GetLength(0), endX);
        endY = Mathf.Min(map.GetLength(1), endY);

        for (var i = startX; i < endX; i++)
        {
            for (var j = startY; j < endY; j++)
            {
                var textureId = map[i, j];
                if (spriteCache.TryGetValue(textureId, out var tilemapSprite))
                {
                    var textureCord = new Vector3(
                        MapConfig.MapStartPointX + i * MapConfig.CellSize,
                        MapConfig.MapStartPointY + j * MapConfig.CellSize
                    );
                    tilemap.SetTilemapSprite(textureCord, tilemapSprite);
                }
            }
        }
    }
}
