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
    private const string ScreenshotPath = "Logs/route_v23_goal_play.png";
    private static int playFrames;
    private static float redOniStartX;
    private static bool hasRedOniStart;
    private static float redOniMinimumX;
    private static float redOniMaximumX;
    private static bool sawRedOniFacingLeft;
    private static bool sawRedOniFacingRight;
    private static float redOniStartGameTime;
    private static bool patrolDiagnosticComplete;

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
        hasRedOniStart = false;
        redOniMinimumX = float.PositiveInfinity;
        redOniMaximumX = float.NegativeInfinity;
        sawRedOniFacingLeft = false;
        sawRedOniFacingRight = false;
        redOniStartGameTime = 0f;
        patrolDiagnosticComplete = false;
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

        camera.transform.position = new Vector3(6.75f, 2.25f, camera.transform.position.z);
        camera.orthographicSize = 4.25f;

        if (playFrames == 8)
        {
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();

            if (player != null)
            {
                player.transform.position = new Vector3(9.2f, 1.1f, player.transform.position.z);
                Rigidbody2D body = player.GetComponent<Rigidbody2D>();

                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                }
            }
        }

        GameObject oni = GameObject.Find("RedOni_GoalForeshadow");

        if (!hasRedOniStart && playFrames >= 10 && oni != null)
        {
            Time.timeScale = 8f;
            redOniStartX = oni.transform.position.x;
            redOniMinimumX = redOniStartX;
            redOniMaximumX = redOniStartX;
            redOniStartGameTime = Time.time;
            hasRedOniStart = true;
        }

        if (hasRedOniStart && oni != null)
        {
            redOniMinimumX = Mathf.Min(redOniMinimumX, oni.transform.position.x);
            redOniMaximumX = Mathf.Max(redOniMaximumX, oni.transform.position.x);
            VisualPatrolMotion currentPatrol = oni.GetComponent<VisualPatrolMotion>();

            if (currentPatrol != null)
            {
                sawRedOniFacingLeft |= currentPatrol.IsFacingLeft;
                sawRedOniFacingRight |= !currentPatrol.IsFacingLeft;
            }
        }

        if (playFrames == 30)
        {
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            float playerHeight = MeasureVisibleHeight(player != null ? player.gameObject : null);
            float oniHeight = MeasureVisibleHeight(oni);
            float ratio = playerHeight > 0f ? oniHeight / playerHeight : 0f;
            Debug.Log($"Stage route V23 size diagnostic: Player={playerHeight:0.00}, RedOni={oniHeight:0.00}, Ratio={ratio:0.00}x.");
        }

        if (playFrames == 20)
        {
            Screen.SetResolution(1280, 720, false);
        }

        if (playFrames == 45)
        {
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(ScreenshotPath));
            Debug.Log("Stage route V23 Play Mode screenshot requested.");
        }

        if (!patrolDiagnosticComplete
            && hasRedOniStart
            && Time.time - redOniStartGameTime >= 7.5f)
        {
            if (!hasRedOniStart || oni == null)
            {
                FailAndExit("Red Oni was not available for the patrol movement diagnostic.");
                return;
            }

            float patrolSpan = redOniMaximumX - redOniMinimumX;
            VisualPatrolMotion patrol = oni.GetComponent<VisualPatrolMotion>();
            Debug.Log(
                $"Stage route V23 patrol diagnostic: StartX={redOniStartX:0.00}, "
                + $"CurrentX={oni.transform.position.x:0.00}, MinX={redOniMinimumX:0.00}, "
                + $"MaxX={redOniMaximumX:0.00}, Span={patrolSpan:0.00}, "
                + $"SawLeft={sawRedOniFacingLeft}, SawRight={sawRedOniFacingRight}, "
                + $"FacingLeftNow={patrol != null && patrol.IsFacingLeft}.");

            if (patrolSpan < 2.3f || !sawRedOniFacingLeft || !sawRedOniFacingRight)
            {
                FailAndExit(
                    $"Red Oni did not complete a readable two-way patrol during Play Mode capture "
                    + $"(span {patrolSpan:0.00}, left {sawRedOniFacingLeft}, right {sawRedOniFacingRight}).");
                return;
            }

            patrolDiagnosticComplete = true;
        }

        if (!patrolDiagnosticComplete)
        {
            if (playFrames > 3600)
            {
                FailAndExit("Red Oni patrol diagnostic timed out before 7.5 seconds of game time elapsed.");
            }

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

        Debug.Log($"Stage route V23 Play Mode screenshot saved: {absolutePath}");
        Time.timeScale = 1f;
        SessionState.SetInt(CapturePhaseKey, 3);
        EditorApplication.ExitPlaymode();
    }

    private static void FailAndExit(string message)
    {
        Debug.LogError(message);
        Time.timeScale = 1f;
        SessionState.SetInt(CapturePhaseKey, 3);

        if (EditorApplication.isPlaying)
        {
            EditorApplication.ExitPlaymode();
        }
    }

    private static float MeasureVisibleHeight(GameObject target)
    {
        if (target == null)
        {
            return 0f;
        }

        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        Bounds bounds = default;
        bool hasBounds = false;

        foreach (SpriteRenderer renderer in renderers)
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.sprite == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? bounds.size.y : 0f;
    }
}
