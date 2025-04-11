using System.IO;
using System.Linq;
using UnityEngine;

public static partial class ConfigLoader
{
    public static void LoadUnitTexturesConfig()
    {
        var json = File.ReadAllText(ConfigPaths.UnitsTexturesPath);
        var unitTexturesCollectionDto = JsonUtility.FromJson<UnitTexturesCollectionDto>(json);

        if (unitTexturesCollectionDto == null)
        {
            Debug.Log("Error unit textures config");
            return;
        }

        var nonPositiveUnitIds = unitTexturesCollectionDto.UnitsGroups
            .Where(unit => unit.UnitId < 0)
            .Select(unit => unit.UnitId)
            .ToList();

        if (nonPositiveUnitIds.Count > 0)
        {
            Debug.Log($"Unit IDs must be positive. Found non-positive IDs: {string.Join(", ", nonPositiveUnitIds)}");
        }

        var duplicateUnitIds = unitTexturesCollectionDto.UnitsGroups
            .GroupBy(unit => unit.UnitId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateUnitIds.Count > 0)
        {
            Debug.Log($"Duplicate Unit IDs found: {string.Join(", ", duplicateUnitIds)}");
        }

        foreach (var unit in unitTexturesCollectionDto.UnitsGroups)
        {

            var nonPositiveActionIds = unit.ActionGroups
                .Where(action => action.ActionId < 0)
                .Select(action => action.ActionId)
                .ToList();

            if (nonPositiveActionIds.Count > 0)
            {
                Debug.Log($"Action IDs must be positive for Unit ID {unit.UnitId}. Found non-positive IDs: {string.Join(", ", nonPositiveActionIds)}");
            }

            var duplicateActionIds = unit.ActionGroups
                .GroupBy(action => action.ActionId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateActionIds.Count > 0)
            {
                Debug.Log($"Duplicate Action IDs found for Unit ID {unit.UnitId}: {string.Join(", ", duplicateActionIds)}");
            }

            foreach (var action in unit.ActionGroups)
            {
                var nonPositiveTextureIds = action.Textures
                    .Where(texture => texture.Id <= 0)
                    .Select(texture => texture.Id)
                    .ToList();

                if (nonPositiveTextureIds.Count > 0)
                {
                    Debug.Log($"Texture IDs must be positive for Unit ID {unit.UnitId}, Action ID {action.ActionId}. Found non-positive IDs: {string.Join(", ", nonPositiveTextureIds)}");
                }

                var duplicateTextureIds = action.Textures
                    .GroupBy(texture => texture.Id)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateTextureIds.Count > 0)
                {
                    Debug.Log($"Duplicate Texture IDs found for Unit ID {unit.UnitId}, Action ID {action.ActionId}: {string.Join(", ", duplicateTextureIds)}");
                }
            }
        }

        UnitsTexturesConfig.TexturesPath = unitTexturesCollectionDto.TexturesPath;
        UnitsTexturesConfig.UnitsGroupsList = unitTexturesCollectionDto.UnitsGroups
            .Select(unitGroup => new UnitsTexturesConfig.UnitsGroup
            {
                UnitId = unitGroup.UnitId,
                UnitFolder = unitGroup.UnitFolder,
                ActionGroupsList = unitGroup.ActionGroups.Select(actionGroup => new UnitsTexturesConfig.ActionGroup
                {
                    ActionFolder = actionGroup.ActionFolder,
                    ActionId = actionGroup.ActionId,
                    ActionName = actionGroup.ActionName,
                    UnitTexturesList = actionGroup.Textures.Select(texture => new UnitsTexturesConfig.UnitTexture
                    {
                        Id = texture.Id,
                        TextureFileName = texture.TextureFileName
                    }).ToList()
                }).ToList()
            }).ToList();

        Debug.Log("Unit textures config loaded");
    }
}
