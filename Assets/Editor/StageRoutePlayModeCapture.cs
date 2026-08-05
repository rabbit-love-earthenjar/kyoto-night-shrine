using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class StageRoutePlayModeCapture
{
    private const string CaptureRequestedKey = "StageRoutePlayModeCapture.Requested";
    private const string CapturePhaseKey = "StageRoutePlayModeCapture.Phase";
    private const string ScenePath = "Assets/Scenes/Stage_1_Route_Prototype.unity";
    private const string ScreenshotPath = "Logs/route_v19_goal_play.png";
    private static int playFrames;

    static StageRoutePlayModeCapture()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    public static void CaptureGoalInPlayMode()
    {
        string absolutePath = Path.GetFullPath(ScreenshotPath);

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(CaptureRequestedKey, true);
        SessionState.SetInt(CapturePhaseKey, 0);
        playFrames = 0;
        Debug.Log("Stage route Play Mode capture requested.");
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
            playFrames = 0;
        }

        if (SessionState.GetInt(CapturePhaseKey, 0) == 2 && EditorApplication.isPlaying)
        {
            CapturePlayFrameWhenReady();
            return;
        }

        if (phase == 3 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.EraseBool(CaptureRequestedKey);
            SessionState.EraseInt(CapturePhaseKey);
            Debug.Log("Stage route Play Mode capture completed.");
            EditorApplication.Exit(0);
        }
    }

    private static void CapturePlayFrameWhenReady()
    {
        playFrames++;
        Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();

        if (camera == null)
        {
            if (playFrames > 120)
            {
                FailAndExit("Main Camera was not available during Play Mode capture.");
            }

            return;
        }

        CameraFollow cameraFollow = camera.GetComponent<CameraFollow>();

        if (cameraFollow != null)
        {
            cameraFollow.enabled = false;
        }

        camera.transform.position = new Vector3(10.5f, 2.5f, camera.transform.position.z);
        camera.orthographicSize = 5.25f;

        if (playFrames == 20)
        {
            Screen.SetResolution(1280, 720, false);
        }

        if (playFrames == 45)
        {
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(ScreenshotPath));
            Debug.Log("Stage route V19 Play Mode screenshot requested.");
        }

        if (playFrames < 90)
        {
            return;
        }

        string absolutePath = Path.GetFullPath(ScreenshotPath);

        if (!File.Exists(absolutePath) || new FileInfo(absolutePath).Length == 0)
        {
            if (playFrames > 240)
            {
                FailAndExit("Play Mode screenshot was not written within the expected frame window.");
            }

            return;
        }

        Debug.Log($"Stage route V19 Play Mode screenshot saved: {absolutePath}");
        SessionState.SetInt(CapturePhaseKey, 3);
        EditorApplication.ExitPlaymode();
    }

    private static void FailAndExit(string message)
    {
        Debug.LogError(message);
        SessionState.SetInt(CapturePhaseKey, 3);

        if (EditorApplication.isPlaying)
        {
            EditorApplication.ExitPlaymode();
        }
    }
}
