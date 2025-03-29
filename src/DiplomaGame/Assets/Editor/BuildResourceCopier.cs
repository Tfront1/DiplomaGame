using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class BuildResourceCopier
{
    private static readonly string[] SourcePaths = new string[]
    {
        "Assets/Config",
        "Assets/Textures"
    };

    private static readonly string[] AllowedExtensions = new string[]
    {
        ".json",
        ".png"
    };

    private const string DestinationFolder = "Assets";

    [InitializeOnLoadMethod]
    private static void RegisterBuildProcessor()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandler);
    }

    private static void BuildPlayerHandler(BuildPlayerOptions options)
    {
        BuildPipeline.BuildPlayer(options);
        CopyResources(options.locationPathName);
    }

    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuildProject)
    {
        CopyResources(pathToBuildProject);
    }

    private static void CopyResources(string buildPath)
    {
        var buildDir = Path.GetDirectoryName(buildPath);

        var destDir = Path.Combine(buildDir, DestinationFolder);
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        foreach (var sourcePath in SourcePaths)
        {
            CopyDirectory(sourcePath, Path.Combine(destDir, Path.GetFileName(sourcePath)));
        }

        Debug.Log($"JSON і PNG файли успішно скопійовано в {destDir}");
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        if (!Directory.Exists(sourceDir))
        {
            Debug.LogWarning($"Вихідна директорія не існує: {sourceDir}");
            return;
        }

        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var extension = Path.GetExtension(file).ToLower();

            if (AllowedExtensions.Contains(extension))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, true);
                Debug.Log($"Скопійовано файл: {fileName}");
            }
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var subDirName = Path.GetFileName(subDir);

            var shouldCopyDir = Directory.GetFiles(subDir, "*.*", SearchOption.AllDirectories)
                .Any(f => AllowedExtensions.Contains(Path.GetExtension(f).ToLower()));

            if (shouldCopyDir)
            {
                CopyDirectory(subDir, Path.Combine(destDir, subDirName));
            }
        }
    }
}