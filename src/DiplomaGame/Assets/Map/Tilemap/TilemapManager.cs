using UnityEngine;

public class TilemapManager : MonoBehaviour
{
    private TilemapVisual _tilemapVisual;
    private Tilemap _tilemap;
    private TilemapSprite _tilemapSprite;
    private int[,] _map;

    private void Start()
    {
        _tilemapVisual = new TilemapVisual();
        _tilemap = new Tilemap(MapConfig.MapWidth, MapConfig.MapHeight, MapConfig.CellSize, new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY));
        _tilemap.SetTilemapVisual(_tilemapVisual);

        var mapGenerator = new MapGenerator(new System.Random().Next(1000, 100000));
        _map = mapGenerator.GenerateMap();

        MapDisplay.DisplayMap(_map, _tilemap);
    }
}
