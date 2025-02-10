using Assets.GameUtilities.Utils;
using Supplies;
using UnityEngine;

public class SupplyManager : MonoBehaviour
{
    private MapGrid<SupplyGridObject> _grid;

    private void Start()
    {
        _grid = new MapGrid<SupplyGridObject>(
            MapConfig.MapWidth,
            MapConfig.MapHeight,
            MapConfig.CellSize,
            new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY),
            (g, x, y) => new SupplyGridObject(g, x, y)
        );

        /*var _biomeMap = SupplyGenerator.GenerateSupply(
            MapConfig.MapWidth,
            MapConfig.MapHeight,
            new System.Random().Next(1000000, 10000000),
            SuppliesConfig.Supplies,
            BiomeSuppliesConfig.BiomeSupplies,
            SuppliesConfig.SupplyPerBlocks,
            TilemapManager.GetMap());

        TestBitmapMatrix.SaveMatrixAsTexture(_biomeMap, "matrix.png");
        */
    }
}