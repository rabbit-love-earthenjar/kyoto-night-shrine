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
    private static bool faithBeanTestStarted;
    private static bool faithBeanDamagePassed;
    private static int faithBeanStartingHp;
    private static float faithBeanTestStart;
    private static bool phaseThresholdPassed;
    private static bool phaseTwoSmashPassed;
    private static bool phaseThreeAddsPassed;
    private static GameObject phaseThreeBeanTarget;
    private static bool phaseThreeBeanKillPassed;
    private static int phaseThreeBeanStartingEnemyCount;
    private static float phaseThreeBeanTestStart;
    private static float phaseThreeNextBeanShotTime;
    private static Collider2D[] phaseThreeBossColliders;
    private static bool[] phaseThreeBossColliderStates;
    private static bool sawBrokenPlatform;
    private static int phaseTwoSmashStartCount;
    private static float phaseThreeTestStart;

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

        bool canCaptureScreenshot = !Application.isBatchMode
            && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;

        if (canCaptureScreenshot && !screenshotRequested && boss.IsAttacking && playFrames > 20)
        {
            CaptureCamera(Path.GetFullPath(ScreenshotPath));
            screenshotRequested = true;
            Debug.Log($"Red Oni boss screenshot requested during {boss.CurrentLane} lane attack.");
        }

        if (!ValidateFaithBeanDamage(boss, player))
        {
            return;
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

        if (!ValidatePhaseProgression(boss))
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

        if (canCaptureScreenshot
            && (!File.Exists(absolutePath) || new FileInfo(absolutePath).Length == 0))
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
            + $"faithBeanDamage={faithBeanDamagePassed}, "
            + $"phaseProgression={phaseThresholdPassed}, "
            + $"phaseThreeAdds={phaseThreeAddsPassed}, "
            + $"bossHeight={minimumBossVisualHeight:0.00}-{maximumBossVisualHeight:0.00}, "
            + $"playerY={player.transform.position.y:0.00}.");
        Time.timeScale = 1f;
        SessionState.SetInt(PhaseKey, 3);
        EditorApplication.ExitPlaymode();
    }

    private static void FailAndExit(string message)
    {
        RestorePhaseThreeBossColliders();
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
        faithBeanTestStarted = false;
        faithBeanDamagePassed = false;
        faithBeanStartingHp = 0;
        faithBeanTestStart = 0f;
        phaseThresholdPassed = false;
        phaseTwoSmashPassed = false;
        phaseThreeAddsPassed = false;
        phaseThreeBeanTarget = null;
        phaseThreeBeanKillPassed = false;
        phaseThreeBeanStartingEnemyCount = 0;
        phaseThreeBeanTestStart = 0f;
        phaseThreeNextBeanShotTime = 0f;
        phaseThreeBossColliders = null;
        phaseThreeBossColliderStates = null;
        sawBrokenPlatform = false;
        phaseTwoSmashStartCount = 0;
        phaseThreeTestStart = 0f;
    }

    private static bool ValidateFaithBeanDamage(
        RedOniPhaseOneController boss,
        PlayerController player)
    {
        if (faithBeanDamagePassed)
        {
            return true;
        }

        FaithBeanShooter shooter = player.GetComponent<FaithBeanShooter>();
        RedOniBossHealth health = boss.GetComponent<RedOniBossHealth>();
        Collider2D hitTrigger = health != null
            ? health.GetComponentInChildren<Collider2D>(true)
            : null;

        if (shooter == null || health == null || hitTrigger == null)
        {
            FailAndExit("Faith Bean shooter, Boss health, or Boss hit trigger was unavailable.");
            return false;
        }

        if (!faithBeanTestStarted)
        {
            faithBeanStartingHp = health.CurrentHP;
            faithBeanTestStart = Time.realtimeSinceStartup;
            faithBeanTestStarted = shooter.TryFireAt(hitTrigger.bounds.center);

            if (faithBeanTestStarted)
            {
                Debug.Log("Faith Bean validation shot fired at the current Red Oni visual position.");
            }

            return false;
        }

        if (health.CurrentHP == faithBeanStartingHp - 1)
        {
            GameObject fillObject = GameObject.Find("BossHealthFill");
            RectTransform fillRect = fillObject != null
                ? fillObject.GetComponent<RectTransform>()
                : null;
            float expectedRatio = health.CurrentHP / (float)health.MaxHP;

            if (fillRect == null || Mathf.Abs(fillRect.anchorMax.x - expectedRatio) > 0.005f)
            {
                FailAndExit(
                    $"Boss HP fill did not visually shrink after damage: "
                    + $"expected={expectedRatio:0.000}, "
                    + $"actual={(fillRect != null ? fillRect.anchorMax.x : -1f):0.000}.");
                return false;
            }

            faithBeanDamagePassed = true;
            Debug.Log(
                $"Faith Bean damage passed: Red Oni lost exactly one HP and the bar shrank "
                + $"to {fillRect.anchorMax.x:0.000} width.");
            return true;
        }

        if (health.CurrentHP < faithBeanStartingHp - 1)
        {
            FailAndExit("Faith Bean validation shot removed more than one Boss HP.");
            return false;
        }

        if (Time.realtimeSinceStartup - faithBeanTestStart > 3f)
        {
            FailAndExit(
                $"Faith Bean did not damage Red Oni: startHP={faithBeanStartingHp}, "
                + $"currentHP={health.CurrentHP}.");
        }

        return false;
    }

    private static bool ValidatePhaseProgression(RedOniPhaseOneController boss)
    {
        if (phaseThresholdPassed)
        {
            return true;
        }

        RedOniBossHealth health = boss.GetComponent<RedOniBossHealth>();

        if (health == null)
        {
            FailAndExit("RedOniBossHealth was unavailable for the phase progression check.");
            return false;
        }

        if (!health.PhaseOneComplete)
        {
            while (health.CurrentHP > health.PhaseOneEndHP)
            {
                health.TakeDamage(1, boss.transform.position);
            }

            phaseTwoSmashStartCount = boss.CompletedPlatformSmashCount;
            Debug.Log("Red Oni Phase 1 threshold reached at 40 HP; waiting for a Phase 2 platform smash.");
            return false;
        }

        if (health.IsTransitioning)
        {
            return false;
        }

        if (!phaseTwoSmashPassed)
        {
            bool stageClearOpened = GameManager.Instance != null
                && GameManager.Instance.IsBlockingUiVisible;

            if (health.CurrentHP != health.PhaseOneEndHP
                || health.CurrentPhase != 2
                || boss.CurrentCombatPhase != 2
                || stageClearOpened)
            {
                FailAndExit(
                    $"Phase 2 did not start cleanly: HP={health.CurrentHP}, "
                    + $"healthPhase={health.CurrentPhase}, combatPhase={boss.CurrentCombatPhase}, "
                    + $"blockingUi={stageClearOpened}.");
                return false;
            }

            RedOniSmashablePlatform[] platforms =
                Object.FindObjectsByType<RedOniSmashablePlatform>(FindObjectsSortMode.None);
            sawBrokenPlatform |= System.Array.Exists(platforms, platform => platform.IsBroken);

            if (boss.CompletedPlatformSmashCount < phaseTwoSmashStartCount + 2)
            {
                return false;
            }

            if (!sawBrokenPlatform || System.Array.Exists(platforms, platform => platform.IsBroken))
            {
                return false;
            }

            if (string.IsNullOrEmpty(boss.LastPhaseTwoAnimationState)
                || !boss.LastPhaseTwoAnimationState.StartsWith("Phase2Smash", System.StringComparison.Ordinal))
            {
                FailAndExit(
                    $"Phase 2 used the wrong animation state: {boss.LastPhaseTwoAnimationState}.");
                return false;
            }

            float impactHorizontalError = Mathf.Abs(
                boss.LastPhaseTwoImpactVisualPosition.x - boss.LastPhaseTwoTargetPosition.x);

            if (impactHorizontalError > 0.2f)
            {
                FailAndExit(
                    $"Phase 2 boss did not move to the targeted platform before impact: "
                    + $"targetX={boss.LastPhaseTwoTargetPosition.x:0.00}, "
                    + $"impactX={boss.LastPhaseTwoImpactVisualPosition.x:0.00}.");
                return false;
            }

            if (boss.MaximumPhaseTwoArcHeightObserved < 0.75f)
            {
                FailAndExit(
                    $"Phase 2 platform approach did not produce a readable jump arc: "
                    + $"observedHeight={boss.MaximumPhaseTwoArcHeightObserved:0.00}.");
                return false;
            }

            phaseTwoSmashPassed = true;
            Debug.Log(
                $"Red Oni Phase 2 two-hit phrase passed: {boss.LastPhaseTwoAnimationState} "
                + $"used a {boss.MaximumPhaseTwoArcHeightObserved:0.00}-unit arc, completed both beats, "
                + "and each targeted platform warned, broke, and restored.");
        }

        if (!health.PhaseTwoComplete)
        {
            while (health.CurrentHP > health.PhaseTwoEndHP)
            {
                health.TakeDamage(1, boss.transform.position);
            }

            phaseThreeTestStart = Time.realtimeSinceStartup;
            Debug.Log("Red Oni Phase 2 threshold reached at 20 HP; waiting for Phase 3 enemy pressure.");
            return false;
        }

        if (health.IsTransitioning)
        {
            return false;
        }

        RedOniPhaseThreeAddsController adds =
            Object.FindFirstObjectByType<RedOniPhaseThreeAddsController>();
        bool blockingUi = GameManager.Instance != null
            && GameManager.Instance.IsBlockingUiVisible;

        if (adds == null
            || health.CurrentHP != health.PhaseTwoEndHP
            || health.CurrentPhase != 3
            || boss.CurrentCombatPhase != 3
            || blockingUi)
        {
            FailAndExit(
                $"Phase 3 did not start cleanly: HP={health.CurrentHP}, "
                + $"healthPhase={health.CurrentPhase}, combatPhase={boss.CurrentCombatPhase}, "
                + $"addsPresent={adds != null}, blockingUi={blockingUi}.");
            return false;
        }

        if (!phaseThreeAddsPassed)
        {
            RangedRunnerEnemy[] runners =
                Object.FindObjectsByType<RangedRunnerEnemy>(FindObjectsSortMode.None);
            int shotsFired = 0;

            foreach (RangedRunnerEnemy runner in runners)
            {
                shotsFired += runner != null ? runner.ShotsFired : 0;
            }

            if (adds.ActiveEnemyCount < 2 || shotsFired < 1)
            {
                if (Time.realtimeSinceStartup - phaseThreeTestStart > 7f)
                {
                    FailAndExit(
                        $"Phase 3 ranged pressure did not become active: "
                        + $"alive={adds.ActiveEnemyCount}, total={adds.TotalSpawned}, shots={shotsFired}.");
                }

                return false;
            }

            phaseThreeAddsPassed = true;
            Debug.Log(
                $"Red Oni Phase 3 pressure passed: {adds.ActiveEnemyCount} ranged runners "
                + $"spawned on separate lower platforms and fired {shotsFired} shot(s).");
        }

        if (!ValidatePhaseThreeBeanKill(
                boss,
                Object.FindFirstObjectByType<PlayerController>(),
                adds))
        {
            return false;
        }

        while (health.CurrentHP > 0)
        {
            health.TakeDamage(1, boss.transform.position);
        }

        phaseThresholdPassed = health.CurrentHP == 0
            && health.BossDefeated
            && !boss.IsAttacking
            && GameManager.Instance != null
            && GameManager.Instance.IsBlockingUiVisible
            && adds.ActiveEnemyCount == 0;

        if (!phaseThresholdPassed)
        {
            FailAndExit(
                $"Phase 3 defeat did not complete cleanly: HP={health.CurrentHP}, "
                + $"defeated={health.BossDefeated}, attacking={boss.IsAttacking}, "
                + $"activeAdds={adds.ActiveEnemyCount}, "
                + $"blockingUi={GameManager.Instance != null && GameManager.Instance.IsBlockingUiVisible}.");
            return false;
        }

        Debug.Log("Red Oni Phase 3 defeat passed: HP reached 0, adds cleared, and Stage Clear opened.");
        return true;
    }

    private static bool ValidatePhaseThreeBeanKill(
        RedOniPhaseOneController boss,
        PlayerController player,
        RedOniPhaseThreeAddsController adds)
    {
        if (phaseThreeBeanKillPassed)
        {
            return true;
        }

        FaithBeanShooter shooter = player != null ? player.GetComponent<FaithBeanShooter>() : null;

        if (shooter == null)
        {
            FailAndExit("FaithBeanShooter was unavailable for the Phase 3 add defeat check.");
            return false;
        }

        if (phaseThreeBeanTarget == null && phaseThreeBeanTestStart <= 0f)
        {
            RangedRunnerEnemy[] runners =
                Object.FindObjectsByType<RangedRunnerEnemy>(FindObjectsSortMode.None);

            if (runners.Length == 0)
            {
                return false;
            }

            phaseThreeBeanTarget = runners[0].gameObject;
            phaseThreeBeanStartingEnemyCount = adds.ActiveEnemyCount;
            phaseThreeBeanTestStart = Time.realtimeSinceStartup;
            phaseThreeNextBeanShotTime = Time.time;
            boss.SetEncounterActive(false);
            SuppressPhaseThreeBossColliders(boss);

            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
            Vector2 targetPosition = phaseThreeBeanTarget.transform.position;
            float approachDirection = targetPosition.x >= 0f ? -1f : 1f;
            Vector2 firingPosition = targetPosition + Vector2.right * approachDirection * 1.6f;

            if (playerBody != null)
            {
                playerBody.position = firingPosition;
                playerBody.linearVelocity = Vector2.zero;
            }
            else
            {
                player.transform.position = firingPosition;
            }

            player.SetControlEnabled(true);
            Debug.Log("Phase 3 Faith Bean add defeat check started against a live ranged runner.");
            return false;
        }

        if (phaseThreeBeanTarget == null)
        {
            phaseThreeBeanKillPassed = adds.ActiveEnemyCount < phaseThreeBeanStartingEnemyCount;
            RestorePhaseThreeBossColliders();
            boss.SetEncounterActive(true);

            if (!phaseThreeBeanKillPassed)
            {
                FailAndExit("The Phase 3 bean target disappeared without reducing the active enemy count.");
                return false;
            }

            Debug.Log("Phase 3 Faith Bean add defeat passed: a live ranged runner was defeated by bean shots.");
            return true;
        }

        if (Time.time >= phaseThreeNextBeanShotTime)
        {
            shooter.TryFireAt(phaseThreeBeanTarget.transform.position);
            phaseThreeNextBeanShotTime = Time.time + 0.34f;
        }

        if (Time.realtimeSinceStartup - phaseThreeBeanTestStart > 5f)
        {
            RestorePhaseThreeBossColliders();
            boss.SetEncounterActive(true);
            FailAndExit("Faith Bean shots did not defeat the Phase 3 ranged runner within five seconds.");
        }

        return false;
    }

    private static void SuppressPhaseThreeBossColliders(RedOniPhaseOneController boss)
    {
        RestorePhaseThreeBossColliders();
        phaseThreeBossColliders = boss.GetComponentsInChildren<Collider2D>(true);
        phaseThreeBossColliderStates = new bool[phaseThreeBossColliders.Length];

        for (int index = 0; index < phaseThreeBossColliders.Length; index++)
        {
            Collider2D bossCollider = phaseThreeBossColliders[index];
            phaseThreeBossColliderStates[index] = bossCollider != null && bossCollider.enabled;

            if (bossCollider != null)
            {
                bossCollider.enabled = false;
            }
        }
    }

    private static void RestorePhaseThreeBossColliders()
    {
        if (phaseThreeBossColliders == null || phaseThreeBossColliderStates == null)
        {
            return;
        }

        int restoreCount = Mathf.Min(
            phaseThreeBossColliders.Length,
            phaseThreeBossColliderStates.Length);

        for (int index = 0; index < restoreCount; index++)
        {
            Collider2D bossCollider = phaseThreeBossColliders[index];

            if (bossCollider != null)
            {
                bossCollider.enabled = phaseThreeBossColliderStates[index];
            }
        }

        phaseThreeBossColliders = null;
        phaseThreeBossColliderStates = null;
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
            health.ResetHealth();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }

            boss.SetEncounterActive(true);
            Debug.Log(
                "Boss fall recovery passed: one HP removed, player returned to a safe platform, "
                + "and the validation player was isolated before phase testing.");
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
