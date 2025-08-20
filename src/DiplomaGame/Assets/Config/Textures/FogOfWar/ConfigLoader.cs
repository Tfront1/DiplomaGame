using System.IO;
using System.Linq;
using FogOfWar;
using UnityEngine;

public static partial class ConfigLoader
{
    public static void LoadForOfWarTexturesConfig()
    {
        var json = File.ReadAllText(ConfigPaths.FogOfWarTexturesPath);
        var fogTexturesConfigDto = JsonUtility.FromJson<FogOfWarTexturesDto>(json);

        if (fogTexturesConfigDto == null)
        {
            Debug.Log("Error Fog textures config");
            return;
        }

        var nonPositiveIds = fogTexturesConfigDto.Clouds
            .Where(x => x.Id <= 0)
            .Select(x => x.Id)
            .ToList();
        if (nonPositiveIds.Any())
        {
            throw new System.Exception($"Fog (clouds) textures Id`s must be positive. Found non-positive IDs: {string.Join(", ", nonPositiveIds)}");
        }

        var hasDuplicates = fogTexturesConfigDto.Clouds
            .GroupBy(x => x.Id)
            .Any(group => group.Count() > 1);

        var repeatedIds = fogTexturesConfigDto.Clouds
            .GroupBy(x => x.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (hasDuplicates)
        {
            throw new System.Exception($"Fog (clouds) textures Id`s repeats: {repeatedIds}");
        }

        FogOfWarTexturesConfig.TexturesFolderPath = fogTexturesConfigDto.FolderPath;
        FogOfWarTexturesConfig.BackgroundTexture = new FogOfWarTexture
        {
            Id = fogTexturesConfigDto.Background.Id,
            FileName = fogTexturesConfigDto.Background.FileName
        };

        fogTexturesConfigDto.Clouds.ForEach(texture => FogOfWarTexturesConfig.CloutTextures.Add(new FogOfWarTexture
        {
            Id = texture.Id,
            FileName = texture.FileName
        }));

        Debug.Log("Fog textures config loaded");
    }
}
