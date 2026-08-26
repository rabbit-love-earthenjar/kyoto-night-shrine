using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RedOniBossSceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Stage_1_1.unity";
    private const string BossScenePath = "Assets/Scenes/Stage_1_Boss_RedOni.unity";
    private const string BossPrefabPath = "Assets/Art/boss/RedOni_Phase1_Visual.prefab";
    private const string BackgroundPath = "Assets/Art/Backgrounds/BG_Section_late.png";
    private const string PlatformPath = "Assets/Sprites/Environment/Platforms/bridge_stage_icon_transparent.png";
    private const string PhaseThreeEnemyVisualPath = "Assets/Prefabs/LightMonsterVisual.prefab";
    private const string PhaseThreeProjectilePath = "Assets/Sprites/Items/faith_icon_transparent.png";
    private const string FaithBeanVisualPath = "Assets/Art/boss/fukubean_cutout.png";
    private const string FaithBeanTrajectoryPath = "Assets/Art/boss/movingline_pieces.png";
    private const string FaithBeanAimIconPath = "Assets/Art/boss/aim_icon.png";
    private const string FaithBeanThrowSfxPath = "Assets/Audio/投げる.mp3";
    private const string RedOniSmashSfxPath = "Assets/Audio/boos_打撃.mp3";
    private const string PhaseOneBgmPath = "Assets/Audio/Crimson Shrine Cage1.mp3";
    private const string PhaseTwoBgmPath = "Assets/Audio/Crimson Shrine Cage2.mp3";
    private const string PhaseThreeBgmPath = "Assets/Audio/Crimson Shrine Cage3.mp3";

    private readonly struct PlatformSpec
    {
        public PlatformSpec(string name, float x, float y, float width)
        {
            Name = name;
            Position = new Vector2(x, y);
            Width = width;
        }

        public string Name { get; }
        public Vector2 Position { get; }
        public float Width { get; }
    }

    // The six ledges form one continuous route: climb the staggered left side,
    // cross the two close upper ledges, then descend the staggered right side.
    private static readonly PlatformSpec[] PlatformLayout =
    {
        new PlatformSpec("Platform_L1", -2.5f, -3.1f, 3.2f),
        new PlatformSpec("Platform_L2", -4.4f, -1.1f, 2.8f),
        new PlatformSpec("Platform_L3", -2.6f, 1.1f, 2.8f),
        new PlatformSpec("Platform_R3", 2.6f, 1.1f, 2.8f),
        new PlatformSpec("Platform_R2", 4.4f, -1.1f, 2.8f),
        new PlatformSpec("Platform_R1", 2.5f, -3.1f, 3.2f)
    };

    private static readonly HashSet<string> PreservedRoots = new HashSet<string>(StringComparer.Ordinal)
    {
        "Player",
        "Main Camera",
        "Global Light 2D",
        "GameManager",
        "GameAudio"
    };

    [MenuItem("Tools/Kyoto Night Shrine/Boss/Build Phase 1 Boss Scene")]
    public static void BuildBossScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
        {
            throw new InvalidOperationException($"Source scene is missing: {SourceScenePath}");
        }

        RedOniBossAnimationBuilder.BuildPhaseOneAnimations();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BossScenePath) != null)
        {
            AssetDatabase.DeleteAsset(BossScenePath);
        }

        if (!AssetDatabase.CopyAsset(SourceScenePath, BossScenePath))
        {
            throw new InvalidOperationException($"Could not create boss scene from {SourceScenePath}.");
        }

        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.OpenScene(BossScenePath, OpenSceneMode.Single);
        RemoveOldStageContent(scene);

        PlayerController player = FindInScene<PlayerController>(scene);
        GameManager gameManager = FindInScene<GameManager>(scene);
        Camera camera = FindInScene<Camera>(scene);

        if (player == null || gameManager == null || camera == null)
        {
            throw new InvalidOperationException("Boss scene source is missing Player, GameManager, or Main Camera.");
        }

        ConfigurePlayer(player);
        ConfigureCamera(camera);
        ConfigureGameManager(gameManager, player);
        EnsurePauseController();

        GameObject arena = new GameObject("RedOniBossArena_Phase1");
        CreateBackground(arena.transform);
        CreatePlatforms(arena.transform);
        CreateArenaBoundaries(arena.transform);
        CreateBossFallRecovery(arena.transform);
        CreateFallZone(arena.transform);
        CreateBoss(arena.transform, player.transform);

        AddSceneToBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, BossScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidateBossScene();
        Debug.Log("Red Oni Phase 1 boss scene built: Assets/Scenes/Stage_1_Boss_RedOni.unity");
    }

    [MenuItem("Tools/Kyoto Night Shrine/Boss/Validate Boss Scene")]
    public static void ValidateBossScene()
    {
        Scene scene = EditorSceneManager.OpenScene(BossScenePath, OpenSceneMode.Single);
        List<string> failures = new List<string>();

        RequireObject("Player", failures);
        RequireObject("GameManager", failures);
        RequireObject("Main Camera", failures);
        RequireObject("RedOniBossArena_Phase1", failures);
        RequireObject("RedOni_Phase1", failures);
        RequireObject("StartPoint", failures);
        RequireObject("BossFallRecoveryZone", failures);
        RequireObject("RecoveryPoint_Left", failures);
        RequireObject("RecoveryPoint_Right", failures);
        RequireObject("FallZone", failures);

        for (int row = 1; row <= 3; row++)
        {
            RequireObject($"Platform_L{row}", failures);
            RequireObject($"Platform_R{row}", failures);
        }

        RedOniSmashablePlatform[] smashablePlatforms =
            UnityEngine.Object.FindObjectsByType<RedOniSmashablePlatform>(FindObjectsSortMode.None);

        if (smashablePlatforms.Length != PlatformLayout.Length)
        {
            failures.Add(
                $"Phase 2 requires {PlatformLayout.Length} smashable platforms, "
                + $"but found {smashablePlatforms.Length}.");
        }

        ValidatePlatformRoute(failures);

        RedOniPhaseOneController controller = UnityEngine.Object.FindFirstObjectByType<RedOniPhaseOneController>();

        if (controller == null)
        {
            failures.Add("RedOniPhaseOneController is missing.");
        }

        FaithBeanShooter faithBeanShooter = UnityEngine.Object.FindFirstObjectByType<FaithBeanShooter>();

        if (faithBeanShooter == null)
        {
            failures.Add("FaithBeanShooter is missing from the boss scene player.");
        }
        else if (!faithBeanShooter.HasCustomVisuals)
        {
            failures.Add("FaithBeanShooter is missing its projectile, trajectory, or aim icon visual.");
        }

        RedOniBossHealth bossHealth = UnityEngine.Object.FindFirstObjectByType<RedOniBossHealth>();

        if (bossHealth == null)
        {
            failures.Add("RedOniBossHealth is missing.");
        }
        else if (bossHealth.MaxHP != 60
            || bossHealth.PhaseOneEndHP != 40
            || bossHealth.PhaseTwoEndHP != 20
            || !Mathf.Approximately(bossHealth.FinalRushMouseSpeedThreshold, 60f)
            || bossHealth.FinalRushRequiredHits != 20)
        {
            failures.Add(
                "Red Oni progression must be 60 -> 40 -> 20 -> 0, then a Final Rush "
                + "requiring mouse speed 60 and 20 qualified hits.");
        }
        else if (bossHealth.GetComponentsInChildren<Collider2D>(true).All(collider => !collider.isTrigger))
        {
            failures.Add("Red Oni projectile hit trigger is missing.");
        }

        RedOniPhaseThreeAddsController phaseThreeAdds =
            UnityEngine.Object.FindFirstObjectByType<RedOniPhaseThreeAddsController>();

        if (phaseThreeAdds == null || !phaseThreeAdds.IsConfigured)
        {
            failures.Add("Red Oni Phase 3 ranged-enemy pressure is missing or not configured.");
        }

        RedOniPhaseMusicController phaseMusic =
            UnityEngine.Object.FindFirstObjectByType<RedOniPhaseMusicController>();

        if (phaseMusic == null || !phaseMusic.IsConfigured)
        {
            failures.Add("Red Oni phase music is missing one or more of its three BGM clips.");
        }

        RequireObject("Phase3AddSpawn_Left", failures);
        RequireObject("Phase3AddSpawn_Right", failures);

        if (UnityEngine.Object.FindFirstObjectByType<CombatPauseController>() == null)
        {
            failures.Add("CombatPauseController is missing.");
        }

        if (GameObject.Find("Stage01_Level") != null || GameObject.Find("Stage01_Backgrounds") != null)
        {
            failures.Add("Old Stage_1_1 level content remains in the boss scene.");
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException("Red Oni boss scene validation failed:\n- " + string.Join("\n- ", failures));
        }

        Debug.Log(
            "Red Oni boss scene validation passed: isolated scene, six reachable one-way platforms, "
            + "fixed camera, fall retry, pause controller, Faith Bean shooting, three HP phases, "
            + "a speed-gated Final Rush, aligned attacks, and bounded Phase 3 ranged-enemy pressure "
            + "are present.");
    }

    [MenuItem("Tools/Kyoto Night Shrine/Boss/Install Phase 1 Faith Bean Combat")]
    public static void InstallPhaseOneFaithBeanCombat()
    {
        InstallPhaseTwoCombat();
    }

    [MenuItem("Tools/Kyoto Night Shrine/Boss/Install Phase 2 Combat")]
    public static void InstallPhaseTwoCombat()
    {
        RedOniBossAnimationBuilder.BuildPhaseTwoSmashAnimation();
        Scene scene = EditorSceneManager.OpenScene(BossScenePath, OpenSceneMode.Single);
        PlayerController player = FindInScene<PlayerController>(scene);
        RedOniPhaseOneController controller = FindInScene<RedOniPhaseOneController>(scene);

        if (player == null || controller == null)
        {
            throw new InvalidOperationException(
                "Existing boss scene is missing PlayerController or RedOniPhaseOneController.");
        }

        EnsureFaithBeanShooter(player);
        RedOniSmashablePlatform[] smashablePlatforms = EnsureSmashablePlatforms();
        controller.ConfigureSmashablePlatforms(smashablePlatforms);
        RedOniBossHealth health = EnsureBossCombat(controller.gameObject, controller);
        EnsurePhaseThreeAdds(controller.gameObject, health);
        EditorUtility.SetDirty(controller);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, BossScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidateBossScene();
        Debug.Log("Red Oni Phase 2 combat and Phase 3 ranged-enemy pressure installed without rebuilding the scene.");
    }

    private static void RemoveOldStageContent(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            bool containsRequiredSystem = root.GetComponentInChildren<PlayerController>(true) != null
                || root.GetComponentInChildren<GameManager>(true) != null
                || root.GetComponentInChildren<Camera>(true) != null;

            if (!PreservedRoots.Contains(root.name) && !containsRequiredSystem)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

    }

    private static void ConfigurePlayer(PlayerController player)
    {
        player.name = "Player";
        player.transform.position = new Vector3(-2.5f, -1.9f, 0f);
        player.transform.rotation = Quaternion.identity;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.simulated = true;
            body.linearVelocity = Vector2.zero;
        }

        foreach (SpriteRenderer renderer in player.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 10);
        }

        EnsureFaithBeanShooter(player);
    }

    private static void ConfigureCamera(Camera camera)
    {
        camera.name = "Main Camera";
        camera.orthographic = true;
        camera.orthographicSize = 5.5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.backgroundColor = new Color(0.015f, 0.025f, 0.08f, 1f);

        CameraFollow follow = camera.GetComponent<CameraFollow>();

        if (follow != null)
        {
            UnityEngine.Object.DestroyImmediate(follow);
        }
    }

    private static void ConfigureGameManager(GameManager gameManager, PlayerController player)
    {
        GameObject startPoint = new GameObject("StartPoint");
        startPoint.transform.position = player.transform.position;

        SerializedObject serialized = new SerializedObject(gameManager);
        serialized.FindProperty("player").objectReferenceValue = player;
        serialized.FindProperty("playerBody").objectReferenceValue = player.GetComponent<Rigidbody2D>();
        serialized.FindProperty("playerHealth").objectReferenceValue = player.GetComponent<PlayerHealth>();
        serialized.FindProperty("startPoint").objectReferenceValue = startPoint.transform;
        serialized.FindProperty("fallRetryY").floatValue = -8.2f;
        serialized.FindProperty("showStarSealUi").boolValue = false;
        serialized.FindProperty("stageClearTitle").stringValue = "Red Oni - Phase 1";
        serialized.FindProperty("stageClearMessage").stringValue = "Phase 1 prototype complete.";
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameManager);
    }

    private static void EnsurePauseController()
    {
        if (UnityEngine.Object.FindFirstObjectByType<CombatPauseController>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject pauseObject = new GameObject("CombatPauseController");
        pauseObject.AddComponent<CombatPauseController>();
    }

    private static void CreateBackground(Transform parent)
    {
        Sprite backgroundSprite = LoadFirstSprite(BackgroundPath);

        if (backgroundSprite == null)
        {
            throw new InvalidOperationException($"Boss background sprite is missing: {BackgroundPath}");
        }

        GameObject background = new GameObject("BossBackground");
        background.transform.SetParent(parent, false);
        background.transform.position = new Vector3(0f, 0f, 4f);

        SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
        renderer.sprite = backgroundSprite;
        renderer.sortingOrder = -20;
        renderer.color = new Color(0.82f, 0.88f, 1f, 1f);

        Vector2 spriteSize = backgroundSprite.bounds.size;
        float scale = Mathf.Max(22f / Mathf.Max(0.1f, spriteSize.x), 11.5f / Mathf.Max(0.1f, spriteSize.y));
        background.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static void CreatePlatforms(Transform parent)
    {
        Transform platforms = new GameObject("Platforms").transform;
        platforms.SetParent(parent, false);

        foreach (PlatformSpec spec in PlatformLayout)
        {
            CreatePlatform(platforms, spec);
        }
    }

    private static void CreatePlatform(Transform parent, PlatformSpec spec)
    {
        Sprite platformSprite = LoadFirstSprite(PlatformPath);

        if (platformSprite == null)
        {
            throw new InvalidOperationException($"Boss platform sprite is missing: {PlatformPath}");
        }

        GameObject platform = new GameObject(spec.Name);
        platform.transform.SetParent(parent, false);
        platform.transform.position = spec.Position;

        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(spec.Width, 0.36f);
        collider.usedByEffector = true;

        PlatformEffector2D effector = platform.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 170f;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(platform.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.4f, 0f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = platformSprite;
        renderer.sortingOrder = 4;

        Vector2 spriteSize = platformSprite.bounds.size;
        visual.transform.localScale = new Vector3(
            spec.Width / Mathf.Max(0.1f, spriteSize.x),
            0.72f / Mathf.Max(0.1f, spriteSize.y),
            1f);

        platform.AddComponent<RedOniSmashablePlatform>();
    }

    private static RedOniSmashablePlatform[] EnsureSmashablePlatforms()
    {
        List<RedOniSmashablePlatform> platforms = new List<RedOniSmashablePlatform>();

        foreach (PlatformSpec spec in PlatformLayout)
        {
            GameObject platformObject = GameObject.Find(spec.Name);

            if (platformObject == null)
            {
                throw new InvalidOperationException($"Boss platform is missing: {spec.Name}");
            }

            RedOniSmashablePlatform platform =
                platformObject.GetComponent<RedOniSmashablePlatform>();

            if (platform == null)
            {
                platform = platformObject.AddComponent<RedOniSmashablePlatform>();
            }

            platforms.Add(platform);
            EditorUtility.SetDirty(platform);
        }

        return platforms.ToArray();
    }

    private static void CreateArenaBoundaries(Transform parent)
    {
        Transform boundaries = new GameObject("Boundaries").transform;
        boundaries.SetParent(parent, false);
        CreateInvisibleWall(boundaries, "LeftWall", new Vector2(-9.2f, 0f), new Vector2(0.5f, 12f));
        CreateInvisibleWall(boundaries, "RightWall", new Vector2(9.2f, 0f), new Vector2(0.5f, 12f));
    }

    private static void CreateInvisibleWall(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private static void CreateFallZone(Transform parent)
    {
        GameObject fallZone = new GameObject("FallZone");
        fallZone.transform.SetParent(parent, false);
        fallZone.transform.position = new Vector3(0f, -8.4f, 0f);

        BoxCollider2D collider = fallZone.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(20f, 1.2f);
        fallZone.AddComponent<FallZone>();
    }

    private static void CreateBossFallRecovery(Transform parent)
    {
        Transform safePoints = new GameObject("RecoveryPoints").transform;
        safePoints.SetParent(parent, false);

        Transform leftPoint = new GameObject("RecoveryPoint_Left").transform;
        leftPoint.SetParent(safePoints, false);
        leftPoint.position = new Vector3(-2.5f, -1.9f, 0f);

        Transform rightPoint = new GameObject("RecoveryPoint_Right").transform;
        rightPoint.SetParent(safePoints, false);
        rightPoint.position = new Vector3(2.5f, -1.9f, 0f);

        GameObject recoveryZone = new GameObject("BossFallRecoveryZone");
        recoveryZone.transform.SetParent(parent, false);
        recoveryZone.transform.position = new Vector3(0f, -5.2f, 0f);

        BoxCollider2D collider = recoveryZone.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(18f, 1.1f);

        BossFallRecoveryZone recovery = recoveryZone.AddComponent<BossFallRecoveryZone>();
        recovery.Configure(new[] { leftPoint, rightPoint });
    }

    private static void CreateBoss(Transform parent, Transform player)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);

        if (prefab == null)
        {
            throw new InvalidOperationException($"Red Oni visual prefab is missing: {BossPrefabPath}");
        }

        GameObject boss = new GameObject("RedOni_Phase1");
        boss.transform.SetParent(parent, false);
        boss.transform.position = new Vector3(0f, 0.35f, 1f);

        GameObject visual = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (visual == null)
        {
            throw new InvalidOperationException("Could not instantiate Red Oni visual prefab.");
        }

        visual.name = "Visual";
        visual.transform.SetParent(boss.transform, false);
        visual.transform.localPosition = Vector3.zero;

        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = -2;
            RedOniVisualHeightNormalizer normalizer = visual.AddComponent<RedOniVisualHeightNormalizer>();
            normalizer.Configure(renderer, 7.45f);
        }

        RedOniPhaseOneController controller = boss.AddComponent<RedOniPhaseOneController>();
        controller.ConfigureAnimator(visual.GetComponent<Animator>());
        controller.ConfigureCombatAudio(AssetDatabase.LoadAssetAtPath<AudioClip>(RedOniSmashSfxPath));
        controller.ConfigureArena(player, -2.2f, -0.2f, 2f, 0f, 17.4f);
        controller.ConfigureSmashablePlatforms(EnsureSmashablePlatforms());
        RedOniBossHealth health = EnsureBossCombat(boss, controller);
        EnsurePhaseThreeAdds(boss, health);
    }

    private static RedOniPhaseThreeAddsController EnsurePhaseThreeAdds(
        GameObject boss,
        RedOniBossHealth health)
    {
        GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PhaseThreeEnemyVisualPath);
        Sprite projectileSprite = LoadFirstSprite(PhaseThreeProjectilePath);

        if (visualPrefab == null)
        {
            throw new InvalidOperationException(
                $"Phase 3 enemy visual prefab is missing: {PhaseThreeEnemyVisualPath}");
        }

        Transform arena = boss.transform.parent;
        Transform pressureRoot = arena != null ? arena.Find("Phase3EnemyPressure") : null;

        if (pressureRoot == null)
        {
            pressureRoot = new GameObject("Phase3EnemyPressure").transform;
            pressureRoot.SetParent(arena, false);
        }

        Transform leftSpawn = EnsureSpawnPoint(
            pressureRoot,
            "Phase3AddSpawn_Left",
            new Vector3(-2.5f, -2.92f, 0f));
        Transform rightSpawn = EnsureSpawnPoint(
            pressureRoot,
            "Phase3AddSpawn_Right",
            new Vector3(2.5f, -2.92f, 0f));

        RedOniPhaseThreeAddsController adds =
            pressureRoot.GetComponent<RedOniPhaseThreeAddsController>();

        if (adds == null)
        {
            adds = pressureRoot.gameObject.AddComponent<RedOniPhaseThreeAddsController>();
        }

        adds.Configure(
            visualPrefab,
            projectileSprite,
            leftSpawn,
            rightSpawn,
            new Vector2(-3.8f, -1.2f),
            new Vector2(1.2f, 3.8f));
        health.ConfigurePhaseThreeAdds(adds);
        EditorUtility.SetDirty(adds);
        EditorUtility.SetDirty(health);
        return adds;
    }

    private static Transform EnsureSpawnPoint(
        Transform parent,
        string objectName,
        Vector3 worldPosition)
    {
        Transform spawnPoint = parent.Find(objectName);

        if (spawnPoint == null)
        {
            spawnPoint = new GameObject(objectName).transform;
            spawnPoint.SetParent(parent, false);
        }

        spawnPoint.position = worldPosition;
        return spawnPoint;
    }

    private static FaithBeanShooter EnsureFaithBeanShooter(PlayerController player)
    {
        EnsureSingleSpriteImport(FaithBeanVisualPath);
        EnsureSingleSpriteImport(FaithBeanTrajectoryPath);
        EnsureSingleSpriteImport(FaithBeanAimIconPath);

        FaithBeanShooter shooter = player.GetComponent<FaithBeanShooter>();

        if (shooter == null)
        {
            shooter = player.gameObject.AddComponent<FaithBeanShooter>();
        }

        shooter.ConfigureVisuals(
            LoadFirstSprite(FaithBeanVisualPath),
            LoadFirstSprite(FaithBeanTrajectoryPath),
            LoadFirstSprite(FaithBeanAimIconPath));
        shooter.ConfigureAimPresentation(30f, 0.22f);
        shooter.ConfigureAudio(AssetDatabase.LoadAssetAtPath<AudioClip>(FaithBeanThrowSfxPath));

        EditorUtility.SetDirty(shooter);
        return shooter;
    }

    private static void EnsureSingleSpriteImport(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer == null)
        {
            throw new InvalidOperationException($"Boss visual texture is missing: {assetPath}");
        }

        bool needsReimport = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || !importer.alphaIsTransparency
            || importer.textureCompression != TextureImporterCompression.Uncompressed
            || importer.filterMode != FilterMode.Point;

        if (!needsReimport)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static RedOniBossHealth EnsureBossCombat(
        GameObject boss,
        RedOniPhaseOneController controller)
    {
        controller.ConfigureCombatAudio(AssetDatabase.LoadAssetAtPath<AudioClip>(RedOniSmashSfxPath));
        RedOniBossHealth health = boss.GetComponent<RedOniBossHealth>();

        if (health == null)
        {
            health = boss.AddComponent<RedOniBossHealth>();
        }

        health.Configure(controller, 60, 40, 20);
        health.ConfigureFinalRush(60f, 20);
        RedOniPhaseMusicController music = boss.GetComponent<RedOniPhaseMusicController>();

        if (music == null)
        {
            music = boss.AddComponent<RedOniPhaseMusicController>();
        }

        AudioClip phaseOneBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(PhaseOneBgmPath);
        music.Configure(
            health,
            phaseOneBgm,
            AssetDatabase.LoadAssetAtPath<AudioClip>(PhaseTwoBgmPath),
            AssetDatabase.LoadAssetAtPath<AudioClip>(PhaseThreeBgmPath));
        GameAudio gameAudio = FindInScene<GameAudio>(boss.scene);

        if (gameAudio != null)
        {
            gameAudio.ConfigureBgm(phaseOneBgm);
            EditorUtility.SetDirty(gameAudio);
        }

        Transform visual = boss.transform.Find("Visual");

        if (visual == null)
        {
            throw new InvalidOperationException("Red Oni Visual child is missing.");
        }

        BoxCollider2D hitTrigger = visual.GetComponent<BoxCollider2D>();

        if (hitTrigger == null)
        {
            hitTrigger = visual.gameObject.AddComponent<BoxCollider2D>();
        }

        hitTrigger.isTrigger = true;
        hitTrigger.size = new Vector2(4.4f, 5.6f);
        hitTrigger.offset = new Vector2(0f, 0.15f);

        EditorUtility.SetDirty(hitTrigger);
        EditorUtility.SetDirty(health);
        EditorUtility.SetDirty(music);
        return health;
    }

    private static void ValidatePlatformRoute(ICollection<string> failures)
    {
        Dictionary<string, BoxCollider2D> colliders = PlatformLayout
            .Select(spec => GameObject.Find(spec.Name))
            .Where(platform => platform != null)
            .ToDictionary(platform => platform.name, platform => platform.GetComponent<BoxCollider2D>());

        foreach (PlatformSpec spec in PlatformLayout)
        {
            if (!colliders.TryGetValue(spec.Name, out BoxCollider2D collider) || collider == null)
            {
                continue;
            }

            PlatformEffector2D effector = collider.GetComponent<PlatformEffector2D>();

            if (!collider.usedByEffector || effector == null || !effector.useOneWay)
            {
                failures.Add($"Boss platform is not configured as a stable upward one-way surface: {spec.Name}.");
            }
        }

        ValidateAscendingStep(colliders, "Platform_L1", "Platform_L2", failures);
        ValidateAscendingStep(colliders, "Platform_L2", "Platform_L3", failures);
        ValidateHorizontalStep(colliders, "Platform_L3", "Platform_R3", failures);
        ValidateAscendingStep(colliders, "Platform_R1", "Platform_R2", failures);
        ValidateAscendingStep(colliders, "Platform_R2", "Platform_R3", failures);
        ValidateHorizontalStep(colliders, "Platform_L1", "Platform_R1", failures);

    }

    private static void ValidateAscendingStep(
        IReadOnlyDictionary<string, BoxCollider2D> colliders,
        string lowerName,
        string upperName,
        ICollection<string> failures)
    {
        if (!colliders.TryGetValue(lowerName, out BoxCollider2D lower)
            || !colliders.TryGetValue(upperName, out BoxCollider2D upper))
        {
            return;
        }

        float rise = upper.bounds.max.y - lower.bounds.max.y;
        float horizontalGap = HorizontalEdgeGap(lower.bounds, upper.bounds);

        // Player jump apex is roughly 2.45 units with jumpForce 12 and gravityScale 3.
        if (rise > 2.2f || horizontalGap > 1.1f)
        {
            failures.Add(
                $"Boss platform step is outside the conservative jump envelope: "
                + $"{lowerName} -> {upperName}, rise={rise:0.00}, gap={horizontalGap:0.00}.");
        }
    }

    private static void ValidateHorizontalStep(
        IReadOnlyDictionary<string, BoxCollider2D> colliders,
        string fromName,
        string toName,
        ICollection<string> failures)
    {
        if (!colliders.TryGetValue(fromName, out BoxCollider2D from)
            || !colliders.TryGetValue(toName, out BoxCollider2D to))
        {
            return;
        }

        float heightDifference = Mathf.Abs(from.bounds.max.y - to.bounds.max.y);
        float horizontalGap = HorizontalEdgeGap(from.bounds, to.bounds);

        if (heightDifference > 0.1f || horizontalGap > 2.6f)
        {
            failures.Add(
                $"Boss upper crossing is outside the conservative jump envelope: "
                + $"{fromName} -> {toName}, height={heightDifference:0.00}, gap={horizontalGap:0.00}.");
        }
    }

    private static float HorizontalEdgeGap(Bounds first, Bounds second)
    {
        return Mathf.Max(0f, Mathf.Max(first.min.x, second.min.x) - Mathf.Min(first.max.x, second.max.x));
    }

    private static Sprite LoadFirstSprite(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (sprite != null)
        {
            return sprite;
        }

        return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
    }

    private static void AddSceneToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

        if (scenes.All(scene => scene.path != BossScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(BossScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    private static void RequireObject(string name, ICollection<string> failures)
    {
        if (GameObject.Find(name) == null)
        {
            failures.Add($"Required scene object is missing: {name}");
        }
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);

            if (component != null)
            {
                return component;
            }
        }

        return null;
    }
}
