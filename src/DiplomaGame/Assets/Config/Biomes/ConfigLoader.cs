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

        var nonPositiveIds = biomesDto.Biomes
            .Where(x => x.Id <= 0)
            .Select(x => x.Id)
            .ToList();
        if (nonPositiveIds.Any())
        {
            throw new System.Exception($"Biomes Id must be positive. Found non-positive IDs: {string.Join(", ", nonPositiveIds)}");
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

        Debug.Log("Biomes config loaded");
    }
}
