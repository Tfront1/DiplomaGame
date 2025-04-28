using System;
using System.Collections.Generic;
using System.IO;
using Assets.Items.Ammunition;
using Assets.Items.Armor;
using Assets.Items.Interfaces;
using Assets.Items.Weapon;
using BuildingAction;
using Items.Resource;
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
                        texture.filterMode = FilterMode.Point;
                        texture.wrapMode = TextureWrapMode.Clamp;
                        texture.Apply();

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

    public Sprite GetResourceSprite(IBackpackItem resourceItem)
    {
        if (resourceItem.GetType() == typeof(ResourceElement))
        {
            return Sprites[$"Items/Resource/{resourceItem.Name}"];
        }
        if (resourceItem.GetType() == typeof(WeaponElement))
        {
            return Sprites[$"Items/Weapon/{resourceItem.Name}"];
        }
        if (resourceItem.GetType() == typeof(ArmorElement))
        {
            return Sprites[$"Items/Armor/{resourceItem.Name}"];
        }
        if (resourceItem.GetType() == typeof(AmmunitionElement))
        {
            return Sprites[$"Items/Equipment/{resourceItem.Name}"];
        }

        return null;
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

    public Sprite GetUnitSkillSprite(BaseSkill skill)
    {
        var statsPath = "Unit/Skills/";

        if (skill is SwordsmanshipSkill)
        {
            return Sprites[$"{statsPath}SwordSkill"];
        }
        if (skill is ArcherySkill)
        {
            return Sprites[$"{statsPath}ArcherySkill"];
        }
        if (skill is BuildingSkill)
        {
            return Sprites[$"{statsPath}BuildingSkill"];
        }
        if (skill is FarmingSkill)
        {
            return Sprites[$"{statsPath}SupplySkill"];
        }
        if (skill is SmithingSkill)
        {
            return Sprites[$"{statsPath}CraftSkill"];
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

    public Sprite GetItemSprite(int id, Type type)
    {
        if (type == typeof(WeaponElement))
        {
            return GetResourceSprite(WeaponConfig.WeaponElements.Find(x => x.Id == id));
        }
        if (type == typeof(ArmorElement))
        {
            return GetResourceSprite(ArmorConfig.ArmorElements.Find(x => x.Id == id));

        }
        if (type == typeof(AmmunitionElement))
        {
            return GetResourceSprite(AmmunitionConfig.AmmunitionElements.Find(x => x.Id == id));

        }

        return null;
    }
}
