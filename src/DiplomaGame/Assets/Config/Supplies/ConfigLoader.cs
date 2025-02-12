using System.IO;
using System.Linq;
using Supplies;
using UnityEngine;

public static partial class ConfigLoader
{
	public static void LoadSuppliesConfig()
	{
		var json = File.ReadAllText(ConfigPaths.SuppliesConfigPath);
		var suppliesDto = JsonUtility.FromJson<SuppliesDto>(json);

		if (suppliesDto == null)
		{
			Debug.Log("Error supplies config");
			return;
		}

        var nonPositiveIds = suppliesDto.Supplies
            .Where(x => x.Id <= 0)
            .Select(x => x.Id)
            .ToList();
        if (nonPositiveIds.Any())
        {
            throw new System.Exception($"Supplies Id must be positive. Found non-positive IDs: {string.Join(", ", nonPositiveIds)}");
        }

        var hasDuplicates = suppliesDto.Supplies
			.GroupBy(x => x.Id)
			.Any(group => group.Count() > 1);

		var repeatedIds = suppliesDto.Supplies
            .GroupBy(x => x.Id)
			.Where(group => group.Count() > 1)
			.Select(group => group.Key)
			.ToList();

		if (hasDuplicates)
		{
			throw new System.Exception($"Supplies Id repeats: {repeatedIds}");
		}

        SuppliesConfig.SupplyPerBlocks = suppliesDto.SupplyPerBlocks;

        suppliesDto.Supplies.ForEach(x => SuppliesConfig.Supplies.Add(new Supply
		{
			Id = x.Id,
			Name = x.Name,
			Type = x.Type,
			Ratio = x.Ratio,
			Texture = x.Texture,
            HeightCell = x.HeightCell,
			WidthCell = x.WidthCell
		}));

        Debug.Log("Supplies config loaded");
    }
}
