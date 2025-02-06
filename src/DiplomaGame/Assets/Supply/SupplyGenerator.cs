using System;
using System.Collections.Generic;
using System.Linq;

namespace Supplies
{
    internal static class SupplyGenerator
    {
        internal static int[,] GenerateSupply(
            int mapWidth,
            int mapHeight,
            int seed,
            List<Supply> supplies,
            List<BiomeSupply> biomeSupplies,
            int SupplyPerBlocks,
            int[,] map)
        {
            var random = new Random(seed);
            var totalSupplies = mapHeight * mapWidth / SupplyPerBlocks;
            var distribution = CalculateSupplyDistribution(supplies, totalSupplies);

            var supplyMap = InitializeSupplyMap(mapWidth, mapHeight);

            SpawnSuppliesOnMap(
                distribution,
                supplyMap,
                map,
                biomeSupplies,
                mapWidth,
                mapHeight,
                random);

            return supplyMap;
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
            int mapWidth,
            int mapHeight,
            Random random)
        {
            var maxAttempts = mapWidth * mapHeight;

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
                    mapWidth,
                    mapHeight,
                    maxAttempts,
                    random);

                // Spawn remaining supplies without biome preferences
                if (remainingCount > 0)
                {
                    SpawnRemainingSupplies(
                        supplyId,
                        remainingCount,
                        supplyMap,
                        mapWidth,
                        mapHeight,
                        maxAttempts,
                        random);
                }
            }
        }
        private static int SpawnSupplyWithBiomePreferences(
            int supplyId,
            int count,
            int[,] supplyMap,
            int[,] biomeMap,
            List<BiomeSupply> biomeSupplies,
            int mapWidth,
            int mapHeight,
            int maxAttempts,
            Random random)
        {
            var attempts = 0;
            var attemptsPerItem = 0;

            while (count > 0 && attempts < maxAttempts)
            {
                var position = GetRandomPosition(mapWidth, mapHeight, random);

                if (CanPlaceSupplyAtPosition(position.x, position.y, supplyMap))
                {
                    var biomeId = biomeMap[position.x, position.y];
                    var biomeSupply = GetBiomeSupply(biomeSupplies, biomeId, supplyId);

                    if (biomeSupply != null && ShouldSpawnBasedOnChance(biomeSupply.SpawnChance, attemptsPerItem, random))
                    {
                        supplyMap[position.x, position.y] = supplyId;
                        count--;
                        attemptsPerItem = 0;
                    }
                    else
                    {
                        attemptsPerItem++;
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
            int mapWidth,
            int mapHeight,
            int maxAttempts,
            Random random)
        {
            var attempts = 0;
            while (count > 0 && attempts < maxAttempts)
            {
                var position = GetRandomPosition(mapWidth, mapHeight, random);

                if (CanPlaceSupplyAtPosition(position.x, position.y, supplyMap))
                {
                    supplyMap[position.x, position.y] = supplyId;
                    count--;
                }

                attempts++;
            }
        }

        private static (int x, int y) GetRandomPosition(int width, int height, Random random)
        {
            return (random.Next(width), random.Next(height));
        }

        private static bool CanPlaceSupplyAtPosition(int x, int y, int[,] supplyMap)
        {
            return supplyMap[x, y] == -1;
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
