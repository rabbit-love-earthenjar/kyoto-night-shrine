using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class Stage12EnemyCombatPlayModeValidation
{
    private const string RequestedKey = "Stage12EnemyCombatValidation.Requested";
    private const string PhaseKey = "Stage12EnemyCombatValidation.Phase";
    private const string ScenePath = "Assets/Scenes/Stage_1_2.unity";
    private const string PaperDollScreenshotPath = "Logs/stage12_paperdoll_healthbar.png";
    private const string LanternScreenshotPath = "Logs/stage12_lantern_healthbar.png";
    private const string CleanupScreenshotPath = "Logs/stage12_enemy_death_cleanup.png";
    private static int playFrames;
    private static GhostHealth paperDoll;
    private static GhostHealth lantern;

    static Stage12EnemyCombatPlayModeValidation()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    public static void ValidateInPlayMode()
    {
        DeleteExistingScreenshot(PaperDollScreenshotPath);
        DeleteExistingScreenshot(LanternScreenshotPath);
        DeleteExistingScreenshot(CleanupScreenshotPath);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(RequestedKey, true);
        SessionState.SetInt(PhaseKey, 0);
        playFrames = 0;
        paperDoll = null;
        lantern = null;
        Debug.Log("Stage 1-2 enemy combat Play Mode validation requested.");
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
            RunCombatSequence();
            return;
        }

        if (phase == 3 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.EraseBool(RequestedKey);
            SessionState.EraseInt(PhaseKey);
            Debug.Log("Stage 1-2 enemy combat Play Mode validation completed.");
            EditorApplication.Exit(0);
        }
    }

    private static void RunCombatSequence()
    {
        if (playFrames <= 35 && paperDoll == null)
        {
            paperDoll = FindHealth("PaperDoll_01_Tutorial");
        }

        if (playFrames <= 72 && lantern == null)
        {
            lantern = FindHealth("GhostLantern_01_Tutorial");
        }

        if ((playFrames <= 35 && paperDoll == null) || (playFrames <= 72 && lantern == null))
        {
            Finish(false, "Required Stage 1-2 combat enemies were not found.");
            return;
        }

        if (paperDoll != null)
        {
            PauseEnemy(paperDoll);
        }

        if (lantern != null)
        {
            PauseEnemy(lantern);
        }

        if (playFrames == 15)
        {
            if (!ValidateOpeningFlowConfiguration())
            {
                return;
            }

            if (!ValidateSpawnPointMarkersHidden())
            {
                return;
            }

            if (paperDoll.MaxHP != 3 || lantern.MaxHP != 5)
            {
                Finish(false, $"Unexpected HP tuning: Paper Doll {paperDoll.MaxHP}, Ghost Lantern {lantern.MaxHP}.");
                return;
            }

            SpawnAttackHitbox(paperDoll, 1);
        }
        else if (playFrames == 19)
        {
            if (!ValidateHitState(paperDoll, 2, 2f / 3f, "Paper Doll"))
            {
                return;
            }

            CaptureEnemy(paperDoll, PaperDollScreenshotPath);
        }
        else if (playFrames == 27)
        {
            SpawnAttackHitbox(paperDoll, 1);
        }
        else if (playFrames == 35)
        {
            SpawnAttackHitbox(paperDoll, 2);
        }
        else if (playFrames == 39)
        {
            if (paperDoll != null && !paperDoll.IsDead)
            {
                Finish(false, "Paper Doll did not die after the 1/1/2 three-input combo.");
            }
        }
        else if (playFrames == 48)
        {
            SpawnAttackHitbox(lantern, 1);
        }
        else if (playFrames == 52)
        {
            if (!ValidateHitState(lantern, 4, 0.8f, "Ghost Lantern"))
            {
                return;
            }

            CaptureEnemy(lantern, LanternScreenshotPath);
        }
        else if (playFrames == 60)
        {
            SpawnAttackHitbox(lantern, 1);
        }
        else if (playFrames == 68)
        {
            SpawnAttackHitbox(lantern, 2);
        }
        else if (playFrames == 72)
        {
            if (lantern == null || lantern.CurrentHP != 1)
            {
                Finish(false, "Ghost Lantern should retain 1 HP after one full 1/1/2 combo.");
                return;
            }

            SpawnAttackHitbox(lantern, 1);
        }
        else if (playFrames == 80)
        {
            if (lantern != null && !lantern.IsDead)
            {
                Finish(false, "Ghost Lantern did not die after the expected fourth input.");
            }
        }
        else if (playFrames >= 90)
        {
            if (GameObject.Find("PaperDoll_01_Tutorial") != null
                || GameObject.Find("GhostLantern_01_Tutorial") != null
                || GameObject.Find("PaperDoll_01_Tutorial_HealthBar") != null
                || GameObject.Find("GhostLantern_01_Tutorial_HealthBar") != null)
            {
                Finish(false, "A defeated enemy body or health bar remained in the scene.");
                return;
            }

            CaptureAtCurrentCamera(CleanupScreenshotPath);
            Finish(true, "Paper Doll requires three inputs, Ghost Lantern requires four, and both health bars updated visibly.");
        }
    }

    private static bool ValidateHitState(GhostHealth health, int expectedHP, float expectedFill, string label)
    {
        if (health.CurrentHP != expectedHP || !health.HealthBarVisible || Mathf.Abs(health.HealthBarFillFraction - expectedFill) > 0.02f)
        {
            Finish(false, $"{label} hit state failed: HP={health.CurrentHP}, visible={health.HealthBarVisible}, fill={health.HealthBarFillFraction:0.00}.");
            return false;
        }

        return true;
    }

    private static bool ValidateSpawnPointMarkersHidden()
    {
        GameObject spawnPoints = GameObject.Find("SpawnPoints");
        if (spawnPoints == null)
        {
            Finish(false, "Stage 1-2 spawn point container was not found.");
            return false;
        }

        for (int index = 0; index < spawnPoints.transform.childCount; index++)
        {
            Transform marker = spawnPoints.transform.GetChild(index);
            if (!marker.name.StartsWith("GhostSpawn_"))
            {
                continue;
            }

            foreach (SpriteRenderer markerRenderer in marker.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (markerRenderer.enabled)
                {
                    Finish(false, $"Spawn marker renderer remained visible: {markerRenderer.name}.");
                    return false;
                }
            }

            foreach (Collider2D markerCollider in marker.GetComponentsInChildren<Collider2D>(true))
            {
                if (markerCollider.enabled)
                {
                    Finish(false, $"Spawn marker collider remained active: {markerCollider.name}.");
                    return false;
                }
            }
        }

        return true;
    }

    private static bool ValidateOpeningFlowConfiguration()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            Finish(false, "Stage 1-2 GameManager was not found.");
            return false;
        }

        SerializedObject serializedManager = new SerializedObject(gameManager);
        string continueScene = serializedManager.FindProperty("continueSceneName")?.stringValue;
        string storyStage = serializedManager.FindProperty("clearStoryStageId")?.stringValue;
        bool unlockCafe = serializedManager.FindProperty("unlockCafeOnClear")?.boolValue ?? true;
        if (continueScene != "HubMap_Day" || storyStage != "HubArrival" || unlockCafe)
        {
            Finish(
                false,
                $"Opening flow mismatch: continue={continueScene}, story={storyStage}, unlockCafe={unlockCafe}.");
            return false;
        }

        return true;
    }

    private static GhostHealth FindHealth(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        return target != null ? target.GetComponent<GhostHealth>() : null;
    }

    private static void PauseEnemy(GhostHealth health)
    {
        GhostEnemy movement = health.GetComponent<GhostEnemy>();
        if (movement != null)
        {
            movement.PauseMovement();
        }
    }

    private static void SpawnAttackHitbox(GhostHealth target, int damage)
    {
        Collider2D targetCollider = target.GetComponent<Collider2D>();
        Bounds bounds = targetCollider != null
            ? targetCollider.bounds
            : new Bounds(target.transform.position, Vector3.one);
        GameObject hitboxObject = new GameObject($"ValidationAttack_{damage}");
        hitboxObject.transform.position = bounds.center;
        hitboxObject.transform.localScale = new Vector3(
            Mathf.Max(0.5f, bounds.size.x + 0.2f),
            Mathf.Max(0.5f, bounds.size.y + 0.2f),
            1f);

        Rigidbody2D body = hitboxObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        BoxCollider2D collider = hitboxObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        AttackHitbox hitbox = hitboxObject.AddComponent<AttackHitbox>();
        hitbox.Initialize(0.1f, damage, bounds.center + Vector3.left, 0.14f);
        Physics2D.SyncTransforms();
        hitboxObject.SendMessage("OnTriggerEnter2D", targetCollider, SendMessageOptions.RequireReceiver);
        hitboxObject.SendMessage("OnTriggerStay2D", targetCollider, SendMessageOptions.RequireReceiver);
    }

    private static void CaptureEnemy(GhostHealth health, string relativePath)
    {
        Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        if (camera == null)
        {
            Finish(false, "Combat screenshot failed because no camera was available.");
            return;
        }

        CameraFollow follow = camera.GetComponent<CameraFollow>();
        if (follow != null)
        {
            follow.enabled = false;
        }

        camera.orthographicSize = 3.5f;
        camera.transform.position = new Vector3(health.transform.position.x, health.transform.position.y + 0.7f, camera.transform.position.z);
        CameraPinnedBackground background = Object.FindAnyObjectByType<CameraPinnedBackground>();
        if (background != null)
        {
            background.SnapToCamera();
        }

        CaptureAtCurrentCamera(relativePath);
    }

    private static void CaptureAtCurrentCamera(string relativePath)
    {
        Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        if (camera == null)
        {
            Finish(false, "Combat screenshot failed because no camera was available.");
            return;
        }

        string absolutePath = Path.GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);

        try
        {
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0f, 0f, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = null;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(renderTexture);
        }
    }

    private static void DeleteExistingScreenshot(string relativePath)
    {
        string absolutePath = Path.GetFullPath(relativePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
    }

    private static void Finish(bool succeeded, string message)
    {
        Time.timeScale = 1f;
        SessionState.SetInt(PhaseKey, 3);
        if (succeeded)
        {
            Debug.Log($"Stage 1-2 enemy combat validation passed: {message}");
        }
        else
        {
            Debug.LogError($"Stage 1-2 enemy combat validation failed: {message}");
        }

        EditorApplication.ExitPlaymode();
    }
}
