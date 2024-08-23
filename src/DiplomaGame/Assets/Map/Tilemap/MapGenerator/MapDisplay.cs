using System.Linq;
using UnityEngine;

public class MapDisplay
{

    /// <summary>
    /// Displays the map on the provided tilemap by iterating through the 2D array and setting corresponding tile sprites.
    /// </summary>
    /// <param name="map">A 2D array representing the map with texture IDs.</param>
    /// <param name="tilemap">The Tilemap on which the map will be displayed.</param>
    /// <remarks>
    /// For each position in the map array, this method retrieves the corresponding sprite from the `TilemapChunkManager` 
    /// using the texture ID and then places the sprite on the `Tilemap` at the correct world position.
    /// </remarks>
    public static void DisplayMap(int[,] map, Tilemap tilemap)
    {
        var _tilemapChunkManager = TilemapChunkManager.Instance;

        for (var i = 0; i < map.GetLength(0); i++)
        {
            for (var j = 0; j < map.GetLength(1); j++)
            {
                var _tilemapSprite = _tilemapChunkManager.GetTilemapDataAtChunkPoint(new Vector3(0, 0))._tilemapSprites.First(x => x._id == map[i, j]);

                var textureCord = new Vector3(MapConfig.MapStartPointX + i * MapConfig.CellSize, MapConfig.MapStartPointY + j * MapConfig.CellSize);

                tilemap.SetTilemapSprite(textureCord, _tilemapSprite);
            }
        }
    }
}
