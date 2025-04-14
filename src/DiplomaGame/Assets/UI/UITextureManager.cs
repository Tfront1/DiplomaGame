using System;
using System.Collections.Generic;
using System.IO;
using BuildingAction;
using UnityEngine;

public class UITextureManager
{
    private static UITextureManager _instance;
    private static readonly object _lock = new();

    public Dictionary<string, Sprite> Sprites { get; set; }

    private UITextureManager()
    {
    }

    public static UITextureManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new UITextureManager();
                    }
                }
            }
            return _instance;
        }
    }

    public static void LoadAllTextures()
    {
        Instance.Sprites = new Dictionary<string, Sprite>();

        UITexturesConfig.Textures.ForEach(textureFolder =>
        {
            var folderPath = textureFolder.Folder;

            textureFolder.Textures.ForEach(textureItem =>
            {
                try
                {
                    var texturePath = $"{UITexturesConfig.TexturesFolderPath}{folderPath}{textureItem.Path}";

                    var fileData = File.ReadAllBytes(texturePath);

                    var texture = new Texture2D(2, 2);

                    if (texture.LoadImage(fileData))
                    {
                        var sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            Vector2.zero
                        );

                        Instance.Sprites[$"{folderPath}{textureItem.Name}"] = sprite;
                    }
                    else
                    {
                        Debug.LogError($"Failed to load image at path: {texturePath}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error loading texture {textureItem.Name}: {ex.Message}");
                }
            });
        });
    }

    public Sprite GetResourceSprite(int id)
    {
        var resource = ResourcesConfig.ResourceElements.Find(x => x.Id == id);
        return Sprites[$"Resources/{resource.Name}"];
    }

    public Sprite GetActionSprite(string actionName)
    {
        return Sprites[$"Actions/{actionName}"];
    }

    public Sprite GetUnitStatSprite(BaseStat stat)
    {
        var statsPath = "Unit/Stats/";

        if (stat is Health)
        {
            return Sprites[$"{statsPath}HP"];
        }
        if (stat is Stamina)
        {
            return Sprites[$"{statsPath}Stamina"];
        }
        if (stat is Armor)
        {
            return Sprites[$"{statsPath}Armor"];
        }
        if (stat is Hunger)
        {
            return Sprites[$"{statsPath}Hunger"];
        }
        
        return null;
    }

    public Sprite GetBuildingActionSprite(Type buildingAction)
    {
        var path = "Actions/";
        if (buildingAction == typeof(AttackBuildingAction))
        {
            return Sprites[$"{path}Attack"];
        }
        if (buildingAction == typeof(BringResourcesBuildingAction))
        {
            return Sprites[$"{path}BringResources"];
        }
        if (buildingAction == typeof(BuildStructureAction))
        {
            return Sprites[$"{path}Build"];
        }
        if (buildingAction == typeof(CraftItemBuildingAction))
        {
            return Sprites[$"{path}Craft"];
        }
        if (buildingAction == typeof(MoveAndEquipUnitAction))
        {
            return Sprites[$"{path}TakeEquip"];
        }
        if (buildingAction == typeof(MoveToBuildingAction))
        {
            return Sprites[$"{path}GoToTarget"];
        }

        return null;
    }
}
