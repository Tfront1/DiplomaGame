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

        SupplyTexturesConfig.TexturesPath = supplyTexturesDto.TexturesPath;
        SupplyTexturesConfig.DefaultTextureSize = supplyTexturesDto.DefaultTextureSize;

        if (supplyTexturesDto.TilemapSprites.Any(x => x.TextureResolution != SupplyTexturesConfig.DefaultTextureSize))
        {
            throw new System.Exception($"Wrong texture resolution. Expected {SupplyTexturesConfig.DefaultTextureSize}X{SupplyTexturesConfig.DefaultTextureSize}");
        }

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
        supplyTexturesDto.TilemapSprites.ForEach(x => SupplyTexturesConfig.SupplyTextures.Add(new SupplyTexture()
        {
            Id = x.Id,
            Texture = LoadTextureFromFile(SupplyTexturesConfig.TexturesPath + x.TextureFileName)
        }));

        Debug.Log("Supply textures config loaded and mesh created");
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
}
