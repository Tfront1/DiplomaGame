using System.IO;
using System.Linq;
using UnityEngine;

public static partial class ConfigLoader
{
    public static void LoadUnitsConfig()
    {
        var json = File.ReadAllText(ConfigPaths.UnitsConfigPath);
        var unitsDto = JsonUtility.FromJson<UnitsDto>(json);

        if (unitsDto == null)
        {
            Debug.Log("Error units config");
            return;
        }

        var nonPositiveIds = unitsDto.Units
            .Where(x => x.Id <= 0)
            .Select(x => x.Id)
            .ToList();
        if (nonPositiveIds.Any())
        {
            throw new System.Exception($"Units Id must be positive. Found non-positive IDs: {string.Join(", ", nonPositiveIds)}");
        }

        var hasDuplicates = unitsDto.Units
            .GroupBy(x => x.Id)
            .Any(group => group.Count() > 1);

        var repeatedIds = unitsDto.Units
            .GroupBy(x => x.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (hasDuplicates)
        {
            throw new System.Exception($"Units Id repeats: {repeatedIds}");
        }

        unitsDto.Units.ForEach(x => UnitsConfig.Units.Add(new Unit()
        {
            Id = x.Id,
            Name = x.Name,
            Texture = x.Texture,
            Size = new Vector2(x.SizeX, x.SizeY),
            Speed = x.Speed
        }));

        Debug.Log("Units config loaded");
    }
}