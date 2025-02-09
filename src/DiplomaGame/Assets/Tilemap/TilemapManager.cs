using Biomes;
using Supplies;
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


        float startTime, endTime;

        // Start BiomeMap generation timing
        startTime = Time.realtimeSinceStartup;
        _map = BiomeManager.GetBiomeMap(new System.Random().Next(1000000, 10000000));
        endTime = Time.realtimeSinceStartup;
        Debug.Log($"BiomeMap generation time: {(endTime - startTime) * 1000:F2}ms");

        //Test
        startTime = Time.realtimeSinceStartup;
        var supplyMap = SupplyGenerator.GenerateSupply(
            MapConfig.MapWidth,
            MapConfig.MapHeight,
            new System.Random().Next(1000000, 10000000),
            SuppliesConfig.Supplies,
            BiomeSuppliesConfig.BiomeSupplies,
            SuppliesConfig.SupplyPerBlocks,
            _map);
        endTime = Time.realtimeSinceStartup;
        Debug.Log($"SupplyMap generation time: {(endTime - startTime) * 1000:F2}ms");

        startTime = Time.realtimeSinceStartup;
        var _grid = new MapGrid<BuildingGridObject>(
            MapConfig.MapWidth,
            MapConfig.MapHeight,
            MapConfig.CellSize,
            new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY),
            (g, x, y) => new BuildingGridObject(g, x, y)
        );
        endTime = Time.realtimeSinceStartup;
        Debug.Log($"Grid creation time: {(endTime - startTime) * 1000:F2}ms");

        startTime = Time.realtimeSinceStartup;
        SupplyDisplay.DisplayMap(supplyMap, _grid);
        endTime = Time.realtimeSinceStartup;
        Debug.Log($"Supply display time: {(endTime - startTime) * 1000:F2}ms");

        //End Test

        startTime = Time.realtimeSinceStartup;
        TilemapDisplay.DisplayMap(_map, _tilemap);
        endTime = Time.realtimeSinceStartup;
        Debug.Log($"Tilemap display time: {(endTime - startTime) * 1000:F2}ms");
    }
}
