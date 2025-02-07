using Assets.GameUtilities.Utils;
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


        _map = BiomeManager.GetBiomeMap(new System.Random().Next(1000000, 10000000));

        //Test
        var supplyMap = SupplyGenerator.GenerateSupply(
            MapConfig.MapWidth,
            MapConfig.MapHeight,
            new System.Random().Next(1000000, 10000000),
            SuppliesConfig.Supplies,
            BiomeSuppliesConfig.BiomeSupplies,
            SuppliesConfig.SupplyPerBlocks,
            _map);

        var _grid = new MapGrid<BuildingGridObject>(
            MapConfig.MapWidth,
            MapConfig.MapHeight,
            MapConfig.CellSize,
            new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY),
            (g, x, y) => new BuildingGridObject(g, x, y)
        );

        SupplyDisplay.DisplayMap(supplyMap, _grid);

        //End Test

        TilemapDisplay.DisplayMap(_map, _tilemap);
    }
}
