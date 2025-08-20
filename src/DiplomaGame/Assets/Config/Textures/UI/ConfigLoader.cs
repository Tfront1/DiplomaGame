using System.IO;
using System.Linq;
using UnityEngine;

public static partial class ConfigLoader
{
    public static void LoadUITexturesConfig()
    {
        var json = File.ReadAllText(ConfigPaths.UITexturesPath);
        var uiTexturesConfigDto = JsonUtility.FromJson<UITexturesConfigDto>(json);

        if (uiTexturesConfigDto == null)
        {
            Debug.Log("Error unit UI textures config");
            return;
        }

        UITexturesConfig.TexturesFolderPath = uiTexturesConfigDto.TexturesFolderPath;

        uiTexturesConfigDto.TextureGroups.ForEach(group => UITexturesConfig.Textures.Add(new UITextureFolder
            {
                Folder = group.Folder,
                Textures = group.Textures.Select(x => new TextureItem {Name = x.Name, Path = x.Path}).ToList()

        } ));

        Debug.Log("UI textures config loaded");
    }
}
