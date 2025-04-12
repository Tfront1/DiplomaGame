using Biomes;
using Supplies;
using System.Collections.Generic;
using Town;
using UnityEngine;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        private int[,] _biomeMap;
        private (int[,], Dictionary<(int, int), int>) _supplyData;
        private bool _displayAllTiles = false;

        void Start()
        {
            // Debug
            GameRandom.SetSeed(new System.Random().Next(1000000, 10000000));

            InitializeComponents();
            GenerateBiomeMap();
            GenerateSupplies();
            DisplaySupplies();
            DisplayTilemap();
            SpawnTowns();
        }

        private void InitializeComponents()
        {
            TilemapManager.SetupTilemap();
        }

        private void GenerateBiomeMap()
        {
            float startTime = Time.realtimeSinceStartup;

            int seed = GameRandom.Seed;
            _biomeMap = BiomeManager.GetBiomeMap(seed);

            float endTime = Time.realtimeSinceStartup;
            Debug.Log($"BiomeMap generation time: {(endTime - startTime) * 1000:F2}ms");
        }

        private void GenerateSupplies()
        {
            float startTime = Time.realtimeSinceStartup;

            _supplyData = SupplyGenerator.GenerateSupply(
                MapConfig.MapWidth,
                MapConfig.MapHeight,
                SuppliesConfig.Supplies,
                BiomeSuppliesConfig.BiomeSupplies,
                SupplyTexturesConfig.SupplyTextures,
                SuppliesConfig.SupplyPerBlocks,
                _biomeMap
            );

            float endTime = Time.realtimeSinceStartup;
            Debug.Log($"SupplyMap generation time: {(endTime - startTime) * 1000:F2}ms");
        }

        private void DisplaySupplies()
        {
            float startTime = Time.realtimeSinceStartup;

            var supplyListInfo = _supplyData.Item2;
            SupplyManager.DisplaySupplyMap(supplyListInfo);

            float endTime = Time.realtimeSinceStartup;
            Debug.Log($"Supply display time: {(endTime - startTime) * 1000:F2}ms");
        }

        private void DisplayTilemap()
        {
            float startTime = Time.realtimeSinceStartup;

            TilemapManager.DisplayTilemap(_biomeMap, _displayAllTiles);

            float endTime = Time.realtimeSinceStartup;
            Debug.Log($"Tilemap display time: {(endTime - startTime) * 1000:F2}ms");
        }

        private void SpawnTowns()
        {
            TownSpawner.SpawnTowns(3, 20, "UserTestTown");
            CameraManager.Instance.SetCameraPositionToMove(
                GridService.GetWorldPosition(TownRegistry.UserTown.TownHall.CenterCoords));
        }
    }
}
