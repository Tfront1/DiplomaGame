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

        var errors = ValidateSupplies(biomeSuppliesDto);
        if (errors != null)
        {
            throw new System.Exception(string.Join("\n", errors));
        }

        biomeSuppliesDto.BiomeSupplies.ForEach(x => BiomeSuppliesConfig.BiomeSupplies.Add(new BiomeSupply()
		{
			BiomeId = x.BiomeId,
			SupplyId = x.SupplyId,
			SpawnChance = x.SpawnChance
        }));

        Debug.Log("Biome supplies config loaded");
    }
    
    public static List<string> ValidateSupplies(BiomeSuppliesDto biomeSupplies)
    {
        var isValid = true;
        var errors = new List<string>();

        biomeSupplies.BiomeSupplies.ForEach(supply =>
        {
            if (!IsBiomeExists(supply.BiomeId))
            {
                isValid = false;
                errors.Add($"BiomeId {supply.BiomeId} does not exist in BiomesConfig");
            }

            if (supply.SpawnChance < 0 || supply.SpawnChance > 1)
            {
                isValid = false;
                errors.Add($"SpawnChance for BiomeId {supply.BiomeId} must be between 0 and 1");
            }
        });

        return !isValid ? errors : null;
    }

    private static bool IsBiomeExists(int biomeId)
    {
        return BiomesConfig.Biomes.Any(b => b.Id == biomeId);
    }
}
