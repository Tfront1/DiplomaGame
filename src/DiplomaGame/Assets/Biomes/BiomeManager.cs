namespace Biomes
{
	public static class BiomeManager
	{
		private static int[,] _biomeMap;

		public static int[,] GetBiomeMap()
		{
			if (_biomeMap is null || _biomeMap.Length == 0)
			{
				bool isBiomeMapValid = false;
				while (!isBiomeMapValid)
				{
					_biomeMap = BiomeMapGenerator.GenerateBiomeMap(
						mapWidth: MapConfig.MapWidth,
						mapHeight: MapConfig.MapHeight,
						seed: new System.Random().Next(1000, 100000),
						biomeRange: BiomesConfig.BiomeRange,
						biomes: BiomesConfig.Biomes,
						noiseMult: BiomesConfig.NoiseMult,
						noiseDist: BiomesConfig.NoiseDist);

					isBiomeMapValid = BiomeMapValidator.ValidateBiomeMap(
						biomeMap: _biomeMap,
						biomes: BiomesConfig.Biomes,
						validationWeightPercentageDifference: BiomesConfig.ValidationWeightPercentageDifference);	
				}
			}

			return _biomeMap;
		}
	}
}

