using UnityEngine;

public static class TilemapManager
{
    public static TilemapVisual _tilemapVisual;
    public static Tilemap _tilemap;

    public static void SetupTilemap()
    {
        _tilemapVisual = new TilemapVisual();
        _tilemap = new Tilemap(MapConfig.MapWidth, MapConfig.MapHeight, MapConfig.CellSize, 
            new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY));
        _tilemap.SetTilemapVisual(_tilemapVisual);
    }

    public static void DisplayTilemap(int[,] biomeMap, bool displayAll)
    {
        if (!displayAll)
        {
            var tilemapViewController = new TilemapViewController();
            tilemapViewController.Init(biomeMap, _tilemap);
        }
        else
        {
            TilemapDisplay.DisplayMap(biomeMap, _tilemap);
        }
    }
}
