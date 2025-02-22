
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class TilemapTexturesManager
{
    private static TilemapTexturesManager _instance;

    private static readonly object _lock = new();

    private List<Texture2D> _textureList;
    private Texture2D _combinedTexture;
    private Dictionary<int, UVCoords> _uvCoordsDictionary = new();
    private List<TilemapSprite> _tilemapSprites;

    private int _padding = 2;

    public static TilemapTexturesManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new TilemapTexturesManager();
                    }
                }
            }

            return _instance;
        }
    }

    private TilemapTexturesManager()
    {
        _textureList = TerrainTexturesConfig.TerrainTextures
            .Select(sprite => Path.Combine(TerrainTexturesConfig.TexturesPath, sprite.TextureFileName))
            .Select(LoadTexture).Where(texture => texture != null)
            .ToList();

        _combinedTexture = CreateCombinedTexture(_textureList, TerrainTexturesConfig.DefaultTextureSize);
        InitializeUVCoordsDictionary(_combinedTexture);
        InitializeTilemapSprites();
    }

    public List<Texture2D> GetTextures()
    {
        return _textureList;
    }

    public Texture2D GetCombinedTexture()
    {
        return _combinedTexture;
    }

    public Dictionary<int, UVCoords> GetUVCoordsDictionary()
    {
        return _uvCoordsDictionary;
    }

    public List<TilemapSprite> GeTilemapSprites()
    {
        return _tilemapSprites;
    }

    /// <summary>
    /// Loads a texture from a file specified by the given path.
    /// </summary>
    /// <param name="path">The file path to the texture image.</param>
    /// <returns>The loaded texture as a `Texture2D` object, or `null` if the file is not found.</returns>
    /// <remarks>
    /// This method reads the image file from disk and creates a `Texture2D` object using the image data. 
    /// If the file does not exist, it logs an error message and returns `null`.
    /// </remarks>
    private Texture2D LoadTexture(string path)
    {
        if (File.Exists(path))
        {
            var fileData = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
            return texture;
        }
        else
        {
            Debug.LogError("Texture file not found: " + path);
            return null;
        }
    }

    /// <summary>
    /// Creates a combined texture by arranging a list of textures side-by-side into a single texture.
    /// </summary>
    /// <param name="textures">A list of textures to combine.</param>
    /// <param name="textureSize">The size of each individual texture.</param>
    /// <returns>A new `Texture2D` object that contains all the input textures combined into one.</returns>
    /// <remarks>
    /// This method calculates the width required for the combined texture based on the number of input textures and their size.
    /// It then creates a new `Texture2D` and arranges each input texture in a horizontal strip within this combined texture.
    /// After setting the pixels for each texture, the combined texture is finalized with `Apply()` and returned.
    /// </remarks>
    private Texture2D CreateCombinedTexture(List<Texture2D> textures, int textureSize)
    {
        var combinedWidth = textures.Count * textureSize + (textures.Count - 1) * _padding;
        var combinedTexture = new Texture2D(combinedWidth, textureSize);

        var clearPixels = new Color[combinedWidth * textureSize];
        for (var i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = Color.clear;
        combinedTexture.SetPixels(0, 0, combinedWidth, textureSize, clearPixels);

        for (var i = 0; i < textures.Count; i++)
        {
            var x = i * (textureSize + _padding);
            var texture = textures[i];
            var pixels = texture.GetPixels();
            combinedTexture.SetPixels(x, 0, textureSize, textureSize, pixels);
        }

        combinedTexture.filterMode = FilterMode.Point;
        combinedTexture.wrapMode = TextureWrapMode.Clamp;
        combinedTexture.Apply();
        return combinedTexture;
    }

    /// <summary>
    /// Initializes the UV coordinate dictionary for the terrain textures based on the combined texture.
    /// </summary>
    /// <param name="combinedTexture">The combined texture containing all terrain textures arranged side-by-side.</param>
    /// <remarks>
    /// This method clears the existing UV coordinate dictionary and populates it with new UV coordinates calculated 
    /// from the positions of each texture in the combined texture. It maps each texture ID to its UV coordinates for 
    /// accurate texture mapping.
    /// </remarks>
    private void InitializeUVCoordsDictionary(Texture2D combinedTexture)
    {
        _uvCoordsDictionary.Clear();
        var terrainTextures = TerrainTexturesConfig.TerrainTextures;
        var resolution = terrainTextures[0].TextureResolution;

        for (var i = 0; i < terrainTextures.Count; i++)
        {
            var sprite = terrainTextures[i];
            var id = sprite.Id;

            var x = i * (resolution + _padding);

            var uv00 = new Vector2((x + 0.5f) / (float)combinedTexture.width, 0.5f / resolution);
            var uv11 = new Vector2((x + resolution - 0.5f) / (float)combinedTexture.width, 1 - 0.5f / resolution);

            _uvCoordsDictionary[id] = new UVCoords
            {
                uv00 = uv00,
                uv11 = uv11
            };
        }
    }

    /// <summary>
    /// Initializes the list of tilemap sprites using the terrain textures configuration.
    /// </summary>
    /// <remarks>
    /// This method creates a new list of `TilemapSprite` objects from the terrain textures configuration.
    /// Each `TilemapSprite` is initialized with the ID and Name of the corresponding terrain texture.
    /// </remarks>
    private void InitializeTilemapSprites()
    {
        var terrainTextures = TerrainTexturesConfig.TerrainTextures;

        _tilemapSprites = new List<TilemapSprite>();
        terrainTextures.ForEach(x => _tilemapSprites.Add(new TilemapSprite(x.Id)));
    }
}
