using System.IO;
using UnityEngine;

public static class Noise
{
	public static int[,] GenerateBiomeMap(int mapWidth, int mapHeight, int seed, int biomeRange, BiomeType[] biomes, float noiseMult, float noiseDist)
	{
		var biomesNum = biomes.Length;
		var biomesMap = new int[mapWidth, mapHeight];

		var prng = new System.Random(seed);
		SeedRandom.SetSeed(seed);

		var xS = prng.Next(10, 20);
		var yS = prng.Next(10, 20);

		float offsetX = prng.Next(-100000, 100000);
		float offsetY = prng.Next(-100000, 100000);
        
		// Створюємо масив для зберігання накопичуваних ваг
		var cumulativeWeights = new float[biomesNum];
		var totalWeight = 0f;

		// Обчислюємо накопичувані ваги
		for (var i = 0; i < biomesNum; i++)
		{
			totalWeight += biomes[i]._weight;
			cumulativeWeights[i] = totalWeight;
		}

		for (var x = 0; x < mapWidth; x++)
		{
			for (var y = 0; y < mapHeight; y++)
			{
				var gridX = (int)Mathf.Floor(x / biomeRange);
				var gridY = (int)Mathf.Floor(y / biomeRange);

				if (x / biomeRange - gridX > 0.5f)
					gridX -= 2;
				else
					gridX -= 1;

				if (y / biomeRange - gridY > 0.5f)
					gridY -= 2;
				else
					gridY -= 1;

				var closest = 0;
				var closestDist = int.MaxValue;
				for (var i = 0; i < 4; i++)
				{
					for (var j = 0; j < 4; j++)
					{
						var curBiome = i * 4 + j;
						var biomeX = SeedRandom.Get(gridX + i, gridY + j) % biomeRange;
						var biomeY = SeedRandom.Get(gridX + i, gridY + j) % biomeRange;

						var dist = ((gridX + i) * biomeRange + biomeX - x) * ((gridX + i) * biomeRange + biomeX - x) +
                                   ((gridY + j) * biomeRange + biomeY - y) * ((gridY + j) * biomeRange + biomeY - y);

						dist += (int)(Mathf.PerlinNoise(noiseDist * ((gridX + i) * biomeRange + biomeX - x + offsetX) / 100f,
														 noiseDist * ((gridY + j) * biomeRange + biomeY - y + offsetY) / 100f) * noiseMult);

						if (dist < closestDist)
						{
							closestDist = dist;
							closest = curBiome;
						}
					}
				}

				// Визначаємо біом, використовуючи накопичувані ваги
				var randomValue = (SeedRandom.Get(gridX + closest / 4, gridY + closest % 4) % 1000) / 1000f * totalWeight;
				for (var i = 0; i < cumulativeWeights.Length; i++)
				{
					if (randomValue <= cumulativeWeights[i])
					{
						biomesMap[x, y] = biomes[i]._biomeId;
						break;
					}
				}
			}
		}

        return biomesMap;
	}
} 

        

