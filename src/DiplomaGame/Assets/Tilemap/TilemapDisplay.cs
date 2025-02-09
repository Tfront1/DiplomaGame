using System.Linq;
using UnityEngine;

public class TilemapDisplay
{

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
}
