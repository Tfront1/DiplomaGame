using System;
using System.Collections.Generic;
using System.Linq;
using GameUtilities.Utils.MapUtils;

namespace Supplies
{
    internal static class SupplyGenerator
    {
        internal static (int[,], Dictionary<(int, int), int>) GenerateSupply(
            int mapWidth,
            int mapHeight,
            int seed,
            List<Supply> supplies,
            List<BiomeSupply> biomeSupplies,
            List<SupplyTexture> suppliesTexture,
            int SupplyPerBlocks,
            int[,] map)
        {
            var random = new Random(seed);
            var totalSupplies = mapHeight * mapWidth / SupplyPerBlocks;
            var distribution = CalculateSupplyDistribution(supplies, totalSupplies);

            var supplyMap = InitializeSupplyMap(mapWidth, mapHeight);

            var supplyTextureCache = suppliesTexture.ToDictionary(t => t.SupplyId);
            var supplyCache = supplies.ToDictionary(t => t.Id);

            SpawnSuppliesOnMap(
                distribution,
                supplyMap,
                map,
                biomeSupplies,
                supplyTextureCache,
                supplyCache,
                mapWidth,
                mapHeight,
                random,
                out var supplyList);

            return (supplyMap, supplyList);
        }

        private static int[,] InitializeSupplyMap(int width, int height)
        {
            var supplyMap = new int[width, height];
            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                    supplyMap[x, y] = -1;
            return supplyMap;
        }

        private static void SpawnSuppliesOnMap(
            int[,] distribution,
            int[,] supplyMap,
            int[,] biomeMap,
            List<BiomeSupply> biomeSupplies,
            Dictionary<int, SupplyTexture> supplyTextureCache,
            Dictionary<int, Supply> supplyCache,
            int mapWidth,
            int mapHeight,
            Random random,
            out Dictionary<(int, int), int> outSupplyList)
        {
            var maxAttempts = mapWidth * mapHeight;
            outSupplyList = new Dictionary<(int, int), int>();

            for (var i = 0; i < distribution.GetLength(0); i++)
            {
                var supplyId = distribution[i, 0];
                var remainingCount = distribution[i, 1];

                // Try to spawn with biome preferences
                remainingCount = SpawnSupplyWithBiomePreferences(
                    supplyId,
                    remainingCount,
                    supplyMap,
                    biomeMap,
                    biomeSupplies,
                    supplyTextureCache,
                    supplyCache,
                    mapWidth,
                    mapHeight,
                    maxAttempts,
                    random,
                    outSupplyList);

                // Spawn remaining supplies without biome preferences
                if (remainingCount > 0)
                {
                    SpawnRemainingSupplies(
                        supplyId,
                        remainingCount,
                        supplyMap,
                        supplyTextureCache,
                        supplyCache,
                        mapWidth,
                        mapHeight,
                        maxAttempts,
                        random,
                        outSupplyList);
                }
            }
        }
        private static int SpawnSupplyWithBiomePreferences(
            int supplyId,
            int count,
            int[,] supplyMap,
            int[,] biomeMap,
            List<BiomeSupply> biomeSupplies,
            Dictionary<int, SupplyTexture> supplyTextureCache,
            Dictionary<int, Supply> supplyCache,
            int mapWidth,
            int mapHeight,
            int maxAttempts,
            Random random,
            Dictionary<(int, int),int> outSupplyList)
        {
            var attempts = 0;
            var attemptsPerItem = 0;

            while (count > 0 && attempts < maxAttempts)
            {
                var position = MapUtils.GetRandomPosition(mapWidth, mapHeight, random);

                var biomeId = biomeMap[position.x, position.y];
                var biomeSupply = GetBiomeSupply(biomeSupplies, biomeId, supplyId);
                var supplyTexture = supplyTextureCache[biomeSupply.SupplyId];
                var supply = supplyCache[biomeSupply.SupplyId];

                if (biomeSupply != null)
                {
                    if (CanPlaceSupplyAtPosition(position.x, position.y, supplyMap, supply, supplyTexture))
                    {
                        if (ShouldSpawnBasedOnChance(biomeSupply.SpawnChance, attemptsPerItem, random))
                        {
                            supplyMap[position.x, position.y] = supplyId;
                            outSupplyList.Add((position.x, position.y), supplyId);
                            for (var i = position.x; i < position.x + supply.WidthCell; i++)
                            {
                                for (var j = position.y; j < position.y + supply.HeightCell; j++)
                                {
                                    if (i == position.x && j == position.y)
                                        continue;

                                    supplyMap[i, j] = 0;
                                }
                            }
                            count--;
                            attemptsPerItem = 0;
                        }
                        else
                        {
                            attemptsPerItem++;
                        }
                    }
                }
                attempts++;
            }
            return count;
        }

        private static void SpawnRemainingSupplies(
            int supplyId,
            int count,
            int[,] supplyMap,
            Dictionary<int, SupplyTexture> supplyTextureCache,
            Dictionary<int, Supply> supplyCache,
            int mapWidth,
            int mapHeight,
            int maxAttempts,
            Random random,
            Dictionary<(int, int), int> outSupplyList)
        {
            var attempts = 0;
            while (count > 0 && attempts < maxAttempts)
            {
                var position = MapUtils.GetRandomPosition(mapWidth, mapHeight, random);

                var supply = supplyCache[supplyMap[position.x, position.y]];
                var supplyTexture = supplyTextureCache[supplyMap[position.x, position.y]];

                if (CanPlaceSupplyAtPosition(position.x, position.y, supplyMap, supply, supplyTexture))
                {
                    supplyMap[position.x, position.y] = supplyId;
                    outSupplyList.Add((position.x, position.y), supplyId);

                    for (var i = position.x; i < position.x + supply.WidthCell; i++)
                    {
                        for (var j = position.y; j < position.y + supply.HeightCell; j++)
                        {
                            if (i == position.x && j == position.y)
                                continue;

                            supplyMap[i, j] = 0;
                        }
                    }
                    count--;
                }

                attempts++;
            }
        }

        private static bool CanPlaceSupplyAtPosition(int x, int y, int[,] supplyMap, Supply supply, SupplyTexture supplyTexture)
        {
            var mapWidth = supplyMap.GetLength(0);
            var mapHeight = supplyMap.GetLength(1);

            if (x < 0 || y < 0 ||
                x + supply.WidthCell > mapWidth ||
                y + supply.HeightCell > mapHeight)
            {
                return false;
            }

            if (x + supplyTexture.VisualWidthCell > mapWidth ||
                y + supplyTexture.VisualHeightCell > mapHeight)
            {
                return false;
            }

            for (var i = x; i < x + supply.WidthCell; i++)
            {
                for (var j = y; j < y + supply.HeightCell; j++)
                {
                    if (supplyMap[i, j] != -1)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static BiomeSupply GetBiomeSupply(
            List<BiomeSupply> biomeSupplies,
            int biomeId,
            int supplyId)
        {
            return biomeSupplies.FirstOrDefault(bs =>
                bs.BiomeId == biomeId && bs.SupplyId == supplyId);
        }
        
        private static bool ShouldSpawnBasedOnChance(float baseChance, int attemptNumber, Random random)
        {
            var adjustedChance = Math.Min(baseChance * (1 + attemptNumber * 0.1f), 1.0f);
            return random.NextDouble() <= adjustedChance;
        }

        private static int[,] CalculateSupplyDistribution(List<Supply> supplies, int totalSupplies)
        {
            var totalRatio = supplies.Sum(s => s.Ratio);
            var supplyDistribution = new int[supplies.Count, 2];

            for (var i = 0; i < supplies.Count; i++)
            {
                var supply = supplies[i];
                var supplyCount = (int)Math.Round((double)(totalSupplies * supply.Ratio) / totalRatio);

                supplyDistribution[i, 0] = supply.Id;
                supplyDistribution[i, 1] = supplyCount;
            }

            AdjustSupplyDistribution(supplies, supplyDistribution, totalSupplies);
            return supplyDistribution;
        }

        private static void AdjustSupplyDistribution(
            List<Supply> supplies,
            int[,] supplyDistribution,
            int totalSupplies)
        {
            var currentTotal = CalculateCurrentTotal(supplyDistribution);

            if (currentTotal != totalSupplies)
            {
                var diff = totalSupplies - currentTotal;
                var maxRatioIndex = FindMaxRatioIndex(supplies);
                supplyDistribution[maxRatioIndex, 1] += diff;
            }
        }

        private static int CalculateCurrentTotal(int[,] supplyDistribution)
        {
            var total = 0;
            for (var i = 0; i < supplyDistribution.GetLength(0); i++)
            {
                total += supplyDistribution[i, 1];
            }

            return total;
        }

        private static int FindMaxRatioIndex(List<Supply> supplies)
        {
            var maxRatioIndex = 0;
            var maxRatio = supplies[0].Ratio;

            for (var i = 1; i < supplies.Count; i++)
            {
                if (supplies[i].Ratio > maxRatio)
                {
                    maxRatio = supplies[i].Ratio;
                    maxRatioIndex = i;
                }
            }

            return maxRatioIndex;
        }
    }
}
