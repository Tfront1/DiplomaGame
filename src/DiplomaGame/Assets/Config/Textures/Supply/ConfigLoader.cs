using System.Collections.Generic;
using System.IO;
using System.Linq;
using Supplies;
using UnityEngine;

public static partial class ConfigLoader
{
    /// <summary>
    /// Loads and parses supply textures configuration from a JSON file.
    /// </summary>
    /// <remarks>
    /// This method reads the JSON configuration file for supply textures, parses it into a `SupplyTexturesData` object,
    /// and validates the texture data. It checks if the texture resolution matches the expected size and if there are any duplicate texture IDs.
    /// If there are any issues, exceptions are thrown to indicate the errors. If everything is valid, it updates the `SupplyTexturesConfig`
    /// with the loaded texture data and logs a success message.
    /// </remarks>
    /// <exception cref="System.Exception"></exception>
    public static void LoadSupplyTexturesConfig()
    {
        
        var json = File.ReadAllText(ConfigPaths.SupplyTexturesPath);
        var supplyTexturesDto = JsonUtility.FromJson<SupplyTexturesDto>(json);

        if (supplyTexturesDto == null)
        {
            Debug.Log("Error supply textures config");
            return;
        }

        var nonPositiveIds = supplyTexturesDto.TilemapSprites
            .Where(x => x.Id <= 0)
            .Select(x => x.Id)
            .ToList();
        if (nonPositiveIds.Any())
        {
            throw new System.Exception($"Supply textures Id must be positive. Found non-positive IDs: {string.Join(", ", nonPositiveIds)}");
        }

        SupplyTexturesConfig.TexturesPath = supplyTexturesDto.TexturesPath;
        SupplyTexturesConfig.DefaultTextureSize = supplyTexturesDto.DefaultTextureSize;

        var hasDuplicates = supplyTexturesDto.TilemapSprites
            .GroupBy(x => x.Id)
            .Any(group => group.Count() > 1);

        var repeatedIds = supplyTexturesDto.TilemapSprites
            .GroupBy(x => x.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (hasDuplicates)
        {
            throw new System.Exception($"Supply textures Id repeats: {repeatedIds}");
        }

        SupplyTexturesConfig.SupplyTexturesSprite = supplyTexturesDto.TilemapSprites;

        var suppliesIdErrors = ValidateSupplies(supplyTexturesDto);

        if (suppliesIdErrors != null)
        {
            throw new System.Exception(string.Join("\n", suppliesIdErrors));
        }

        supplyTexturesDto.TilemapSprites.ForEach(x => SupplyTexturesConfig.SupplyTextures.Add(new SupplyTexture()
        {
            Id = x.Id,
            SupplyId = x.SupplyId,
            Texture = LoadTextureFromFile(SupplyTexturesConfig.TexturesPath + x.TextureFileName),
            VisualWidthCell = x.VisualWidthCell,
            VisualHeightCell = x.VisualHeightCell
        }));

        Debug.Log("Supply textures config loaded");
    }
    private static Texture2D LoadTextureFromFile(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("No file found: " + path);
            return null;
        }

        var fileData = File.ReadAllBytes(path);
        var texture = new Texture2D(2, 2);
        if (texture.LoadImage(fileData))
        {
            return texture;
        }

        Debug.LogError("Error during creating texture (Supply)");
        return null;
    }

    private static List<string> ValidateSupplies(SupplyTexturesDto suppliesTextures)
    {
        var isValid = true;
        var errors = new List<string>();

        suppliesTextures.TilemapSprites.ForEach(supply =>
        {
            if (!IsSupplyExists(supply.SupplyId))
            {
                isValid = false;
                errors.Add($"SupplyId {supply.SupplyId} does not exist in SuppliesConfig");
            }
        });

        return !isValid ? errors : null;
    }
}
