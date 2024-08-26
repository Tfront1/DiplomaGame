namespace Biomes
{
	public static class BiomeManager
	{
		private static int[,] _biomeMap;

		public static int[,] GetBiomeMap(int seed)
		{
			if (_biomeMap is null || _biomeMap.Length == 0)
			{
				var isBiomeMapValid = false;

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

                }
            }
            return _biomeMap;
		}
	}
}

