using Biomes;
using UnityEngine;

public class TilemapManager : MonoBehaviour
{
    private TilemapVisual _tilemapVisual;
    private Tilemap _tilemap;
    private int[,] _map;

    private void Start()
    {
        _tilemapVisual = new TilemapVisual();
        _tilemap = new Tilemap(MapConfig.MapWidth, MapConfig.MapHeight, MapConfig.CellSize, new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY));
        _tilemap.SetTilemapVisual(_tilemapVisual);

        _map = BiomeManager.GetBiomeMap(new System.Random().Next(1000000, 10000000));

        TilemapDisplay.DisplayMap(_map, _tilemap);
    }
}
