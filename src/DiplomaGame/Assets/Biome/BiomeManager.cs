using System;
using UnityEngine;

namespace Biomes
{
	public static class BiomeManager
	{
		private static int[,] _biomeMap;

        public static void GetBiomeMap(int seed, Action<int[,]> onCompleted)
        {
            ThreadPoolManager.Instance.QueueJobWithResult(
                () => GenerateMap(seed),
                result => onCompleted?.Invoke(result)
            );
        }

        private static int[,] GenerateMap(int seed)
        {
			if (_biomeMap is null || _biomeMap.Length == 0)
			{
				var isBiomeMapValid = false;
				var generateCount = 0;

                while (!isBiomeMapValid)
				{
                    _biomeMap = BiomeMapGenerator.GenerateBiomeMap(
						mapWidth: MapConfig.MapWidth,
						mapHeight: MapConfig.MapHeight, 
						seed: seed,
                        biomeRange: BiomesConfig.BiomeRange,
						biomes: BiomesConfig.Biomes,
						noiseMult: BiomesConfig.NoiseMult,
						noiseDist: BiomesConfig.NoiseDist);

					isBiomeMapValid = BiomeMapValidator.ValidateBiomeMap(
						biomeMap: _biomeMap,
						biomes: BiomesConfig.Biomes,
						validationWeightPercentageDifference: BiomesConfig.ValidationWeightPercentageDifference);

                    seed = SeedRandom.GenerateNewSeed(seed);

                    generateCount++;
                    Debug.Log($"BiomeMap generated for {generateCount} times");

                }
            }
            return _biomeMap;
        }
	}
}

