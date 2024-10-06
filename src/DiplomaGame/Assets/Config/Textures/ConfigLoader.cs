using System.IO;
using System.Linq;
using UnityEngine;

public static partial class ConfigLoader
{
	/// <summary>
	/// Loads and parses terrain textures configuration from a JSON file.
	/// </summary>
	/// <remarks>
	/// This method reads the JSON configuration file for terrain textures, parses it into a `TerrainTexturesData` object,
	/// and validates the texture data. It checks if the texture resolution matches the expected size and if there are any duplicate texture IDs.
	/// If there are any issues, exceptions are thrown to indicate the errors. If everything is valid, it updates the `TerrainTexturesConfig`
	/// with the loaded texture data and logs a success message.
	/// </remarks>
	/// <exception cref="System.Exception"></exception>
	public static void LoadTerrainTexturesConfig()
	{

		var json = File.ReadAllText(ConfigPaths.TerrainTexturesPath);
		var terrainTexturesDto = JsonUtility.FromJson<TerrainTexturesDto>(json);

		if (terrainTexturesDto == null)
		{
			Debug.Log("Error terrain textures config");
			return;
		}

		TerrainTexturesConfig.TexturesPath = terrainTexturesDto.TexturesPath;
		TerrainTexturesConfig.DefaultTextureSize = terrainTexturesDto.DefaultTextureSize;

		if (terrainTexturesDto.TilemapSprites.Any(x => x.TextureResolution != TerrainTexturesConfig.DefaultTextureSize))
		{
			throw new System.Exception($"Wrong texture resolution. Expected {TerrainTexturesConfig.DefaultTextureSize}X{TerrainTexturesConfig.DefaultTextureSize}");
		}

		var hasDuplicates = terrainTexturesDto.TilemapSprites
			.GroupBy(x => x.Id)
			.Any(group => group.Count() > 1);

		var repeatedIds = terrainTexturesDto.TilemapSprites
			.GroupBy(x => x.Id)
			.Where(group => group.Count() > 1)
			.Select(group => group.Key)
			.ToList();

		if (hasDuplicates)
		{
			throw new System.Exception($"Textures Id repeats: {repeatedIds}");
		}


		TerrainTexturesConfig.TerrainTextures = terrainTexturesDto.TilemapSprites;

		Debug.Log("Terrain textures config loaded and mesh created");
	}
}
