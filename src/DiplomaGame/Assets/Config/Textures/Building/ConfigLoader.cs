using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static partial class ConfigLoader
{
    public static void LoadBuildingTexturesConfig()
    {

        var json = File.ReadAllText(ConfigPaths.BuildingTexturesPath);
        var buildingTexturesDto = JsonUtility.FromJson<BuildingTexturesDto>(json);

        if (buildingTexturesDto == null)
        {
            Debug.Log("Error building textures config");
            return;
        }

        var nonPositiveIds = buildingTexturesDto.TilemapSprites
            .Where(x => x.Id <= 0)
            .Select(x => x.Id)
            .ToList();

        if (nonPositiveIds.Any())
        {
            throw new System.Exception($"Building textures Id must be positive. Found non-positive IDs: {string.Join(", ", nonPositiveIds)}");
        }

        BuildingTexturesConfig.TexturesPath = buildingTexturesDto.TexturesPath;
        BuildingTexturesConfig.DefaultTextureSize = buildingTexturesDto.DefaultTextureSize;

        var hasDuplicates = buildingTexturesDto.TilemapSprites
            .GroupBy(x => x.Id)
            .Any(group => group.Count() > 1);

        var repeatedIds = buildingTexturesDto.TilemapSprites
            .GroupBy(x => x.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (hasDuplicates)
        {
            throw new System.Exception($"Building textures Id repeats: {repeatedIds}");
        }

        var buildingsIdErrors = ValidateBuildings(buildingTexturesDto);

        if (buildingsIdErrors != null)
        {
            throw new System.Exception(string.Join("\n", buildingsIdErrors));
        }

        buildingTexturesDto.TilemapSprites.ForEach(x => BuildingTexturesConfig.BuildingTexture.Add(new BuildingTexture()
        {
            Id = x.Id,
            BuildingId = x.BuildingId,
            Texture = LoadTextureFromFile(BuildingTexturesConfig.TexturesPath + x.TextureFileName),
            VisualWidthCell = x.VisualWidthCell,
            VisualHeightCell = x.VisualHeightCell,
            RandomPos = x.RandomPos,
            Scale = x.Scale
        }));

        Debug.Log("Building textures config loaded");
    }

    private static List<string> ValidateBuildings(BuildingTexturesDto buildingsTextures)
    {
        var isValid = true;
        var errors = new List<string>();

        buildingsTextures.TilemapSprites.ForEach(building =>
        {
            if (!IsBuildingExists(building.BuildingId))
            {
                isValid = false;
                errors.Add($"BuildingId {building.BuildingId} does not exist in BuildingsConfig");
            }
        });

        return !isValid ? errors : null;
    }

    private static bool IsBuildingExists(int buildingId)
    {
        return BuildingsConfig.Buildings.Any(b => b.Id == buildingId);
    }
}
