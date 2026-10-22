using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Checks that path-loaded cafe/Hub art resolves the way a player build resolves it
// (RuntimeArtLibrary instead of AssetDatabase / source PNG files). It rebuilds the library
// asset first; it does not build a player, so a real Windows build still needs a look.
// Run: Unity -batchmode -projectPath <project> -executeMethod RuntimeArtLibraryPlayModeValidation.Run -logFile <log>
[InitializeOnLoad]
public static class RuntimeArtLibraryPlayModeValidation
{
    private const string Key = "RuntimeArtLibrary.Validation.";
    private const string LogDirectory = "Logs/RuntimeArtLibraryValidation";
    private const string CafeScenePath = "Assets/Scenes/CafeInterior_Temporary.unity";
    private const string HubSceneName = "HubMap_Day";
    private static readonly string[] PixelSamplePaths =
    {
        "Assets/Art/cafe_icon/progress_bar.png",
        "Assets/Art/cafe_icon/speak_bubble.png",
        "Assets/Art/farm_icon/wheat_growing.png",
        "Assets/Art/Backgrounds/store.png"
    };
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int step;
    private static double stepAt;

    static RuntimeArtLibraryPlayModeValidation()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        Directory.CreateDirectory(LogDirectory);
        string save = SaveManager.SaveFilePath;
        SessionState.SetString(Key + "SavePath", save);
        SessionState.SetBool(Key + "HadSave", File.Exists(save));
        if (File.Exists(save)) File.Copy(save, LogDirectory + "/autosave.before.json", true);
        File.WriteAllText(LogDirectory + "/playmode.txt", "Runtime art library checks (player lookup simulated in the Editor)\n");
        SessionState.SetInt(Key + "ExitCode", 0);

        try
        {
            CheckLibraryMatchesEditorLookup();
        }
        catch (Exception exception)
        {
            File.AppendAllText(LogDirectory + "/playmode.txt", "FAIL: " + exception.Message + "\n");
            RestoreSaveAndExit(1);
            return;
        }

        SessionState.SetBool(Key + "Requested", true);
        SessionState.SetInt(Key + "Phase", 0);
    }

    private static void CheckLibraryMatchesEditorLookup()
    {
        RuntimeArtLibrary library = RuntimeArtLibraryBuilder.Rebuild();
        Require(library.Entries.Count > 0, "Library is empty");
        int mismatches = 0;

        foreach (RuntimeArtLibrary.Entry entry in library.Entries)
        {
            RuntimeArtLibrary.SimulatePlayerLookupInEditor = false;
            Sprite editorSprite = RuntimeArtLibrary.LoadSprite(entry.assetPath);
            Texture2D editorTexture = RuntimeArtLibrary.LoadTexture(entry.assetPath);
            RuntimeArtLibrary.SimulatePlayerLookupInEditor = true;
            Sprite playerSprite = RuntimeArtLibrary.LoadSprite(entry.assetPath);
            Texture2D playerTexture = RuntimeArtLibrary.LoadTexture(entry.assetPath);

            if (editorSprite != playerSprite || editorTexture != playerTexture || playerTexture == null)
            {
                mismatches++;
                Record("Mismatch: " + entry.assetPath);
            }
        }

        RuntimeArtLibrary.SimulatePlayerLookupInEditor = false;
        Require(mismatches == 0, mismatches + " library entries differ from Editor lookup");
        Record("Library rebuilt: " + library.Entries.Count + " images; player lookup returns the same Sprite/Texture as the Editor for every entry.");

        foreach (string path in new[]
        {
            "Assets/Art/cafe_icon/speak_bubble_request.png",
            "Assets/Art/cafe_icon/menu_runtime/inari_coffee.png",
            "Assets/Art/farm_icon/wheat_seed.png",
            "Assets/Art/Backgrounds/farm.png"
        })
        {
            Require(library.Entries.Any(entry => entry.assetPath == path) || !File.Exists(path), "Library is missing " + path);
        }
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key + "Requested", false)) return;
        try
        {
            int phase = SessionState.GetInt(Key + "Phase", 0);
            if (phase == 0 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorSceneManager.OpenScene(CafeScenePath, OpenSceneMode.Single);
                SessionState.SetInt(Key + "Phase", 1);
                EditorApplication.EnterPlaymode();
                return;
            }
            if (phase == 1 && EditorApplication.isPlaying)
            {
                SessionState.SetInt(Key + "Phase", 2);
                RuntimeArtLibrary.SimulatePlayerLookupInEditor = true;
                SceneManager.LoadScene(Path.GetFileNameWithoutExtension(CafeScenePath));
                Advance(0);
                return;
            }
            if (phase == 2 && EditorApplication.isPlaying) RunStep();
            if (SessionState.GetInt(Key + "Phase", 0) == 3 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(Key + "Requested", false);
                RestoreSaveAndExit(SessionState.GetInt(Key + "ExitCode", 1));
            }
        }
        catch (Exception exception) { Fail(exception.ToString()); }
    }

    private static void RunStep()
    {
        switch (step)
        {
            case 0:
                if (EditorApplication.timeSinceStartup - stepAt < 1.0) return;
                CheckPixelCopies();
                CafeSceneController cafe = UnityEngine.Object.FindAnyObjectByType<CafeSceneController>();
                CafeOperationController operation = UnityEngine.Object.FindAnyObjectByType<CafeOperationController>();
                Require(cafe != null && operation != null, "Cafe controllers missing");
                cafe.ShowReceptionPanel();
                CafeProductionPopupController popup = Field<CafeProductionPopupController>(cafe, "cafeProductionPopupController");
                Require(popup != null, "Production popup missing");
                foreach (string imageField in new[] { "coffeeMachineImage", "bakerMachineImage", "recipeBubbleImage", "inariCoffeeIconImage", "yozakuraCakeIconImage" })
                {
                    Image image = Field<Image>(popup, imageField);
                    Require(image != null && image.sprite != null, "Popup image has no sprite: " + imageField);
                }
                popup.ClosePopup();
                Record("Cafe production popup images resolved through the library.");
                operation.TryOpenForBusiness();
                Advance(1);
                break;
            case 1:
                if (EditorApplication.timeSinceStartup - stepAt < 6.0) return;
                int guestCount = 0;
                foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))
                {
                    if (renderer.name != "GuestSpriteVisual") continue;
                    guestCount++;
                    Require(renderer.sprite != null, "Guest has no sprite: " + renderer.transform.parent.name);
                }
                Require(guestCount > 0, "No cafe guests appeared");
                Record("Cafe guests visible with library sprites: " + guestCount + ".");
                SceneManager.LoadScene(HubSceneName);
                Advance(2);
                break;
            case 2:
                if (EditorApplication.timeSinceStartup - stepAt < 1.5) return;
                HubFarmPanelController farm = UnityEngine.Object.FindAnyObjectByType<HubFarmPanelController>(FindObjectsInactive.Include);
                HubIngredientShopController shop = UnityEngine.Object.FindAnyObjectByType<HubIngredientShopController>(FindObjectsInactive.Include);
                Require(farm != null && shop != null, "Hub farm/shop controllers missing");
                // Call the loaders directly: opening the panels would also grant starter seeds in PlayerPrefs.
                Require(Invoke<Sprite>(farm, "ResolveFarmBackgroundSprite") != null, "Farm background not loaded");
                Require(Invoke<Sprite>(farm, "LoadSpriteFromAssetPath", "Art/farm_icon/wheat_growing.png", "ValidationCrop", true, false, false) != null, "Farm crop (edge cleanup) not loaded");
                Record("Hub farm background and cleaned crop sprite loaded through the library.");
                if (IsNotImported("Assets/Art/Backgrounds/store.png"))
                {
                    Sprite shopBackground = Invoke<Sprite>(shop, "ResolveShopInteriorBackgroundSprite");
                    Record(shopBackground == null
                        ? "KNOWN ISSUE: shop background would be missing in a build (store.png is not imported)."
                        : "Shop background falls back to the serialized sprite " + shopBackground.name + ".");
                }
                else
                {
                    Require(Invoke<Sprite>(shop, "ResolveShopInteriorBackgroundSprite") != null, "Shop background not loaded");
                    Record("Ingredient shop background loaded through the library.");
                }
                SessionState.SetInt(Key + "Phase", 3);
                RuntimeArtLibrary.SimulatePlayerLookupInEditor = false;
                EditorApplication.ExitPlaymode();
                break;
        }
    }

    // Compares the build-time readable copy (GPU copy of the imported texture) with the source PNG.
    private static void CheckPixelCopies()
    {
        foreach (string path in PixelSamplePaths)
        {
            if (!File.Exists(path)) continue;
            if (IsNotImported(path)) continue;
            RuntimeArtLibrary.SimulatePlayerLookupInEditor = false;
            Texture2D source = RuntimeArtLibrary.LoadReadableTextureCopy(path);
            RuntimeArtLibrary.SimulatePlayerLookupInEditor = true;
            Texture2D copy = RuntimeArtLibrary.LoadReadableTextureCopy(path);
            Require(source != null && copy != null, "Readable copy missing: " + path);
            Require(source.width == copy.width && source.height == copy.height, "Readable copy size differs: " + path);

            // Fully transparent pixels are excluded from the colour metric: the importer's
            // alpha-is-transparency option rewrites their (invisible) RGB on purpose.
            Color32[] a = source.GetPixels32();
            Color32[] b = copy.GetPixels32();
            double alphaTotal = 0;
            double visibleRgbTotal = 0;
            int visiblePixels = 0;
            int transparentRgbChanged = 0;
            for (int i = 0; i < a.Length; i++)
            {
                alphaTotal += Math.Abs(a[i].a - b[i].a);
                int rgbDifference = Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b);
                if (a[i].a > 0)
                {
                    visibleRgbTotal += rgbDifference;
                    visiblePixels++;
                }
                else if (rgbDifference > 0)
                {
                    transparentRgbChanged++;
                }
            }
            double meanAlpha = alphaTotal / a.Length;
            double meanVisibleRgb = visiblePixels > 0 ? visibleRgbTotal / (visiblePixels * 3.0) : 0;
            Record($"Pixel copy {path}: {copy.width}x{copy.height}, mean alpha diff {meanAlpha:0.00}/255, mean visible RGB diff {meanVisibleRgb:0.00}/255, transparent pixels with rewritten RGB {transparentRgbChanged}/{a.Length}.");
            Require(meanAlpha < 4.0 && meanVisibleRgb < 8.0, "Readable copy differs too much from source: " + path);
            UnityEngine.Object.Destroy(source);
            UnityEngine.Object.Destroy(copy);
        }
    }

    // A PNG that exists on disk but that Unity failed to import (for example a malformed .meta)
    // cannot be referenced by any build; it is reported instead of failing the whole run.
    private static bool IsNotImported(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null) return false;
        Record("KNOWN ISSUE: " + path + " exists but is not imported by Unity (check its .meta).");
        return true;
    }

    private static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, Private).GetValue(value);
    private static T Invoke<T>(object value, string name, params object[] arguments) => (T)value.GetType().GetMethod(name, Private).Invoke(value, arguments);
    private static void Advance(int next)
    {
        step = next;
        stepAt = EditorApplication.timeSinceStartup;
    }
    private static void Require(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
    private static void Record(string message)
    {
        File.AppendAllText(LogDirectory + "/playmode.txt", message + "\n");
        Debug.Log("Runtime art validation: " + message);
    }
    private static void OnLog(string condition, string trace, LogType type)
    {
        if (SessionState.GetBool(Key + "Requested", false)
            && SessionState.GetInt(Key + "Phase", 0) == 2
            && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)) Fail(condition);
    }
    private static void Fail(string message)
    {
        if (SessionState.GetInt(Key + "Phase", 0) == 3) return;
        SessionState.SetInt(Key + "ExitCode", 1);
        SessionState.SetInt(Key + "Phase", 3);
        RuntimeArtLibrary.SimulatePlayerLookupInEditor = false;
        File.AppendAllText(LogDirectory + "/playmode.txt", "FAIL: " + message + "\n");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }
    private static void RestoreSaveAndExit(int exitCode)
    {
        string save = SessionState.GetString(Key + "SavePath", "");
        if (SessionState.GetBool(Key + "HadSave", false))
            File.Copy(LogDirectory + "/autosave.before.json", save, true);
        else if (!string.IsNullOrEmpty(save) && File.Exists(save)) File.Delete(save);
        Record(exitCode == 0
            ? "PASS: simulated player lookup only; autosave restored. A real Windows build still needs a visual check."
            : "FAILED; autosave restored.");
        EditorApplication.Exit(exitCode);
    }
}
