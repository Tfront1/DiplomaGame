using System;
using Biomes;
using Supplies;
using System.Collections;
using System.Collections.Generic;
using FogOfWar;
using TMPro;
using Town;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        private int[,] _biomeMap;
        private (int[,], Dictionary<(int, int), int>) _supplyData;
        private bool _displayAllTiles = false;

        private GameObject _loadingPanel;
        private Slider _progressBar;
        private TextMeshProUGUI _loadingInfoText;

        void Start()
        {
            GameRandom.SetSeed(MainMenuUI.Seed);

            InitializeLoadingProgressBar();
            InitializeComponents();
            StartCoroutine(InitializeGame());
        }

        private void InitializeComponents()
        {
            TilemapManager.SetupTilemap();
        }

        private IEnumerator InitializeGame()
        {
            var totalSteps = 4; // LoadTextures, GenerateBiome, GenerateSupplies, DisplaySupplies, DisplayTilemap
            if (MainMenuUI.EnableBots)
                totalSteps += 3; // SpawnTowns, SpawnUnits, StartBots
            else
                totalSteps += 1; // SpawnTowns
            if (MainMenuUI.EnableFog)
                totalSteps += 1; // InitializeFog

            var progressPerStep = 100f / totalSteps;

            yield return StartCoroutine(ExecuteWithProgress("Loading textures...", progressPerStep,
                LoadAllTextures));

            yield return StartCoroutine(ExecuteCoroutineWithProgress("Generating biome map...", progressPerStep,
                GenerateBiomeMap));

            yield return StartCoroutine(ExecuteWithProgress("Generating supplies...", progressPerStep,
                GenerateSupplies));

            yield return StartCoroutine(ExecuteWithProgress("Displaying supplies...", progressPerStep,
                DisplaySupplies));

            yield return StartCoroutine(ExecuteWithProgress("Displaying tilemap...", progressPerStep,
                DisplayTilemap));

            if (MainMenuUI.EnableBots)
            {
                yield return StartCoroutine(ExecuteWithProgress("Spawning towns...", progressPerStep,
                    () => SpawnTowns(3)));

                yield return StartCoroutine(ExecuteWithProgress("Spawning units...", progressPerStep,
                    () => SpawnTownUnits(3)));

                yield return StartCoroutine(ExecuteWithProgress("Starting bots...", progressPerStep,
                    StartAllBots));
            }
            else
            {
                yield return StartCoroutine(ExecuteWithProgress("Spawning town...", progressPerStep,
                    () => SpawnTowns(1)));
            }

            if (MainMenuUI.EnableFog)
            {
                yield return StartCoroutine(ExecuteWithProgress("Initializing fog of war...", progressPerStep,
                    InitializeFogOfWar));
            }
        }

        private IEnumerator ExecuteWithProgress(string infoText, float progressToAdd, Action action)
        {
            SetLoadingInfoText(infoText);
            action.Invoke();
            AddLoadingProgress(progressToAdd);
            yield return null;
        }

        private IEnumerator ExecuteCoroutineWithProgress(string infoText, float progressToAdd, Func<IEnumerator> coroutineFunc)
        {
            SetLoadingInfoText(infoText);
            yield return StartCoroutine(coroutineFunc.Invoke());
            AddLoadingProgress(progressToAdd);
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
            if (!MainMenuUI.TownName.Equals(""))
            {
                TownSpawner.SpawnTowns(count, 20, MainMenuUI.TownName);
            }
            else
            {
                TownSpawner.SpawnTowns(count, 20, "UserTestTown");
            }
            CameraManager.Instance.SetCameraPositionToMove(
                GridService.GetWorldPosition(TownRegistry.UserTown.TownHall.CenterCoords));
            
            /*
            CameraManager.Instance.SetCameraPositionToMove(
                GridService.GetWorldPosition(TownRegistry.TownList.Find(x => x.IsUnitControlTown == false).TownHall.CenterCoords));
            */
        }

        private void LoadAllTextures()
        {
            if (!UITextureManager.IsLoadedAllTextures)
            {
                UITextureManager.LoadAllTextures();
            }

            if (!BuildingManager.IsInitializedCaches)
            {
                BuildingManager.InitializeCaches();
            }

            if (!SupplyManager.IsInitializedCaches)
            {
                SupplyManager.InitializeCaches();
            }

            if (!UnitManager.IsLoadedCaches)
            {
                UnitManager.InitializeCaches();
            }
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

        private void InitializeFogOfWar()
        {
            var startTime = Time.realtimeSinceStartup;

            FogOfWarDisplay.Instance.StartFog(MapConfig.MapWidth, MapConfig.MapHeight,
                new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY));

            var endTime = Time.realtimeSinceStartup;
            Debug.Log($"Fog of war display time: {(endTime - startTime) * 1000:F2}ms");
        }

        private void InitializeLoadingProgressBar()
        {
            var mainCanvas = MainCanvasUI.MainCanvas;
            var loadingPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Loading/LoadingProgressBar");

            var loadingPanel = loadingPrefab.transform.Find("Canvas/Panel");
            _loadingPanel = Instantiate(loadingPanel.gameObject, mainCanvas.transform);
            _loadingPanel.name = "BuildingInfoPanel";

            _progressBar = _loadingPanel.transform.Find("ProgressBar").GetComponent<Slider>();
            _loadingInfoText = _loadingPanel.transform.Find("InfoText").GetComponent<TextMeshProUGUI>();
        }

        private void AddLoadingProgress(float number)
        {
            _progressBar.value += number;
            if (Math.Abs(_progressBar.maxValue - _progressBar.value) < 0.01)
            {
                StartCoroutine(DestroyLoadingPanelWithDelay());
            }
        }

        private void SetLoadingInfoText(string text)
        {
            _loadingInfoText.text = text;
        }

        private IEnumerator DestroyLoadingPanelWithDelay()
        {
            yield return new WaitForSeconds(1f);
            Destroy(_loadingPanel);
        }
    }
}
