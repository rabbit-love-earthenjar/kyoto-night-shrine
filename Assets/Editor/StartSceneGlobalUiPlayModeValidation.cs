using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StartSceneGlobalUiPlayModeValidation
{
    private const string RequestedKey = "StartSceneGlobalUiValidation.Requested";
    private const string PhaseKey = "StartSceneGlobalUiValidation.Phase";
    private const string ScenePath = "Assets/Scenes/StartScene.unity";
    private const string SettingsScreenshotPath = "Logs/start_scene_settings.png";
    private const string CreditsScreenshotPath = "Logs/start_scene_credits.png";
    private static int playFrames;
    private static StartSceneAuxiliaryMenuController auxiliaryMenu;
    private static RectTransform selectedSettingsLabel;
    private static Vector2 selectedSettingsLabelPosition;
    private static int menuOpenedFrame = -1;

    static StartSceneGlobalUiPlayModeValidation()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    public static void ValidateInPlayMode()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        DeleteScreenshot(SettingsScreenshotPath);
        DeleteScreenshot(CreditsScreenshotPath);
        SessionState.SetBool(RequestedKey, true);
        SessionState.SetInt(PhaseKey, 0);
        playFrames = 0;
        auxiliaryMenu = null;
        selectedSettingsLabel = null;
        menuOpenedFrame = -1;
        Debug.Log("StartScene global UI Play Mode validation requested.");
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(RequestedKey, false))
        {
            return;
        }

        int phase = SessionState.GetInt(PhaseKey, 0);
        if (phase == 0 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetInt(PhaseKey, 1);
            EditorApplication.EnterPlaymode();
            return;
        }

        if (phase <= 1 && EditorApplication.isPlaying)
        {
            SessionState.SetInt(PhaseKey, 2);
            playFrames = 0;
        }

        if (SessionState.GetInt(PhaseKey, 0) == 2 && EditorApplication.isPlaying)
        {
            playFrames++;
            if (menuOpenedFrame < 0 && playFrames >= 120)
            {
                if (!ValidateRuntimeState())
                {
                    return;
                }

                auxiliaryMenu = Object.FindAnyObjectByType<StartSceneAuxiliaryMenuController>();
                if (auxiliaryMenu == null)
                {
                    Finish(false, "auxiliary settings/credits controller was not created.");
                    return;
                }

                StartScreenController startScreen = Object.FindAnyObjectByType<StartScreenController>();
                if (startScreen == null)
                {
                    Finish(false, "StartScreenController was not found.");
                    return;
                }

                if (!startScreen.CanShowMenu)
                {
                    if (playFrames > 900)
                    {
                        Finish(false, "title screen did not become ready for input.");
                    }
                    return;
                }

                startScreen.ShowMenu();
                if (!startScreen.IsMenuVisible)
                {
                    Finish(false, "title screen did not leave PRESS START state.");
                    return;
                }

                auxiliaryMenu.ShowSettings();
                menuOpenedFrame = playFrames;
            }
            else if (menuOpenedFrame >= 0 && playFrames == menuOpenedFrame + 2)
            {
                if (!auxiliaryMenu.IsOpen
                    || GameObject.Find("SettingsPanel") == null
                    || GameObject.Find("BgmVolume") == null
                    || GameObject.Find("SfxVolume") == null
                    || GameObject.Find("TitleBgmButton") == null
                    || GameObject.Find("ResolutionButton") == null
                    || GameObject.Find("DisplayModeButton") == null)
                {
                    Finish(false, "settings panel controls were not all active.");
                    return;
                }

                GameObject resolutionButtonObject = GameObject.Find("ResolutionButton");
                Button resolutionButton = resolutionButtonObject != null ? resolutionButtonObject.GetComponent<Button>() : null;
                Text resolutionLabel = resolutionButton != null ? resolutionButton.GetComponentInChildren<Text>() : null;
                if (resolutionButton == null || resolutionLabel == null || EventSystem.current == null)
                {
                    Finish(false, "could not select the resolution control for label-position validation.");
                    return;
                }

                selectedSettingsLabel = resolutionLabel.rectTransform;
                selectedSettingsLabelPosition = selectedSettingsLabel.anchoredPosition;
                EventSystem.current.SetSelectedGameObject(resolutionButton.gameObject);
            }
            else if (menuOpenedFrame >= 0 && playFrames == menuOpenedFrame + 10)
            {
                if (selectedSettingsLabel == null
                    || Vector2.Distance(selectedSettingsLabel.anchoredPosition, selectedSettingsLabelPosition) > 0.1f)
                {
                    Finish(false, "selected settings-button text shifted away from its fixed anchor.");
                    return;
                }

                CaptureStartCanvas(SettingsScreenshotPath);
                auxiliaryMenu.ShowCredits();
            }
            else if (menuOpenedFrame >= 0 && playFrames == menuOpenedFrame + 12)
            {
                if (!auxiliaryMenu.IsOpen || GameObject.Find("CreditsPanel") == null)
                {
                    Finish(false, "credits panel did not become active.");
                    return;
                }

                CaptureStartCanvas(CreditsScreenshotPath);
                auxiliaryMenu.Close();
                Finish(true, "single EventSystem ownership plus settings and credits panel switching passed.");
            }
            return;
        }

        if (phase == 3 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.EraseBool(RequestedKey);
            SessionState.EraseInt(PhaseKey);
            Debug.Log("StartScene global UI Play Mode validation completed.");
            EditorApplication.Exit(0);
        }
    }

    private static bool ValidateRuntimeState()
    {
        EventSystem[] activeSystems = Object.FindObjectsByType<EventSystem>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        bool valid = activeSystems.Length == 1
            && activeSystems[0] != null
            && activeSystems[0].enabled
            && activeSystems[0].gameObject.activeInHierarchy
            && EventSystem.current == activeSystems[0]
            && activeSystems[0].name == "GlobalEventSystem";

        if (!valid)
        {
            string currentName = EventSystem.current != null ? EventSystem.current.name : "none";
            Finish(false, $"active EventSystems={activeSystems.Length}, current={currentName}.");
            return false;
        }

        return true;
    }

    private static void Finish(bool succeeded, string message)
    {
        SessionState.SetInt(PhaseKey, 3);
        if (succeeded)
        {
            Debug.Log($"StartScene global UI validation passed: {message}");
        }
        else
        {
            Debug.LogError($"StartScene global UI validation failed: {message}");
        }

        EditorApplication.ExitPlaymode();
    }

    private static void CaptureStartCanvas(string relativePath)
    {
        StartScreenController startScreen = Object.FindAnyObjectByType<StartScreenController>();
        Canvas canvas = startScreen != null ? startScreen.GetComponent<Canvas>() : null;
        Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        if (canvas == null || camera == null)
        {
            Finish(false, $"could not capture '{relativePath}' because the title Canvas or camera is missing.");
            return;
        }

        string absolutePath = Path.GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        RenderMode previousMode = canvas.renderMode;
        Camera previousCanvasCamera = canvas.worldCamera;
        float previousPlaneDistance = canvas.planeDistance;
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);

        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
            texture.Apply();
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCanvasCamera;
            canvas.planeDistance = previousPlaneDistance;
            camera.targetTexture = previousTarget;
            RenderTexture.active = null;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(renderTexture);
        }
    }

    private static void DeleteScreenshot(string relativePath)
    {
        string absolutePath = Path.GetFullPath(relativePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
    }
}
