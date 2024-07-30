using GameUtilities.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using File = UnityEngine.Windows.File;

public class TilemapData
{
    public List<TilemapSprite> _tilemapSprites;

    public Mesh _combinedMesh;

    public Dictionary<int, UVCoords> _uvCoordsDictionary = new();

    public Vector3[] _vertices;
    public Vector2[] _uv;
    public int[] _triangles;

    public int _width, _height;

    /// <summary>
    /// Initializes the mesh with specified dimensions and positions the GameObject.
    /// </summary>
    /// <param name="width">The width of the mesh in terms of number of tiles.</param>
    /// <param name="height">The height of the mesh in terms of number of tiles.</param>
    /// <param name="gameObjectPosition">The position to place the GameObject in the scene.</param>
    /// <remarks>
    /// This method configures the mesh and related resources based on the given width and height.
    /// It also positions the created GameObject at the specified position. The method includes:
    /// 1. Loading and combining textures.
    /// 2. Creating and assigning a material.
    /// 3. Generating an empty mesh and setting up its attributes.
    /// 4. Initializing UV coordinates and mesh arrays for vertices, UVs, and triangles.
    /// </remarks>
    public void InitialiseMesh(int width, int height, Vector3 gameObjectPosition)
    {
        _width = width;
        _height = height;

        var textures = TerrainTexturesConfig.TerrainTextures
            .Select(sprite => Path.Combine(TerrainTexturesConfig.TexturesPath, sprite.TextureFileName))
            .Select(LoadTexture).Where(texture => texture != null)
            .ToList();

        var combinedTexture = CreateCombinedTexture(textures, TerrainTexturesConfig.DefaultTextureSize);

        InitializeTilemapSprites();

        var material = CreateMaterialWithTexture(combinedTexture);

        var mesh = CreateMeshForCombinedTexture();

        var gameObject = CreateGameObjectWithMeshAndMaterial(mesh, material);
        gameObject.transform.position = gameObjectPosition;

        _combinedMesh = mesh;

        InitializeUVCoordsDictionary(combinedTexture);
        
        MeshUtils.CreateEmptyMeshArrays(width * height, out var vertices, out var uv, out var triangles);
        _vertices = vertices;
        _triangles = triangles;
        _uv = uv;

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
    public Texture2D LoadTexture(string path)
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
    public Texture2D CreateCombinedTexture(List<Texture2D> textures, int textureSize)
    {
        var combinedWidth = textures.Count * textureSize;
        var combinedTexture = new Texture2D(combinedWidth, textureSize);

        for (var i = 0; i < textures.Count; i++)
        {
            var x = i * textureSize;
            var y = 0;
            var texture = textures[i];

            var pixels = texture.GetPixels();
            combinedTexture.SetPixels(x, y, textureSize, textureSize, pixels);
        }

        combinedTexture.Apply();

        return combinedTexture;
    }

    /// <summary>
    /// Creates a material with a specified texture applied.
    /// </summary>
    /// <param name="texture">The texture to apply to the material.</param>
    /// <returns>A new `Material` object with the specified texture set as its main texture.</returns>
    /// <remarks>
    /// This method creates a `Material` using the "Unlit/Texture" shader and assigns the provided texture to the material's main texture slot.
    /// </remarks>

    public Material CreateMaterialWithTexture(Texture2D texture)
    {
        var material = new Material(Shader.Find("Unlit/Texture"));
        material.mainTexture = texture;
        return material;
    }

    /// <summary>
    /// Creates and returns a new, empty mesh.
    /// </summary>
    /// <returns>A new `Mesh` object with no geometry or attributes set.</returns>
    public Mesh CreateMeshForCombinedTexture()
    {
        var mesh = new Mesh();

        return mesh;
    }

    /// <summary>
    /// Creates a new GameObject with a mesh and material applied.
    /// </summary>
    /// <param name="mesh">The mesh to assign to the GameObject's MeshFilter component.</param>
    /// <param name="material">The material to assign to the GameObject's MeshRenderer component.</param>
    /// <returns>The newly created `GameObject` with the specified mesh and material.</returns>
    public GameObject CreateGameObjectWithMeshAndMaterial(Mesh mesh, Material material)
    {
        var gameObject = new GameObject("TerrainMesh");

        var meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;

        return gameObject;
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

    public void InitializeUVCoordsDictionary(Texture2D combinedTexture)
    {
        _uvCoordsDictionary.Clear();
        var terrainTextures = TerrainTexturesConfig.TerrainTextures;

        foreach (var sprite in terrainTextures)
        {
            var id = sprite.Id;
            var resolution = sprite.TextureResolution;

            var x = id * resolution;
            var uv00 = new Vector2(x / (float)combinedTexture.width, 0);
            var uv11 = new Vector2((x + resolution) / (float)combinedTexture.width, 1);


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
    public void InitializeTilemapSprites()
    {
        var terrainTextures = TerrainTexturesConfig.TerrainTextures;

        _tilemapSprites = new List<TilemapSprite>();
        terrainTextures.ForEach(x => _tilemapSprites.Add(new TilemapSprite(x.Id, x.Name)));
    }
}

