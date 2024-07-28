using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using File = UnityEngine.Windows.File;

public static class TilemapData
{
    public static List<TilemapSprite> _tilemapSprites;

    public static Mesh _combinedMesh;

    public static Dictionary<int, UVCoords> _uvCoordsDictionary = new();

    public static void InitialiseMesh()
    {
        var textures = TerrainTexturesConfig._terrainTextures
            .Select(sprite => Path.Combine(TerrainTexturesConfig._texturesPath, sprite.textureFileName))
            .Select(LoadTexture).Where(texture => texture != null)
            .ToList();

        var combinedTexture = CreateCombinedTexture(textures, TerrainTexturesConfig._defaultTextureSize);

        InitializeTilemapSprites();

        var material = CreateMaterialWithTexture(combinedTexture);

        var mesh = CreateMeshForCombinedTexture();

        var gameObject = CreateGameObjectWithMeshAndMaterial(mesh, material);

        _combinedMesh = mesh;

        InitializeUVCoordsDictionary(combinedTexture);
    }

    public static Texture2D LoadTexture(string path)
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

    public static Texture2D CreateCombinedTexture(List<Texture2D> textures, int textureSize)
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

    public static Material CreateMaterialWithTexture(Texture2D texture)
    {
        var material = new Material(Shader.Find("Unlit/Texture"));
        material.mainTexture = texture;
        return material;
    }

    public static Mesh CreateMeshForCombinedTexture()
    {
        var mesh = new Mesh();

        return mesh;
    }

    public static GameObject CreateGameObjectWithMeshAndMaterial(Mesh mesh, Material material)
    {
        var gameObject = new GameObject("TerrainMesh");

        var meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;

        return gameObject;
    }

    public static void InitializeUVCoordsDictionary(Texture2D combinedTexture)
    {
        _uvCoordsDictionary.Clear();
        var terrainTextures = TerrainTexturesConfig._terrainTextures;

        foreach (var sprite in terrainTextures)
        {
            var id = sprite.id;
            var resolution = sprite.textureResolution;

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

    public static void InitializeTilemapSprites()
    {
        var terrainTextures = TerrainTexturesConfig._terrainTextures;

        _tilemapSprites = new List<TilemapSprite>();
        terrainTextures.ForEach(x => _tilemapSprites.Add(new TilemapSprite(x.id, x.name)));
    }
}

