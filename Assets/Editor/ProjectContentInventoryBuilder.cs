using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ProjectContentInventoryBuilder
{
    private const string ArtRoot = "Assets/Art";
    private const string SceneRoot = "Assets/Scenes";
    private const string OutputRoot = "Docs/Generated";
    private const string ArtCsvPath = OutputRoot + "/ArtAssetInventory.csv";
    private const string SceneCsvPath = OutputRoot + "/SceneInventory.csv";
    private const string SummaryPath = OutputRoot + "/ArtAndSceneInventory.md";

    private static readonly HashSet<string> DependencySourceExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".unity", ".prefab", ".asset", ".controller", ".anim", ".mat", ".playable", ".overrideController"
    };

    [MenuItem("Tools/Night Shrine/Rebuild Art And Scene Inventory")]
    public static void BuildFromMenu()
    {
        BuildInventory();
        AssetDatabase.Refresh();
        Debug.Log($"Night Shrine art and scene inventory rebuilt under {OutputRoot}.");
    }

    public static void BuildFromCommandLine()
    {
        BuildInventory();
        AssetDatabase.Refresh();
        Debug.Log($"Night Shrine art and scene inventory rebuilt under {OutputRoot}.");
        EditorApplication.Exit(0);
    }

    private static void BuildInventory()
    {
        Directory.CreateDirectory(OutputRoot);
        List<string> artPaths = FindAssetPaths("t:Texture2D", ArtRoot);
        List<string> scenePaths = FindAssetPaths("t:SceneAsset", SceneRoot);
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (!string.IsNullOrWhiteSpace(buildScene.path)
                && buildScene.path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                && !scenePaths.Contains(buildScene.path, StringComparer.OrdinalIgnoreCase))
            {
                scenePaths.Add(buildScene.path);
            }
        }

        scenePaths = scenePaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
        Dictionary<string, HashSet<string>> references = BuildSerializedReferenceMap(artPaths);
        List<ArtRecord> artRecords = BuildArtRecords(artPaths, references);
        List<SceneRecord> sceneRecords = BuildSceneRecords(scenePaths);

        File.WriteAllText(ArtCsvPath, BuildArtCsv(artRecords), new UTF8Encoding(true));
        File.WriteAllText(SceneCsvPath, BuildSceneCsv(sceneRecords), new UTF8Encoding(true));
        File.WriteAllText(SummaryPath, BuildSummary(artRecords, sceneRecords), new UTF8Encoding(true));
    }

    private static List<string> FindAssetPaths(string filter, string root)
    {
        return AssetDatabase.FindAssets(filter, new[] { root })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, HashSet<string>> BuildSerializedReferenceMap(IReadOnlyCollection<string> artPaths)
    {
        HashSet<string> artPathSet = new HashSet<string>(artPaths, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, HashSet<string>> references = artPaths.ToDictionary(
            path => path,
            _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        foreach (string sourcePath in AssetDatabase.GetAllAssetPaths())
        {
            if (!sourcePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                || !DependencySourceExtensions.Contains(Path.GetExtension(sourcePath)))
            {
                continue;
            }

            foreach (string dependency in AssetDatabase.GetDependencies(sourcePath, false))
            {
                if (artPathSet.Contains(dependency))
                {
                    references[dependency].Add(sourcePath);
                }
            }
        }

        return references;
    }

    private static List<ArtRecord> BuildArtRecords(
        IReadOnlyList<string> artPaths,
        IReadOnlyDictionary<string, HashSet<string>> references)
    {
        Dictionary<string, int> filenameCounts = artPaths
            .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        List<ArtRecord> records = new List<ArtRecord>(artPaths.Count);

        foreach (string path in artPaths)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            references.TryGetValue(path, out HashSet<string> sources);
            List<string> sortedSources = sources != null
                ? sources.OrderBy(source => source, StringComparer.OrdinalIgnoreCase).ToList()
                : new List<string>();
            string filename = Path.GetFileName(path);

            records.Add(new ArtRecord
            {
                Path = path,
                Category = GetArtCategory(path),
                Extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant(),
                Width = texture != null ? texture.width : 0,
                Height = texture != null ? texture.height : 0,
                TextureType = importer != null ? importer.textureType.ToString() : "Unknown",
                SpriteMode = importer != null ? importer.spriteImportMode.ToString() : "N/A",
                PixelsPerUnit = importer != null ? importer.spritePixelsPerUnit : 0f,
                ReferenceCount = sortedSources.Count,
                ReferencedBy = string.Join(" | ", sortedSources),
                DuplicateFilename = filenameCounts.TryGetValue(filename, out int count) && count > 1
            });
        }

        return records;
    }

    private static List<SceneRecord> BuildSceneRecords(IReadOnlyList<string> scenePaths)
    {
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        Dictionary<string, int> settingsOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> runtimeBuildIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, bool> enabledState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        int enabledIndex = 0;

        for (int index = 0; index < buildScenes.Length; index++)
        {
            string path = buildScenes[index].path;
            settingsOrder[path] = index;
            enabledState[path] = buildScenes[index].enabled;
            runtimeBuildIndex[path] = buildScenes[index].enabled ? enabledIndex++ : -1;
        }

        List<SceneRecord> records = new List<SceneRecord>(scenePaths.Count);
        foreach (string path in scenePaths)
        {
            bool exists = File.Exists(path);
            string[] dependencies = exists ? AssetDatabase.GetDependencies(path, false) : Array.Empty<string>();
            FileInfo file = new FileInfo(path);
            bool inBuild = settingsOrder.TryGetValue(path, out int order);
            bool enabled = inBuild && enabledState[path];

            records.Add(new SceneRecord
            {
                Path = path,
                Name = Path.GetFileNameWithoutExtension(path),
                Exists = exists,
                InBuildSettings = inBuild,
                BuildEnabled = enabled,
                SettingsOrder = inBuild ? order : -1,
                RuntimeBuildIndex = enabled ? runtimeBuildIndex[path] : -1,
                FileKilobytes = file.Exists ? Math.Round(file.Length / 1024d, 1) : 0d,
                ArtDependencyCount = dependencies.Count(dependency => dependency.StartsWith(ArtRoot + "/", StringComparison.OrdinalIgnoreCase)),
                ScriptDependencyCount = dependencies.Count(dependency => dependency.StartsWith("Assets/Scripts/", StringComparison.OrdinalIgnoreCase))
            });
        }

        return records;
    }

    private static string BuildArtCsv(IEnumerable<ArtRecord> records)
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Path,Category,Extension,Width,Height,TextureType,SpriteMode,PixelsPerUnit,SerializedReferenceCount,DuplicateFilename,ReferencedBy");
        foreach (ArtRecord record in records)
        {
            AppendCsvRow(csv,
                record.Path,
                record.Category,
                record.Extension,
                record.Width.ToString(CultureInfo.InvariantCulture),
                record.Height.ToString(CultureInfo.InvariantCulture),
                record.TextureType,
                record.SpriteMode,
                record.PixelsPerUnit.ToString("0.##", CultureInfo.InvariantCulture),
                record.ReferenceCount.ToString(CultureInfo.InvariantCulture),
                record.DuplicateFilename ? "Yes" : "No",
                record.ReferencedBy);
        }

        return csv.ToString();
    }

    private static string BuildSceneCsv(IEnumerable<SceneRecord> records)
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Path,SceneName,Exists,InBuildSettings,BuildEnabled,SettingsOrder,RuntimeBuildIndex,FileKB,ArtDependencies,ScriptDependencies");
        foreach (SceneRecord record in records)
        {
            AppendCsvRow(csv,
                record.Path,
                record.Name,
                record.Exists ? "Yes" : "No",
                record.InBuildSettings ? "Yes" : "No",
                record.BuildEnabled ? "Yes" : "No",
                record.SettingsOrder.ToString(CultureInfo.InvariantCulture),
                record.RuntimeBuildIndex.ToString(CultureInfo.InvariantCulture),
                record.FileKilobytes.ToString("0.0", CultureInfo.InvariantCulture),
                record.ArtDependencyCount.ToString(CultureInfo.InvariantCulture),
                record.ScriptDependencyCount.ToString(CultureInfo.InvariantCulture));
        }

        return csv.ToString();
    }

    private static string BuildSummary(IReadOnlyList<ArtRecord> artRecords, IReadOnlyList<SceneRecord> sceneRecords)
    {
        int unreferencedCount = artRecords.Count(record => record.ReferenceCount == 0);
        int duplicateCount = artRecords.Count(record => record.DuplicateFilename);
        int oversizedCount = artRecords.Count(record => record.Width > 4096 || record.Height > 4096);
        int missingEnabledSceneCount = sceneRecords.Count(record => record.BuildEnabled && !record.Exists);
        StringBuilder markdown = new StringBuilder();
        markdown.AppendLine("# Art And Scene Inventory");
        markdown.AppendLine();
        markdown.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} local time");
        markdown.AppendLine();
        markdown.AppendLine("## Snapshot");
        markdown.AppendLine();
        markdown.AppendLine($"- Art textures: {artRecords.Count}");
        markdown.AppendLine($"- Scene files: {sceneRecords.Count}");
        markdown.AppendLine($"- Enabled build scenes: {sceneRecords.Count(record => record.BuildEnabled)}");
        markdown.AppendLine($"- Enabled build scenes with missing files: {missingEnabledSceneCount}");
        markdown.AppendLine($"- Art files with no serialized reference: {unreferencedCount}");
        markdown.AppendLine($"- Art files sharing a filename: {duplicateCount}");
        markdown.AppendLine($"- Textures larger than 4096 pixels on one axis: {oversizedCount}");
        markdown.AppendLine();
        markdown.AppendLine("Complete row-level lists are stored in `ArtAssetInventory.csv` and `SceneInventory.csv` beside this file.");
        markdown.AppendLine("Human purpose, flow, and acceptance decisions are tracked separately in `../SceneReviewChecklist.md` so regeneration never overwrites review notes.");
        markdown.AppendLine();
        markdown.AppendLine("## Audit Boundary");
        markdown.AppendLine();
        markdown.AppendLine("`SerializedReferenceCount` covers dependencies from scenes, prefabs, controllers, animations, materials, and ScriptableObject assets. A zero does not prove an asset is unused: resources loaded dynamically by code, editor-only source art, future art, and documentation references may legitimately report zero. Review before moving or deleting anything.");
        markdown.AppendLine();
        markdown.AppendLine("## Art Categories");
        markdown.AppendLine();
        markdown.AppendLine("| Category | Files | Referenced | No serialized reference | Duplicate names | >4096 px |");
        markdown.AppendLine("|---|---:|---:|---:|---:|---:|");
        foreach (IGrouping<string, ArtRecord> group in artRecords.GroupBy(record => record.Category).OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            markdown.AppendLine($"| {EscapeMarkdown(group.Key)} | {group.Count()} | {group.Count(record => record.ReferenceCount > 0)} | {group.Count(record => record.ReferenceCount == 0)} | {group.Count(record => record.DuplicateFilename)} | {group.Count(record => record.Width > 4096 || record.Height > 4096)} |");
        }

        markdown.AppendLine();
        markdown.AppendLine("## Scenes");
        markdown.AppendLine();
        markdown.AppendLine("| Scene | File | Build | Index | Size KB | Art deps | Script deps | Path |");
        markdown.AppendLine("|---|---|---|---:|---:|---:|---:|---|");
        foreach (SceneRecord record in sceneRecords.OrderBy(record => record.BuildEnabled ? record.RuntimeBuildIndex : int.MaxValue).ThenBy(record => record.Name, StringComparer.OrdinalIgnoreCase))
        {
            string buildStatus = record.BuildEnabled ? "Enabled" : record.InBuildSettings ? "Disabled" : "Not listed";
            string fileStatus = record.Exists ? "Present" : "MISSING";
            markdown.AppendLine($"| {EscapeMarkdown(record.Name)} | {fileStatus} | {buildStatus} | {record.RuntimeBuildIndex} | {record.FileKilobytes:0.0} | {record.ArtDependencyCount} | {record.ScriptDependencyCount} | `{record.Path}` |");
        }

        return markdown.ToString();
    }

    private static string GetArtCategory(string path)
    {
        string relative = path.Length > ArtRoot.Length ? path.Substring(ArtRoot.Length).TrimStart('/') : string.Empty;
        int separator = relative.IndexOf('/');
        return separator >= 0 ? relative.Substring(0, separator) : string.IsNullOrEmpty(relative) ? "Root" : "Root";
    }

    private static void AppendCsvRow(StringBuilder builder, params string[] values)
    {
        builder.AppendLine(string.Join(",", values.Select(EscapeCsv)));
    }

    private static string EscapeCsv(string value)
    {
        string safe = value ?? string.Empty;
        return safe.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
            ? $"\"{safe.Replace("\"", "\"\"")}\""
            : safe;
    }

    private static string EscapeMarkdown(string value)
    {
        return (value ?? string.Empty).Replace("|", "\\|");
    }

    private sealed class ArtRecord
    {
        public string Path;
        public string Category;
        public string Extension;
        public int Width;
        public int Height;
        public string TextureType;
        public string SpriteMode;
        public float PixelsPerUnit;
        public int ReferenceCount;
        public string ReferencedBy;
        public bool DuplicateFilename;
    }

    private sealed class SceneRecord
    {
        public string Path;
        public string Name;
        public bool Exists;
        public bool InBuildSettings;
        public bool BuildEnabled;
        public int SettingsOrder;
        public int RuntimeBuildIndex;
        public double FileKilobytes;
        public int ArtDependencyCount;
        public int ScriptDependencyCount;
    }
}
