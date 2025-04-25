using Biomes;
using Supplies;
using System.Collections;
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

            LoadAllTextures();
            InitializeComponents();
            StartCoroutine(InitializeGame());
        }

        private void InitializeComponents()
        {
            TilemapManager.SetupTilemap();
        }

        private IEnumerator InitializeGame()
        {
            yield return StartCoroutine(GenerateBiomeMap());

            GenerateSupplies();
            DisplaySupplies();
            DisplayTilemap();
            SpawnTowns(100);
            SpawnTownUnits(5);
            StartAllBots();
        }

        private IEnumerator GenerateBiomeMap()
        {
            var startTime = Time.realtimeSinceStartup;
            var seed = GameRandom.Seed;
            var isCompleted = false;

            BiomeManager.GetBiomeMap(seed, (biomeMap) =>
            {
                _biomeMap = biomeMap;
                isCompleted = true;
            });

            yield return new WaitUntil(() => isCompleted);

            var endTime = Time.realtimeSinceStartup;
            Debug.Log($"BiomeMap generation time: {(endTime - startTime) * 1000:F2}ms");
        }

        private void GenerateSupplies()
        {
            var startTime = Time.realtimeSinceStartup;

            _supplyData = SupplyGenerator.GenerateSupply(
                MapConfig.MapWidth,
                MapConfig.MapHeight,
                SuppliesConfig.Supplies,
                BiomeSuppliesConfig.BiomeSupplies,
                SupplyTexturesConfig.SupplyTextures,
                SuppliesConfig.SupplyPerBlocks,
                _biomeMap
            );

            var endTime = Time.realtimeSinceStartup;
            Debug.Log($"SupplyMap generation time: {(endTime - startTime) * 1000:F2}ms");
        }

        private void DisplaySupplies()
        {
            var startTime = Time.realtimeSinceStartup;

            var supplyListInfo = _supplyData.Item2;
            SupplyManager.DisplaySupplyMap(supplyListInfo);

            var endTime = Time.realtimeSinceStartup;
            Debug.Log($"Supply display time: {(endTime - startTime) * 1000:F2}ms");
        }

        private void DisplayTilemap()
        {
            var startTime = Time.realtimeSinceStartup;

            TilemapManager.DisplayTilemap(_biomeMap, _displayAllTiles);

            var endTime = Time.realtimeSinceStartup;
            Debug.Log($"Tilemap display time: {(endTime - startTime) * 1000:F2}ms");
        }

        private void SpawnTowns(int count)
        {
            TownSpawner.SpawnTowns(count, 20, "UserTestTown");
            CameraManager.Instance.SetCameraPositionToMove(
                GridService.GetWorldPosition(TownRegistry.UserTown.TownHall.CenterCoords));
        }

        private void LoadAllTextures()
        {
            UITextureManager.LoadAllTextures();
            BuildingManager.InitializeCaches();
            SupplyManager.InitializeCaches();
            UnitManager.InitializeCaches();
        }

        private void StartAllBots()
        {
            foreach (var town in TownRegistry.TownList)
            {
                if (town.Bot != null)
                {
                    town.Bot.StartBot();
                }
            }
        }

        private void SpawnTownUnits(int startUnits)
        {
            foreach (var town in TownRegistry.TownList)
            {
                town.TownUnitSpawner.SpawnStartUnits(startUnits);
            }
        }
    }
}
