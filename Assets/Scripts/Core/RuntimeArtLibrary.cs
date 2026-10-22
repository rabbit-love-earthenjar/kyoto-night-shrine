using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Path-based art lookup that also works in player builds.
// Several cafe/Hub scripts load art by project path ("Assets/Art/...png" or "Art/...png").
// In the Editor that keeps using AssetDatabase / the source PNG exactly as before.
// In a build, the paths resolve through Resources/RuntimeArtLibrary.asset, which only
// references the existing textures; it never changes their import settings.
// Rebuild the asset from Tools > Night Shrine > Rebuild Runtime Art Library (also runs before every build).
public class RuntimeArtLibrary : ScriptableObject
{
    public const string ResourcePath = "RuntimeArtLibrary";

    [Serializable]
    public class Entry
    {
        public string assetPath;
        public Texture2D texture;
        public Sprite primarySprite;
        public Sprite[] sprites = new Sprite[0];
        public int sourceWidth;
        public int sourceHeight;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private static RuntimeArtLibrary cachedLibrary;
    private Dictionary<string, Entry> entriesByPath;
    private Dictionary<Texture, Entry> entriesByTexture;

#if UNITY_EDITOR
    // Validation hook: makes the Editor resolve art exactly as a player build would.
    public static bool SimulatePlayerLookupInEditor { get; set; }
#endif

    public IReadOnlyList<Entry> Entries => entries;

    public void SetEntries(List<Entry> newEntries)
    {
        entries = newEntries ?? new List<Entry>();
        entriesByPath = null;
        entriesByTexture = null;
    }

    // Accepts "Assets/Art/x.png", "Art/x.png" (relative to Application.dataPath) or backslash paths.
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string normalized = path.Trim().Replace('\\', '/');
        return normalized.StartsWith("Assets/", StringComparison.Ordinal) ? normalized : "Assets/" + normalized;
    }

    public static Sprite LoadSprite(string assetPath)
    {
        string path = NormalizePath(assetPath);

#if UNITY_EDITOR
        if (!SimulatePlayerLookupInEditor)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
#endif

        Entry entry = FindEntry(path);
        return entry != null ? entry.primarySprite : null;
    }

    public static Sprite[] LoadAllSprites(string assetPath)
    {
        string path = NormalizePath(assetPath);

#if UNITY_EDITOR
        if (!SimulatePlayerLookupInEditor)
        {
            List<Sprite> editorSprites = new List<Sprite>();
            foreach (UnityEngine.Object asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    editorSprites.Add(sprite);
                }
            }

            return editorSprites.ToArray();
        }
#endif

        Entry entry = FindEntry(path);
        return entry != null && entry.sprites != null ? entry.sprites : new Sprite[0];
    }

    public static Texture2D LoadTexture(string assetPath)
    {
        string path = NormalizePath(assetPath);

#if UNITY_EDITOR
        if (!SimulatePlayerLookupInEditor)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
#endif

        Entry entry = FindEntry(path);
        return entry != null ? entry.texture : null;
    }

    public static string GetAssetPath(Sprite sprite)
    {
        if (sprite == null)
        {
            return string.Empty;
        }

#if UNITY_EDITOR
        if (!SimulatePlayerLookupInEditor)
        {
            return UnityEditor.AssetDatabase.GetAssetPath(sprite);
        }
#endif

        RuntimeArtLibrary library = Load();
        if (library == null || sprite.texture == null)
        {
            return string.Empty;
        }

        library.EnsureLookups();
        return library.entriesByTexture.TryGetValue(sprite.texture, out Entry entry) ? entry.assetPath : string.Empty;
    }

    // Returns a new readable RGBA32 texture with the source PNG's pixel size, or null.
    // The caller owns (and should Destroy) the returned texture.
    public static Texture2D LoadReadableTextureCopy(string assetPath)
    {
        string path = NormalizePath(assetPath);

#if UNITY_EDITOR
        if (!SimulatePlayerLookupInEditor)
        {
            string absolutePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
            if (!File.Exists(absolutePath))
            {
                return null;
            }

            Texture2D fileTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!fileTexture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                Destroy(fileTexture);
                return null;
            }

            return fileTexture;
        }
#endif

        Entry entry = FindEntry(path);
        if (entry == null || entry.texture == null)
        {
            return null;
        }

        int width = entry.sourceWidth > 0 ? entry.sourceWidth : entry.texture.width;
        int height = entry.sourceHeight > 0 ? entry.sourceHeight : entry.texture.height;
        return CopyToReadableTexture(entry.texture, width, height);
    }

    private static Texture2D CopyToReadableTexture(Texture source, int width, int height)
    {
        // Imported textures are not CPU-readable; draw them into a temporary render target
        // and read the pixels back. sRGB in/out keeps the stored colour values unchanged.
        RenderTexture renderTarget = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture previous = RenderTexture.active;

        try
        {
            Graphics.Blit(source, renderTarget);
            RenderTexture.active = renderTarget;
            Texture2D readable = new Texture2D(width, height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            readable.Apply();
            return readable;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTarget);
        }
    }

    private static Entry FindEntry(string normalizedPath)
    {
        RuntimeArtLibrary library = Load();
        if (library == null || string.IsNullOrEmpty(normalizedPath))
        {
            return null;
        }

        library.EnsureLookups();
        return library.entriesByPath.TryGetValue(normalizedPath, out Entry entry) ? entry : null;
    }

    private static RuntimeArtLibrary Load()
    {
        if (cachedLibrary == null)
        {
            cachedLibrary = Resources.Load<RuntimeArtLibrary>(ResourcePath);

            if (cachedLibrary == null)
            {
                Debug.LogWarning($"RuntimeArtLibrary is missing at Resources/{ResourcePath}. Run Tools > Night Shrine > Rebuild Runtime Art Library.");
            }
        }

        return cachedLibrary;
    }

    private void EnsureLookups()
    {
        if (entriesByPath != null)
        {
            return;
        }

        entriesByPath = new Dictionary<string, Entry>(StringComparer.Ordinal);
        entriesByTexture = new Dictionary<Texture, Entry>();

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.assetPath))
            {
                continue;
            }

            entriesByPath[entry.assetPath] = entry;

            if (entry.texture != null && !entriesByTexture.ContainsKey(entry.texture))
            {
                entriesByTexture[entry.texture] = entry;
            }
        }
    }
}
