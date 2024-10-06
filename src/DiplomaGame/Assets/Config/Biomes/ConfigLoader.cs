using Biomes;
using System.IO;
using System.Linq;
using UnityEngine;

public static partial class ConfigLoader
{
	public static void LoadBiomesConfig()
	{
		var json = File.ReadAllText(ConfigPaths.BiomesConfigPath);
		var biomesDto = JsonUtility.FromJson<BiomesDto>(json);

		if (biomesDto == null)
		{
			Debug.Log("Error biomes config");
			return;
		}

		if (biomesDto.ValidationWeightPercentageDifference < 0 ||
			biomesDto.ValidationWeightPercentageDifference > 100)
		{
			throw new System.Exception($"Invalid ValidationWeightPercentageDifference: " +
				$"{biomesDto.ValidationWeightPercentageDifference}");
		}

		var hasDuplicates = biomesDto.Biomes
			.GroupBy(x => x.Id)
			.Any(group => group.Count() > 1);

		var repeatedIds = biomesDto.Biomes
			.GroupBy(x => x.Id)
			.Where(group => group.Count() > 1)
			.Select(group => group.Key)
			.ToList();

		if (hasDuplicates)
		{
			throw new System.Exception($"Biomes Id repeats: {repeatedIds}");
		}


		BiomesConfig.NoiseMult = biomesDto.NoiseMult;
		BiomesConfig.NoiseDist = biomesDto.NoiseDist;
		BiomesConfig.BiomeRange = biomesDto.BiomeRange;
		BiomesConfig.ValidationWeightPercentageDifference = biomesDto.ValidationWeightPercentageDifference;

		biomesDto.Biomes.ForEach(x => BiomesConfig.Biomes.Add(new Biome
		{
			Id = x.Id,
			Name = x.Name,
			Weight = x.Weight
		}));
	}
}
