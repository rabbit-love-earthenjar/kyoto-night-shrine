using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class Stage12EnemyGroundingPlayModeValidation
{
    private const string RequestedKey = "Stage12EnemyGroundingValidation.Requested";
    private const string PhaseKey = "Stage12EnemyGroundingValidation.Phase";
    private const string ScenePath = "Assets/Scenes/Stage_1_2.unity";
    private const string EarlyScreenshotPath = "Logs/stage12_grounding_early.png";
    private const string MiddleScreenshotPath = "Logs/stage12_grounding_middle.png";
    private const string LateScreenshotPath = "Logs/stage12_grounding_late.png";
    private const string EarlyBackgroundPath = "Logs/stage12_background_early.png";
    private const string MiddleBackgroundPath = "Logs/stage12_background_middle.png";
    private const string LateBackgroundPath = "Logs/stage12_background_late.png";
    private static int playFrames;

    static Stage12EnemyGroundingPlayModeValidation()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    public static void ValidateInPlayMode()
    {
        DeleteExistingScreenshot(EarlyScreenshotPath);
        DeleteExistingScreenshot(MiddleScreenshotPath);
        DeleteExistingScreenshot(LateScreenshotPath);
        DeleteExistingScreenshot(EarlyBackgroundPath);
        DeleteExistingScreenshot(MiddleBackgroundPath);
        DeleteExistingScreenshot(LateBackgroundPath);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(RequestedKey, true);
        SessionState.SetInt(PhaseKey, 0);
        playFrames = 0;
        Debug.Log("Stage 1-2 enemy grounding Play Mode validation requested.");
    }

    public static void ReportBackgroundSpriteSize()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Backgrounds/Night_shrine_1_background.png");
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
            {
                Debug.Log($"Stage 1-2 full background sprite world size: {sprite.bounds.size.x:0.000} x {sprite.bounds.size.y:0.000}.");
            }
        }
        EditorApplication.Exit(0);
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
            Time.timeScale = 4f;

            if (playFrames == 20 || playFrames == 100)
            {
                if (!ValidateSingleBackground())
                {
                    Finish(false);
                    return;
                }

                if (!ValidateGroundEnemies(playFrames == 20 ? "startup" : "patrol"))
                {
                    Finish(false);
                    return;
                }

                if (playFrames == 100 && !CaptureGroundingScreenshots())
                {
                    Finish(false);
                    return;
                }
            }

            if (playFrames >= 100)
            {
                Finish(true);
            }

            return;
        }

        if (phase == 3 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.EraseBool(RequestedKey);
            SessionState.EraseInt(PhaseKey);
            Debug.Log("Stage 1-2 enemy grounding Play Mode validation completed.");
            EditorApplication.Exit(0);
        }
    }

    private static bool ValidateGroundEnemies(string checkpoint)
    {
        GhostEnemy[] enemies = Object.FindObjectsByType<GhostEnemy>(FindObjectsSortMode.None);
        int groundEnemyCount = 0;

        foreach (GhostEnemy enemy in enemies)
        {
            SerializedObject serializedEnemy = new SerializedObject(enemy);
            SerializedProperty movementMode = serializedEnemy.FindProperty("movementMode");
            if (movementMode == null || movementMode.enumValueIndex != 1)
            {
                continue;
            }

            groundEnemyCount++;
            SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
            float surfaceY = FindSupportingSurfaceY(enemy);
            SerializedProperty visualInsetProperty = serializedEnemy.FindProperty("groundVisualInset");
            float visualInset = visualInsetProperty != null ? Mathf.Max(0f, visualInsetProperty.floatValue) : 0f;
            float clearance = renderer != null && !float.IsNegativeInfinity(surfaceY)
                ? renderer.bounds.min.y - surfaceY
                : float.NegativeInfinity;
            float correctedClearance = clearance + visualInset;

            Debug.Log($"Stage 1-2 grounding {checkpoint}: {enemy.name}, clearance={clearance:0.000}, visualInset={visualInset:0.000}.");
            if (renderer == null || float.IsNegativeInfinity(surfaceY) || correctedClearance < -0.03f)
            {
                Debug.LogError($"Stage 1-2 grounding failed at {checkpoint}: {enemy.name} is embedded in its platform.");
                return false;
            }
        }

        if (groundEnemyCount != 5)
        {
            Debug.LogError($"Stage 1-2 grounding expected 5 ground enemies but found {groundEnemyCount}.");
            return false;
        }

        return true;
    }

    private static float FindSupportingSurfaceY(GhostEnemy enemy)
    {
        Vector2 origin = new Vector2(enemy.transform.position.x, enemy.transform.position.y + 1.5f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 5f);
        float highestSurfaceY = float.NegativeInfinity;

        foreach (RaycastHit2D hit in hits)
        {
            Collider2D candidate = hit.collider;
            if (candidate == null
                || candidate.isTrigger
                || candidate.GetComponentInParent<GhostEnemy>() != null
                || candidate.GetComponentInParent<PlayerController>() != null)
            {
                continue;
            }

            float surfaceY = candidate.bounds.max.y;
            if (surfaceY <= enemy.transform.position.y + 0.5f)
            {
                highestSurfaceY = Mathf.Max(highestSurfaceY, surfaceY);
            }
        }

        return highestSurfaceY;
    }

    private static bool CaptureGroundingScreenshots()
    {
        Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        if (camera == null)
        {
            Debug.LogError("Stage 1-2 grounding screenshots failed because no camera was available.");
            return false;
        }

        CameraFollow follow = camera.GetComponent<CameraFollow>();
        if (follow != null)
        {
            follow.enabled = false;
        }

        camera.orthographicSize = 4.5f;
        CaptureAt(camera, new Vector2(24f, 2f), EarlyScreenshotPath);
        CaptureAt(camera, new Vector2(66.5f, 2f), MiddleScreenshotPath);
        CaptureAt(camera, new Vector2(94f, 2f), LateScreenshotPath);
        camera.orthographicSize = 5f;
        CaptureAt(camera, new Vector2(5f, 2f), EarlyBackgroundPath);
        CaptureAt(camera, new Vector2(68f, 2f), MiddleBackgroundPath);
        CaptureAt(camera, new Vector2(131f, 2f), LateBackgroundPath);
        Debug.Log("Stage 1-2 grounding visual screenshots saved.");
        return true;
    }

    private static void CaptureAt(Camera camera, Vector2 position, string relativePath)
    {
        camera.transform.position = new Vector3(position.x, position.y, camera.transform.position.z);
        CameraPinnedBackground background = Object.FindAnyObjectByType<CameraPinnedBackground>();
        if (background != null)
        {
            background.SnapToCamera();
        }
        string absolutePath = Path.GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

        const int width = 1280;
        const int height = 720;
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

        try
        {
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
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

    private static bool ValidateSingleBackground()
    {
        CameraPinnedBackground[] backgrounds = Object.FindObjectsByType<CameraPinnedBackground>(FindObjectsSortMode.None);
        if (backgrounds.Length != 1)
        {
            Debug.LogError($"Stage 1-2 expected one active full background but found {backgrounds.Length}.");
            return false;
        }

        SpriteRenderer renderer = backgrounds[0].GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null || renderer.sprite.name != "Night_shrine_1_background")
        {
            Debug.LogError("Stage 1-2 full background is missing the intended Night_shrine_1_background sprite.");
            return false;
        }

        Camera camera = Camera.main;
        float requiredHeight = camera != null ? camera.orthographicSize * 2f : 10f;
        if (renderer.bounds.size.y + 0.01f < requiredHeight)
        {
            Debug.LogError($"Stage 1-2 full background height {renderer.bounds.size.y:0.000} does not cover camera height {requiredHeight:0.000}.");
            return false;
        }

        return true;
    }

    private static void DeleteExistingScreenshot(string relativePath)
    {
        string absolutePath = Path.GetFullPath(relativePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
    }

    private static void Finish(bool succeeded)
    {
        Time.timeScale = 1f;
        SessionState.SetInt(PhaseKey, 3);
        if (succeeded)
        {
            Debug.Log("Stage 1-2 enemy grounding passed at startup and after patrol movement.");
        }
        EditorApplication.ExitPlaymode();
    }
}
