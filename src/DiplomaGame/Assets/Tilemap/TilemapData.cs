using GameUtilities.Utils;
using System.Collections.Generic;
using UnityEngine;

public class TilemapData
{
    public List<TilemapSprite> _tilemapSprites;

    public Mesh _combinedMesh;

    public Dictionary<int, UVCoords> _uvCoordsDictionary = new();

    public Vector3[] _vertices;
    public Vector2[] _uv;
    public int[] _triangles;

    public int _width, _height;

    private Transform _terrainFolder;

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

        var combinedTexture = TilemapTexturesManager.Instance.GetCombinedTexture();

        _tilemapSprites = TilemapTexturesManager.Instance.GeTilemapSprites();

        var material = CreateMaterialWithTexture(combinedTexture);

        var mesh = CreateMeshForCombinedTexture();

        var gameObject = CreateGameObjectWithMeshAndMaterial(mesh, material);
        gameObject.transform.position = gameObjectPosition;

        _combinedMesh = mesh;

        _uvCoordsDictionary = TilemapTexturesManager.Instance.GetUVCoordsDictionary();

        MeshUtils.CreateEmptyMeshArrays(width * height, out var vertices, out var uv, out var triangles);
        _vertices = vertices;
        _triangles = triangles;
        _uv = uv;
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
    /// Creates a new GameObject with a mesh and material applied and puts it in the Terrain folder.
    /// </summary>
    /// <param name="mesh">The mesh to assign to the GameObject's MeshFilter component.</param>
    /// <param name="material">The material to assign to the GameObject's MeshRenderer component.</param>
    /// <returns>The newly created `GameObject` with the specified mesh and material.</returns>
    public GameObject CreateGameObjectWithMeshAndMaterial(Mesh mesh, Material material)
    {
        var gameObject = new GameObject($"TerrainMesh");
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;

        gameObject.transform.SetParent(GetTerrainFolder());

        return gameObject;
    }

    private Transform GetTerrainFolder()
    {
        if (_terrainFolder != null) return _terrainFolder;

        var folderGO = GameObject.Find("Terrain");
        if (folderGO == null)
        {
            folderGO = new GameObject("Terrain");
        }

        _terrainFolder = folderGO.transform;
        return _terrainFolder;
    }
}
