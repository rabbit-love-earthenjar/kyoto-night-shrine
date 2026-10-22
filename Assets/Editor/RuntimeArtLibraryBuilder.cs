using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds Assets/Resources/RuntimeArtLibrary.asset from the image paths that runtime scripts
// and build scenes load by string. It only reads textures and records references; it never
// changes, reimports or renames art. Runs automatically before every player build.
public class RuntimeArtLibraryBuilder : IPreprocessBuildWithReport
{
    private const string LibraryAssetPath = "Assets/Resources/RuntimeArtLibrary.asset";
    private const string RuntimeScriptsFolder = "Assets/Scripts";
    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg" };
    // Quoted literals such as "Assets/Art/a.png", "Art/a.png" or $"Assets/Art/guest_{id}.png".
    private static readonly Regex QuotedPathPattern = new Regex("\\$?@?\"([^\"\\r\\n]*)\"", RegexOptions.Compiled);
    // Serialized string fields in scenes, for Inspector overrides of path fields.
    private static readonly Regex SceneStringPattern = new Regex(@":\s*([^\s{}:][^\r\n]*\.(?:png|jpg|jpeg))\s*$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Rebuild();
    }

    [MenuItem("Tools/Night Shrine/Rebuild Runtime Art Library")]
    public static void RebuildFromMenu()
    {
        RuntimeArtLibrary library = Rebuild();
        Debug.Log($"RuntimeArtLibrary rebuilt with {library.Entries.Count} images.");
    }

    // Unity -batchmode -projectPath <project> -executeMethod RuntimeArtLibraryBuilder.RebuildFromCommandLine -quit
    public static void RebuildFromCommandLine()
    {
        RebuildFromMenu();
    }

    public static RuntimeArtLibrary Rebuild()
    {
        SortedSet<string> imagePaths = CollectImagePaths();
        List<RuntimeArtLibrary.Entry> entries = new List<RuntimeArtLibrary.Entry>();

        foreach (string path in imagePaths)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                continue;
            }

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            int sourceWidth = texture.width;
            int sourceHeight = texture.height;

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.GetSourceTextureWidthAndHeight(out sourceWidth, out sourceHeight);
            }

            entries.Add(new RuntimeArtLibrary.Entry
            {
                assetPath = path,
                texture = texture,
                primarySprite = AssetDatabase.LoadAssetAtPath<Sprite>(path),
                sprites = sprites,
                sourceWidth = sourceWidth,
                sourceHeight = sourceHeight
            });
        }

        RuntimeArtLibrary library = AssetDatabase.LoadAssetAtPath<RuntimeArtLibrary>(LibraryAssetPath);
        if (library == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LibraryAssetPath));
            library = ScriptableObject.CreateInstance<RuntimeArtLibrary>();
            library.SetEntries(entries);
            AssetDatabase.CreateAsset(library, LibraryAssetPath);
        }
        else
        {
            library.SetEntries(entries);
            EditorUtility.SetDirty(library);
        }

        AssetDatabase.SaveAssets();
        return library;
    }

    private static SortedSet<string> CollectImagePaths()
    {
        SortedSet<string> imagePaths = new SortedSet<string>(StringComparer.Ordinal);
        // Interpolated path -> regex over full asset paths, searched inside its fixed folder.
        Dictionary<string, string> patterns = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (string scriptPath in Directory.GetFiles(RuntimeScriptsFolder, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(scriptPath);

            foreach (Match match in QuotedPathPattern.Matches(source))
            {
                AddLiteral(match.Groups[1].Value, imagePaths, patterns);
            }
        }

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled || !File.Exists(scene.path))
            {
                continue;
            }

            foreach (Match match in SceneStringPattern.Matches(File.ReadAllText(scene.path)))
            {
                AddLiteral(match.Groups[1].Value, imagePaths, patterns);
            }
        }

        // Interpolated paths (e.g. guest walk frames) include only the images whose path fits the pattern.
        foreach (KeyValuePair<string, string> pattern in patterns)
        {
            if (!AssetDatabase.IsValidFolder(pattern.Value))
            {
                continue;
            }

            Regex pathPattern = new Regex(pattern.Key, RegexOptions.IgnoreCase);

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { pattern.Value }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsImagePath(path) && pathPattern.IsMatch(path))
                {
                    imagePaths.Add(path);
                }
            }
        }

        return imagePaths;
    }

    private static void AddLiteral(string literal, SortedSet<string> imagePaths, Dictionary<string, string> patterns)
    {
        string value = literal.Trim().Replace('\\', '/');
        int interpolationStart = value.IndexOf('{');

        if (interpolationStart >= 0)
        {
            string fixedPrefix = value.Substring(0, interpolationStart);
            int lastSlash = fixedPrefix.LastIndexOf('/');
            if (!IsImagePath(value) || lastSlash <= 0)
            {
                return;
            }

            // "Assets/Art/guest_{id}/guest_{id}.png" -> ^Assets/Art/guest_[^/]*/guest_[^/]*\.png$
            string normalized = RuntimeArtLibrary.NormalizePath(value);
            string regex = "^" + string.Join("[^/]*", Regex.Split(normalized, @"\{[^}]*\}").Select(Regex.Escape)) + "$";
            patterns[regex] = RuntimeArtLibrary.NormalizePath(fixedPrefix.Substring(0, lastSlash));
            return;
        }

        int fragment = value.IndexOf('#');
        if (fragment >= 0)
        {
            value = value.Substring(0, fragment);
        }

        if (!IsImagePath(value))
        {
            return;
        }

        string path = RuntimeArtLibrary.NormalizePath(value);
        if (File.Exists(path))
        {
            imagePaths.Add(path);
        }
    }

    private static bool IsImagePath(string path)
    {
        string lower = path.ToLowerInvariant();
        return ImageExtensions.Any(lower.EndsWith);
    }
}
