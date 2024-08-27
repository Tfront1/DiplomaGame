using System.Collections.Generic;

namespace Biomes
{
	internal static class BiomeMapGenerator
	{
		internal static int[,] GenerateBiomeMap(
			int mapWidth,
			int mapHeight,
			int seed,
			int biomeRange,
			List<Biome> biomes,
			float noiseMult,
			float noiseDist)
		{
			return Noise.GenerateBiomeMap(
				mapWidth,
				mapHeight,
				seed,
				biomeRange,
				biomes,
				noiseMult, 
				noiseDist);
		}
	}
}