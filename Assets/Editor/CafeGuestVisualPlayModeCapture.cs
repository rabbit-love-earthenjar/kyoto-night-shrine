using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CafeGuestVisualPlayModeCapture
{
    private const string CaptureRequestedKey = "CafeGuestVisualPlayModeCapture.Requested";
    private const string CapturePhaseKey = "CafeGuestVisualPlayModeCapture.Phase";
    private const string ScenePath = "Assets/Scenes/CafeInterior_Temporary.unity";
    private const string ScreenshotPath = "Logs/cafe_guest_visual_play.png";
    private const float DiagnosticDuration = 10f;
    private const float MaximumHeightRatio = 1.35f;

    private static readonly Dictionary<int, VisibleHeightRange> heightRanges =
        new Dictionary<int, VisibleHeightRange>();

    private static int playFrames;
    private static float diagnosticStartTime;
    private static bool businessOpened;

    static CafeGuestVisualPlayModeCapture()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Kyoto Night Shrine/Validate Cafe Guest Visuals")]
    public static void CaptureGuestVisualsInPlayMode()
    {
        string screenshotDirectory = Path.GetDirectoryName(Path.GetFullPath(ScreenshotPath));

        if (!string.IsNullOrEmpty(screenshotDirectory))
        {
            Directory.CreateDirectory(screenshotDirectory);
        }

        string absoluteScreenshotPath = Path.GetFullPath(ScreenshotPath);

        if (File.Exists(absoluteScreenshotPath))
        {
            File.Delete(absoluteScreenshotPath);
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(CaptureRequestedKey, true);
        SessionState.SetInt(CapturePhaseKey, 0);
        ResetRuntimeState();
        Debug.Log("Cafe guest visual Play Mode capture requested.");
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(CaptureRequestedKey, false))
        {
            return;
        }

        int phase = SessionState.GetInt(CapturePhaseKey, 0);

        if (phase == 0 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetInt(CapturePhaseKey, 1);
            EditorApplication.EnterPlaymode();
            return;
        }

        if (phase <= 1 && EditorApplication.isPlaying)
        {
            SessionState.SetInt(CapturePhaseKey, 2);
            ResetRuntimeState();
        }

        if (SessionState.GetInt(CapturePhaseKey, 0) == 2 && EditorApplication.isPlaying)
        {
            RunPlayModeDiagnostic();
            return;
        }

        if (phase == 3 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.EraseBool(CaptureRequestedKey);
            SessionState.EraseInt(CapturePhaseKey);
            Debug.Log("Cafe guest visual Play Mode capture completed.");
            EditorApplication.Exit(0);
        }
    }

    private static void RunPlayModeDiagnostic()
    {
        playFrames++;

        if (playFrames == 8)
        {
            Screen.SetResolution(1280, 720, false);
        }

        CafeOperationController operationController = Object.FindAnyObjectByType<CafeOperationController>();

        if (!businessOpened && playFrames >= 15 && operationController != null)
        {
            operationController.RefreshVisitors();
            operationController.TryOpenForBusiness();
            businessOpened = true;
            diagnosticStartTime = Time.time;
            Debug.Log($"Cafe guest visual diagnostic opened business with {operationController.Guests.Count} visitors.");
        }

        if (!businessOpened)
        {
            if (playFrames > 180)
            {
                FailAndExit("CafeOperationController was not available to open the cafe.");
            }

            return;
        }

        TrackGuestVisuals();

        float elapsed = Time.time - diagnosticStartTime;

        if (elapsed >= DiagnosticDuration - 1f && elapsed < DiagnosticDuration - 0.8f)
        {
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(ScreenshotPath));
        }

        if (elapsed < DiagnosticDuration)
        {
            return;
        }

        ValidateGuestVisuals();
        SessionState.SetInt(CapturePhaseKey, 3);
        EditorApplication.ExitPlaymode();
    }

    private static void TrackGuestVisuals()
    {
        GameObject[] sceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        for (int i = 0; i < sceneObjects.Length; i++)
        {
            GameObject guestRoot = sceneObjects[i];

            if (!guestRoot.name.StartsWith("CafeGuestVisual_"))
            {
                continue;
            }

            Transform spriteVisual = guestRoot.transform.Find("GuestSpriteVisual");
            SpriteRenderer spriteRenderer = spriteVisual != null
                ? spriteVisual.GetComponent<SpriteRenderer>()
                : null;

            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                continue;
            }

            int instanceId = guestRoot.GetInstanceID();
            float visibleHeight = spriteRenderer.bounds.size.y;

            if (!heightRanges.TryGetValue(instanceId, out VisibleHeightRange range))
            {
                range = new VisibleHeightRange(guestRoot.name, visibleHeight);
            }

            range.Record(visibleHeight);
            heightRanges[instanceId] = range;
        }
    }

    private static void ValidateGuestVisuals()
    {
        GameObject[] sceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int guestCount = 0;

        for (int i = 0; i < sceneObjects.Length; i++)
        {
            GameObject guestRoot = sceneObjects[i];

            if (!guestRoot.name.StartsWith("CafeGuestVisual_"))
            {
                continue;
            }

            guestCount++;

            if ((guestRoot.transform.localScale - Vector3.one).sqrMagnitude > 0.0001f)
            {
                FailAndExit($"{guestRoot.name} changed its root scale to {guestRoot.transform.localScale}.");
                return;
            }

            Transform spriteVisual = guestRoot.transform.Find("GuestSpriteVisual");

            if (spriteVisual == null || spriteVisual.GetComponent<SpriteRenderer>() == null)
            {
                FailAndExit($"{guestRoot.name} is missing its isolated GuestSpriteVisual renderer.");
                return;
            }

            if (guestRoot.transform.Find("RequestBubble") == null)
            {
                FailAndExit($"{guestRoot.name} is missing its root-level request bubble.");
                return;
            }

            if (!heightRanges.TryGetValue(guestRoot.GetInstanceID(), out VisibleHeightRange range))
            {
                continue;
            }

            float ratio = range.Minimum > 0f ? range.Maximum / range.Minimum : 0f;
            Debug.Log(
                $"Cafe guest visual diagnostic: {range.Name}, minHeight={range.Minimum:0.000}, "
                + $"maxHeight={range.Maximum:0.000}, ratio={ratio:0.000}x.");

            if (ratio > MaximumHeightRatio)
            {
                FailAndExit($"{range.Name} visible height changed too much while walking ({ratio:0.000}x).");
                return;
            }
        }

        if (guestCount == 0)
        {
            FailAndExit("No cafe guest visuals appeared during the Play Mode diagnostic.");
            return;
        }

        Debug.Log($"Cafe guest visual diagnostic passed with {guestCount} seated visitor visuals.");
    }

    private static void FailAndExit(string message)
    {
        Debug.LogError($"Cafe guest visual diagnostic failed: {message}");
        SessionState.SetInt(CapturePhaseKey, 3);

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.ExitPlaymode();
        }
        else
        {
            EditorApplication.Exit(1);
        }
    }

    private static void ResetRuntimeState()
    {
        playFrames = 0;
        diagnosticStartTime = 0f;
        businessOpened = false;
        heightRanges.Clear();
    }

    private struct VisibleHeightRange
    {
        public string Name { get; }
        public float Minimum { get; private set; }
        public float Maximum { get; private set; }

        public VisibleHeightRange(string name, float initialHeight)
        {
            Name = name;
            Minimum = initialHeight;
            Maximum = initialHeight;
        }

        public void Record(float height)
        {
            Minimum = Mathf.Min(Minimum, height);
            Maximum = Mathf.Max(Maximum, height);
        }
    }
}
