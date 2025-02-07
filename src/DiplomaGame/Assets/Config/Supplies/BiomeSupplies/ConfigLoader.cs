using System.Collections.Generic;
using System.IO;
using System.Linq;
using Supplies;
using UnityEngine;

public static partial class ConfigLoader
{
    /// <summary>
    /// Should execute after loading Biome and Supply configs
    /// </summary>
    public static void LoadBiomeSuppliesConfig()
	{
		var json = File.ReadAllText(ConfigPaths.BiomeSuppliesConfigPath);
		var biomeSuppliesDto = JsonUtility.FromJson<BiomeSuppliesDto>(json);

		if (biomeSuppliesDto == null)
		{
			Debug.Log("Error biome supplies config");
			return;
		}

        var biomeIdErrors = ValidateBiomes(biomeSuppliesDto);
        if (biomeIdErrors != null)
        {
            throw new System.Exception(string.Join("\n", biomeIdErrors));
        }

        var supplyIdErrors = ValidateSupplies(biomeSuppliesDto);
        if (supplyIdErrors != null)
        {
            throw new System.Exception(string.Join("\n", supplyIdErrors));
        }

        biomeSuppliesDto.BiomeSupplies.ForEach(x => BiomeSuppliesConfig.BiomeSupplies.Add(new BiomeSupply()
		{
			BiomeId = x.BiomeId,
			SupplyId = x.SupplyId,
			SpawnChance = x.SpawnChance
        }));

        Debug.Log("Biome supplies config loaded");
    }

    private static List<string> ValidateBiomes(BiomeSuppliesDto biomeSupplies)
    {
        var isValid = true;
        var errors = new List<string>();

        biomeSupplies.BiomeSupplies.ForEach(biome =>
        {
            if (!IsBiomeExists(biome.BiomeId))
            {
                isValid = false;
                errors.Add($"BiomeId {biome.BiomeId} does not exist in BiomesConfig");
            }

            if (biome.SpawnChance < 0 || biome.SpawnChance > 1)
            {
                isValid = false;
                errors.Add($"SpawnChance for BiomeId {biome.BiomeId} must be between 0 and 1");
            }
        });

        return !isValid ? errors : null;
    }

    private static bool IsBiomeExists(int biomeId)
    {
        return BiomesConfig.Biomes.Any(b => b.Id == biomeId);
    }

    private static List<string> ValidateSupplies(BiomeSuppliesDto biomeSupplies)
    {
        var isValid = true;
        var errors = new List<string>();

        biomeSupplies.BiomeSupplies.ForEach(supply =>
        {
            if (!IsSupplyExists(supply.SupplyId))
            {
                isValid = false;
                errors.Add($"SupplyId {supply.SupplyId} does not exist in SuppliesConfig");
            }

            if (supply.SpawnChance < 0 || supply.SpawnChance > 1)
            {
                isValid = false;
                errors.Add($"SpawnChance for SupplyId {supply.SupplyId} must be between 0 and 1");
            }
        });

        return !isValid ? errors : null;
    }

    private static bool IsSupplyExists(int supplyId)
    {
        return SuppliesConfig.Supplies.Any(b => b.Id == supplyId);
    }
}
