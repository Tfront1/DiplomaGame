
using System;
using System.Collections.Generic;
using System.Linq;

namespace Biomes
{
	internal static class BiomeMapValidator
	{
		internal static bool ValidateBiomeMap(
			int[,] biomeMap,
			List<Biome> biomes,
			int validationWeightPercentageDifference) 
		{
			int totalElements = biomeMap.Length;

			var biomeCounts = new Dictionary<int, int>();

			foreach (var biome in biomes)
			{
				biomeCounts[biome.Id] = 0;
			}

			for (int i = 0; i < biomeMap.GetLength(0); i++)
			{
				for (int j = 0; j < biomeMap.GetLength(1); j++)
				{
					int biomeId = biomeMap[i, j];
					if (biomeCounts.ContainsKey(biomeId))
					{
						biomeCounts[biomeId]++;
					}
				}
			}

			foreach (var biome in biomes)
			{
				int biomeId = biome.Id;
				double actualPercentage = (double)biomeCounts[biomeId] / totalElements * 100;
				double expectedPercentage = (double)biome.Weight / biomes.Sum(b => b.Weight) * 100;
				double percentageDifference = Math.Abs(actualPercentage - expectedPercentage);

				if (percentageDifference > validationWeightPercentageDifference)
				{
					return false;
				}
			}

			return true;
		}
	}
}

