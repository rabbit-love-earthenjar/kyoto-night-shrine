using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SaveManager : MonoBehaviour
{
    private const string SaveFileName = "autosave.json";
    private const string DefaultContinueScene = "HubMap_Day";
    private const string InitialScene = "Stage_1_2";
    private const string BgmVolumeKey = "Settings.BgmVolume";
    private const string SfxVolumeKey = "Settings.SfxVolume";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string TitleBgmPreferenceKey = "Settings.TitleBgmPreference";
    private const string ResolutionIndexKey = "Settings.ResolutionIndex";

    private static readonly string[] SafeSceneNames =
    {
        InitialScene,
        "Tutorial_00_BasicMove",
        DefaultContinueScene,
        "CafeInterior_Temporary"
    };

    public static SaveManager Instance { get; private set; }
    public static bool HasAutosave
    {
        get
        {
            SaveManager manager = EnsureInstance();
            return manager.currentData != null && manager.currentData.hasStarted;
        }
    }
    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    [SerializeField] private GameSaveData currentData;

    private float unsavedPlaySeconds;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static SaveManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        SaveManager existing = FindAnyObjectByType<SaveManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject saveObject = new GameObject("SaveManager");
        return saveObject.AddComponent<SaveManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAutosaveOrMigrateLegacyProgress();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        if (currentData != null && currentData.hasStarted)
        {
            unsavedPlaySeconds += Time.unscaledDeltaTime;
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            WriteAutosave();
        }
    }

    private void OnApplicationQuit()
    {
        WriteAutosave();
    }

    public static void StartNewGame(string initialSceneName = InitialScene)
    {
        SaveManager manager = EnsureInstance();
        manager.ResetGameplayPlayerPrefsPreservingSettings();
        HubMapController.ResetSessionProgress();
        ResourceInventory.Instance?.ResetForNewGame();

        string now = DateTime.UtcNow.ToString("O");
        manager.currentData = new GameSaveData
        {
            hasStarted = true,
            lastSafeSceneName = string.IsNullOrWhiteSpace(initialSceneName) ? InitialScene : initialSceneName,
            storyStageId = "Prologue",
            createdUtc = now,
            updatedUtc = now,
            totalPlaySeconds = 0f,
            usesLegacyPlayerPrefsBridge = true
        };
        manager.unsavedPlaySeconds = 0f;
        manager.WriteAutosave();
    }

    public static bool TryGetContinueScene(string fallbackSceneName, out string sceneName)
    {
        SaveManager manager = EnsureInstance();
        sceneName = string.Empty;

        if (manager.currentData == null || !manager.currentData.hasStarted)
        {
            return false;
        }

        string savedScene = manager.currentData.lastSafeSceneName;
        if (!string.IsNullOrWhiteSpace(savedScene) && Application.CanStreamedLevelBeLoaded(savedScene))
        {
            sceneName = savedScene;
            return true;
        }

        string fallback = string.IsNullOrWhiteSpace(fallbackSceneName)
            ? DefaultContinueScene
            : fallbackSceneName;
        if (Application.CanStreamedLevelBeLoaded(fallback))
        {
            sceneName = fallback;
            return true;
        }

        Debug.LogError("Autosave exists, but neither its safe scene nor HubMap_Day is enabled in Build Settings.");
        return false;
    }

    public static void NotifyLegacyProgressChanged()
    {
        SaveManager manager = EnsureInstance();
        if (manager.currentData != null && manager.currentData.hasStarted)
        {
            manager.WriteAutosave();
        }
    }

    public static void RecordCheckpoint(string safeSceneName, string storyStageId = null)
    {
        SaveManager manager = EnsureInstance();
        if (manager.currentData == null || !manager.currentData.hasStarted)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(safeSceneName))
        {
            manager.currentData.lastSafeSceneName = safeSceneName;
        }

        if (!string.IsNullOrWhiteSpace(storyStageId))
        {
            manager.currentData.storyStageId = storyStageId;
        }

        manager.WriteAutosave();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (currentData == null || !currentData.hasStarted || !IsSafeScene(scene.name))
        {
            return;
        }

        currentData.lastSafeSceneName = scene.name;
        WriteAutosave();
    }

    private void LoadAutosaveOrMigrateLegacyProgress()
    {
        currentData = ReadAutosave();
        if (currentData != null && currentData.hasStarted)
        {
            if (UpgradeSaveData(currentData))
            {
                WriteAutosave();
            }
            return;
        }

        if (!HasLegacyProgress())
        {
            currentData = null;
            return;
        }

        string now = DateTime.UtcNow.ToString("O");
        currentData = new GameSaveData
        {
            hasStarted = true,
            lastSafeSceneName = DefaultContinueScene,
            storyStageId = "LegacyProgress",
            createdUtc = now,
            updatedUtc = now,
            usesLegacyPlayerPrefsBridge = true
        };
        WriteAutosave();
        Debug.Log("Migrated existing Night Shrine PlayerPrefs progression into the autosave slot.", this);
    }

    private GameSaveData ReadAutosave()
    {
        string path = SaveFilePath;
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            return data != null && data.saveVersion > 0 ? data : null;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to read autosave '{path}': {exception.Message}", this);
            return null;
        }
    }

    private static bool UpgradeSaveData(GameSaveData data)
    {
        bool changed = false;

        if (data.saveVersion < 2)
        {
            if (data.storyStageId == "Prologue"
                && data.lastSafeSceneName == "Tutorial_00_BasicMove")
            {
                data.lastSafeSceneName = InitialScene;
            }

            data.saveVersion = 2;
            changed = true;
        }

        return changed;
    }

    private void WriteAutosave()
    {
        if (currentData == null || !currentData.hasStarted)
        {
            return;
        }

        currentData.totalPlaySeconds += Mathf.Max(0f, unsavedPlaySeconds);
        unsavedPlaySeconds = 0f;
        currentData.updatedUtc = DateTime.UtcNow.ToString("O");

        string path = SaveFilePath;
        string temporaryPath = path + ".tmp";

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(currentData, true));

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(temporaryPath, path);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to write autosave '{path}': {exception.Message}", this);
        }
    }

    private void ResetGameplayPlayerPrefsPreservingSettings()
    {
        float bgmVolume = GameSettings.BgmVolume;
        float sfxVolume = GameSettings.SfxVolume;
        bool fullscreen = GameSettings.IsFullscreen;
        int titleBgmPreference = (int)GameSettings.PreferredTitleBgm;
        int resolutionIndex = GameSettings.ResolutionIndex;

        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(TitleBgmPreferenceKey, titleBgmPreference);
        PlayerPrefs.SetInt(ResolutionIndexKey, resolutionIndex);
        PlayerPrefs.Save();
    }

    private static bool HasLegacyProgress()
    {
        return PlayerPrefs.HasKey("ResourceInventory.FaithPoints")
            || PlayerPrefs.HasKey("HubMap_Day.ShrineRepaired")
            || PlayerPrefs.HasKey("CafeFoxAltar.Level")
            || PlayerPrefs.HasKey("ResourceInventory.CafeStarterIngredientsInitialized")
            || PlayerPrefs.HasKey("ResourceInventory.FarmStarterSeedsInitialized");
    }

    private static bool IsSafeScene(string sceneName)
    {
        for (int index = 0; index < SafeSceneNames.Length; index++)
        {
            if (sceneName == SafeSceneNames[index])
            {
                return true;
            }
        }

        return false;
    }
}
