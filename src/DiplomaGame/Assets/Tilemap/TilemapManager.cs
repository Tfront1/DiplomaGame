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
        var supplyOut = SupplyGenerator.GenerateSupply(
            MapConfig.MapWidth,
            MapConfig.MapHeight,
            new System.Random().Next(1000000, 10000000),
            SuppliesConfig.Supplies,
            BiomeSuppliesConfig.BiomeSupplies,
            SupplyTexturesConfig.SupplyTextures,
            SuppliesConfig.SupplyPerBlocks,
            _map);
        endTime = Time.realtimeSinceStartup;
        Debug.Log($"SupplyMap generation time: {(endTime - startTime) * 1000:F2}ms");
        
        var supplyItemList = new ItemList<SupplyItem>();
        var supplyListInt = supplyOut.Item2;

        startTime = Time.realtimeSinceStartup;
        SupplyManager.DisplaySupplyMap(supplyListInt, supplyItemList);
        endTime = Time.realtimeSinceStartup;
        Debug.Log($"Supply display time: {(endTime - startTime) * 1000:F2}ms");

        //End Test

        startTime = Time.realtimeSinceStartup;

        //Debug
        var displayAll = false;
        if (!displayAll)
        {
            var tilemapViewController = new TilemapViewController();
            tilemapViewController.Init(_map, _tilemap);
        }
        else
        {
            TilemapDisplay.DisplayMap(_map, _tilemap);
        }
        
        endTime = Time.realtimeSinceStartup;
        Debug.Log($"Tilemap display time: {(endTime - startTime) * 1000:F2}ms");
    }
}
