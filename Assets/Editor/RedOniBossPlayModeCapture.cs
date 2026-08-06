using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RedOniBossPlayModeCapture
{
    private const string RequestedKey = "RedOniBossPlayModeCapture.Requested";
    private const string PhaseKey = "RedOniBossPlayModeCapture.Phase";
    private const string ScenePath = "Assets/Scenes/Stage_1_Boss_RedOni.unity";
    private const string ScreenshotPath = "Logs/red_oni_boss_phase1_play.png";

    private static int playFrames;
    private static bool sawTelegraph;
    private static bool screenshotRequested;
    private static float playStartRealtime;
    private static Rigidbody2D platformProbe;
    private static BoxCollider2D platformUnderTest;
    private static int platformProbePhase;
    private static float platformProbePhaseStart;
    private static bool passedPlatformFromBelow;
    private static bool landedOnPlatformFromAbove;
    private static bool fallRecoveryTestStarted;
    private static bool fallRecoveryDamagePassed;
    private static int fallRecoveryStartHp;
    private static float fallRecoveryTestStart;
    private static float minimumBossVisualHeight;
    private static float maximumBossVisualHeight;

    static RedOniBossPlayModeCapture()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    public static void CapturePhaseOnePlayMode()
    {
        string absolutePath = Path.GetFullPath(ScreenshotPath);

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(RequestedKey, true);
        SessionState.SetInt(PhaseKey, 0);
        playFrames = 0;
        sawTelegraph = false;
        screenshotRequested = false;
        playStartRealtime = 0f;
        ResetPlatformProbeState();
        Debug.Log("Red Oni boss Phase 1 Play Mode capture requested.");
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
            playStartRealtime = Time.realtimeSinceStartup;
        }

        if (SessionState.GetInt(PhaseKey, 0) == 2 && EditorApplication.isPlaying)
        {
            ObservePlayMode();
            return;
        }

        if (phase == 3 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.EraseBool(RequestedKey);
            SessionState.EraseInt(PhaseKey);
            Debug.Log("Red Oni boss Phase 1 Play Mode capture completed.");
            EditorApplication.Exit(0);
        }
    }

    private static void ObservePlayMode()
    {
        playFrames++;
        Time.timeScale = 3f;

        RedOniPhaseOneController boss = Object.FindFirstObjectByType<RedOniPhaseOneController>();
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();

        if (boss == null || player == null)
        {
            if (ElapsedRealtime > 8f)
            {
                FailAndExit("Boss or player was not available in the Phase 1 Play Mode check.");
            }

            return;
        }

        if (player.transform.position.y < -5.5f)
        {
            FailAndExit($"Player fell out of the boss arena before input: y={player.transform.position.y:0.00}.");
            return;
        }

        sawTelegraph |= boss.IsAttacking;
        SampleBossVisualHeight(boss);
        ValidateOneWayPlatformPhysics();

        if (!screenshotRequested && boss.IsAttacking && playFrames > 20)
        {
            CaptureCamera(Path.GetFullPath(ScreenshotPath));
            screenshotRequested = true;
            Debug.Log($"Red Oni boss screenshot requested during {boss.CurrentLane} lane attack.");
        }

        if (boss.CompletedAttackCount < 2)
        {
            if (ElapsedRealtime > 24f)
            {
                FailAndExit("Red Oni did not complete two attacks within the expected Play Mode window.");
            }

            return;
        }

        if (!sawTelegraph)
        {
            FailAndExit("No Red Oni telegraph state was observed before the completed attacks.");
            return;
        }

        if (!passedPlatformFromBelow || !landedOnPlatformFromAbove)
        {
            if (ElapsedRealtime > 26f)
            {
                FailAndExit(
                    $"Boss one-way platform physics did not complete: "
                    + $"passedUp={passedPlatformFromBelow}, caughtDown={landedOnPlatformFromAbove}.");
            }

            return;
        }

        if (!ValidateFallRecoveryDamage(boss, player))
        {
            return;
        }

        if (maximumBossVisualHeight - minimumBossVisualHeight > 0.15f)
        {
            FailAndExit(
                $"Red Oni visual height changed across attack frames: "
                + $"min={minimumBossVisualHeight:0.00}, max={maximumBossVisualHeight:0.00}.");
            return;
        }

        string absolutePath = Path.GetFullPath(ScreenshotPath);

        if (!File.Exists(absolutePath) || new FileInfo(absolutePath).Length == 0)
        {
            if (ElapsedRealtime > 26f)
            {
                FailAndExit("Red Oni boss screenshot was not written.");
            }

            return;
        }

        Debug.Log(
            $"Red Oni boss Play Mode validation passed: attacks={boss.CompletedAttackCount}, "
            + $"telegraphObserved={sawTelegraph}, oneWayUp={passedPlatformFromBelow}, "
            + $"solidFromAbove={landedOnPlatformFromAbove}, fallDamage={fallRecoveryDamagePassed}, "
            + $"bossHeight={minimumBossVisualHeight:0.00}-{maximumBossVisualHeight:0.00}, "
            + $"playerY={player.transform.position.y:0.00}.");
        Time.timeScale = 1f;
        SessionState.SetInt(PhaseKey, 3);
        EditorApplication.ExitPlaymode();
    }

    private static void FailAndExit(string message)
    {
        Debug.LogError(message);
        Time.timeScale = 1f;
        SessionState.SetInt(PhaseKey, 3);

        if (EditorApplication.isPlaying)
        {
            EditorApplication.ExitPlaymode();
        }
    }

    private static float ElapsedRealtime => playStartRealtime > 0f
        ? Time.realtimeSinceStartup - playStartRealtime
        : 0f;

    private static void ValidateOneWayPlatformPhysics()
    {
        if (landedOnPlatformFromAbove)
        {
            return;
        }

        if (platformProbe == null)
        {
            GameObject platformObject = GameObject.Find("Platform_L2");
            platformUnderTest = platformObject != null ? platformObject.GetComponent<BoxCollider2D>() : null;

            if (platformUnderTest == null)
            {
                return;
            }

            GameObject probeObject = new GameObject("OneWayPlatformValidationProbe");
            probeObject.transform.position = new Vector3(
                platformUnderTest.bounds.center.x,
                platformUnderTest.bounds.min.y - 0.8f,
                0f);

            CircleCollider2D probeCollider = probeObject.AddComponent<CircleCollider2D>();
            probeCollider.radius = 0.18f;
            platformProbe = probeObject.AddComponent<Rigidbody2D>();
            platformProbe.gravityScale = 0f;
            platformProbe.freezeRotation = true;
            platformProbe.linearVelocity = new Vector2(0f, 6f);
            platformProbePhase = 1;
            platformProbePhaseStart = Time.realtimeSinceStartup;
            return;
        }

        float platformTop = platformUnderTest.bounds.max.y;

        if (platformProbePhase == 1)
        {
            if (platformProbe.position.y > platformTop + 0.45f)
            {
                passedPlatformFromBelow = true;
                platformProbe.position = new Vector2(platformUnderTest.bounds.center.x, platformTop + 0.9f);
                platformProbe.linearVelocity = new Vector2(0f, -5f);
                platformProbePhase = 2;
                platformProbePhaseStart = Time.realtimeSinceStartup;
            }
            else if (Time.realtimeSinceStartup - platformProbePhaseStart > 3f)
            {
                FailAndExit("The Boss platform blocked the validation probe rising from below.");
            }

            return;
        }

        if (platformProbePhase == 2)
        {
            float expectedRestY = platformTop + 0.18f;

            if (Mathf.Abs(platformProbe.linearVelocity.y) < 0.05f
                && Mathf.Abs(platformProbe.position.y - expectedRestY) < 0.12f)
            {
                landedOnPlatformFromAbove = true;
                Object.Destroy(platformProbe.gameObject);
                platformProbe = null;
                Debug.Log("Boss one-way platform physics passed: upward traversal and solid top landing confirmed.");
            }
            else if (platformProbe.position.y < platformTop - 0.25f
                || Time.realtimeSinceStartup - platformProbePhaseStart > 3f)
            {
                FailAndExit("The Boss platform did not catch the validation probe falling from above.");
            }
        }
    }

    private static void ResetPlatformProbeState()
    {
        platformProbe = null;
        platformUnderTest = null;
        platformProbePhase = 0;
        platformProbePhaseStart = 0f;
        passedPlatformFromBelow = false;
        landedOnPlatformFromAbove = false;
        fallRecoveryTestStarted = false;
        fallRecoveryDamagePassed = false;
        fallRecoveryStartHp = 0;
        fallRecoveryTestStart = 0f;
        minimumBossVisualHeight = float.PositiveInfinity;
        maximumBossVisualHeight = 0f;
    }

    private static void SampleBossVisualHeight(RedOniPhaseOneController boss)
    {
        RedOniVisualHeightNormalizer normalizer = boss.GetComponentInChildren<RedOniVisualHeightNormalizer>();
        SpriteRenderer renderer = normalizer != null ? normalizer.GetComponent<SpriteRenderer>() : null;

        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        float height = renderer.bounds.size.y;

        // Animator initialization can expose one pre-render frame with empty
        // bounds. It is not a displayed attack frame and should not affect the
        // visual consistency measurement.
        if (height <= 0.1f)
        {
            return;
        }

        minimumBossVisualHeight = minimumBossVisualHeight <= 0f
            ? height
            : Mathf.Min(minimumBossVisualHeight, height);
        maximumBossVisualHeight = Mathf.Max(maximumBossVisualHeight, height);
    }

    private static bool ValidateFallRecoveryDamage(
        RedOniPhaseOneController boss,
        PlayerController player)
    {
        if (fallRecoveryDamagePassed)
        {
            return true;
        }

        PlayerHealth health = player.GetComponent<PlayerHealth>();

        if (health == null)
        {
            FailAndExit("PlayerHealth was unavailable for the Boss fall recovery check.");
            return false;
        }

        if (!fallRecoveryTestStarted)
        {
            if (health.IsInvincible)
            {
                return false;
            }

            if (health.CurrentHP <= 1)
            {
                health.ResetHealth();
            }

            boss.SetEncounterActive(false);
            fallRecoveryStartHp = health.CurrentHP;
            fallRecoveryTestStart = Time.realtimeSinceStartup;
            fallRecoveryTestStarted = true;

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();

            if (body != null)
            {
                body.position = new Vector2(0f, -5.2f);
                body.linearVelocity = Vector2.zero;
            }
            else
            {
                player.transform.position = new Vector3(0f, -5.2f, 0f);
            }

            return false;
        }

        if (health.CurrentHP == fallRecoveryStartHp - 1 && player.transform.position.y > -3f)
        {
            fallRecoveryDamagePassed = true;
            Debug.Log("Boss fall recovery passed: one HP removed and player returned to a safe platform.");
            return true;
        }

        if (health.CurrentHP < fallRecoveryStartHp - 1)
        {
            FailAndExit("Boss fall recovery removed more than one HP.");
            return false;
        }

        if (Time.realtimeSinceStartup - fallRecoveryTestStart > 3f)
        {
            FailAndExit(
                $"Boss fall recovery did not complete: startHP={fallRecoveryStartHp}, "
                + $"currentHP={health.CurrentHP}, playerY={player.transform.position.y:0.00}.");
        }

        return false;
    }

    private static void CaptureCamera(string absolutePath)
    {
        Camera camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();

        if (camera == null)
        {
            throw new System.InvalidOperationException("Main Camera is unavailable for boss capture.");
        }

        const int width = 1280;
        const int height = 720;
        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;

        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(texture);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
        }
    }
}
