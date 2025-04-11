using System.IO;
using System.Linq;
using UnityEngine;

public static partial class ConfigLoader
{
	public static void LoadBuildingsConfig()
	{
		var json = File.ReadAllText(ConfigPaths.BuildingsConfigPath);
		var buildingsDto = JsonUtility.FromJson<BuildingsDto>(json);

		if (buildingsDto == null)
		{
			Debug.Log("Error buildings config");
			return;
		}

        var nonPositiveIds = buildingsDto.Buildings
            .Where(x => x.Id <= 0)
            .Select(x => x.Id)
            .ToList();
        if (nonPositiveIds.Any())
        {
            throw new System.Exception($"Buildings Id must be positive. Found non-positive IDs: {string.Join(", ", nonPositiveIds)}");
        }

        var hasDuplicates = buildingsDto.Buildings
			.GroupBy(x => x.Id)
			.Any(group => group.Count() > 1);

		var repeatedIds = buildingsDto.Buildings
			.GroupBy(x => x.Id)
			.Where(group => group.Count() > 1)
			.Select(group => group.Key)
			.ToList();

		if (hasDuplicates)
		{
			throw new System.Exception($"Buildings Id repeats: {repeatedIds}");
		}

		buildingsDto.Buildings.ForEach(x => BuildingsConfig.Buildings.Add(new Building
		{
			Id = x.Id,
			Name = x.Name,
            WidthCell = x.WidthCell,
			HeightCell = x.HeightCell,
			HasMargin = x.HasMargin,
			MaxHP = x.MaxHP,
			BuildingType = GetBuildingType(x.BuildingType),
            BackpackCapacity = x.BackpackCapacity,
			HasCrafts = x.HasCrafts,
			MaxResidents = x.MaxResidents
		}));

        Debug.Log("Buildings config loaded");
    }

    private static Building.BuildingTypes GetBuildingType(string type)
    {
        return type switch
        {
            "Construction" => Building.BuildingTypes.Construction,
            "TownHall" => Building.BuildingTypes.TownHall,
            "Fence" => Building.BuildingTypes.Fence,
            "Vault" => Building.BuildingTypes.Vault,
            "Blacksmith" => Building.BuildingTypes.Blacksmith,
            _ => Building.BuildingTypes.None
        };
    }
}
