using System;
using System.IO;
using System.Linq;
using Assets.Items.Armor;
using UnityEngine;

public static partial class ConfigLoader
{
    public static void LoadArmorsConfig()
    {
        var json = File.ReadAllText(ConfigPaths.ArmorsConfig);
        var armorsDto = JsonUtility.FromJson<ArmorsDto>(json);

        if (armorsDto == null)
        {
            Debug.Log("Error armors config");
            return;
        }

        var nonPositiveIds = armorsDto.Armors
            .Where(x => x.Id <= 0)
            .Select(x => x.Id)
            .ToList();

        if (nonPositiveIds.Any())
        {
            throw new Exception($"Armors Id must be positive. Found non-positive IDs: {string.Join(", ", nonPositiveIds)}");
        }

        var hasDuplicates = armorsDto.Armors
            .GroupBy(x => x.Id)
            .Any(group => group.Count() > 1);

        var repeatedIds = armorsDto.Armors
            .GroupBy(x => x.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (hasDuplicates)
        {
            throw new Exception($"Armors Id repeats: {repeatedIds}");
        }

        armorsDto.Armors.ForEach(x => ArmorConfig.ArmorElements.Add(new ArmorElement()
        {
            Id = x.Id,
            Name = x.Name,
            ArmorResistance = x.ArmorResistance,
        }));

        Debug.Log("Armors config loaded");
    }
}
