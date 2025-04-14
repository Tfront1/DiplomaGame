using System.IO;
using System.Linq;
using Items.Resource;
using UnityEngine;

public static partial class ConfigLoader
{
    public static void LoadResourceItemsConfig()
    {
        var json = File.ReadAllText(ConfigPaths.ResourceItemsConfigPath);
        var resourcesDto = JsonUtility.FromJson<ResourcesDto>(json);
        
        if (resourcesDto == null)
        {
            Debug.Log("Error resources items config");
            return;
        }

        var nonPositiveIds = resourcesDto.Resources
            .Where(x => x.Id <= 0)
            .Select(x => x.Id)
            .ToList();
        if (nonPositiveIds.Any())
        {
            throw new System.Exception($"Resource items Id must be positive. Found non-positive IDs: {string.Join(", ", nonPositiveIds)}");
        }

        var hasDuplicates = resourcesDto.Resources
            .GroupBy(x => x.Id)
            .Any(group => group.Count() > 1);

        var repeatedIds = resourcesDto.Resources
            .GroupBy(x => x.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (hasDuplicates)
        {
            throw new System.Exception($"Resource items Id repeats: {repeatedIds}");
        }

        resourcesDto.Resources.ForEach(x => ResourcesConfig.ResourceElements.Add(new ResourceElement
        {
            Id = x.Id,
            Name = x.Name,
        }));

        Debug.Log("Resource items config loaded");
    }
}