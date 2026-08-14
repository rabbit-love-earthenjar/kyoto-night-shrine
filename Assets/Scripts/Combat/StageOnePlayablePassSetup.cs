using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class StageOnePlayablePassSetup : MonoBehaviour
{
    [Header("Existing assets")]
    [SerializeField] private GameObject groundEnemyPrefab;
    [SerializeField] private GameObject flyingEnemyPrefab;
    [SerializeField] private GameObject rangedEnemyVisualPrefab;

    [Header("Existing scene templates")]
    [SerializeField] private GameObject stoneTemplate;
    [SerializeField] private GameObject woodTemplate;
    [SerializeField] private GameObject cloudTemplate;
    [SerializeField] private GameObject toriiTemplate;
    [SerializeField] private GameObject endGateTemplate;
    [SerializeField] private GameObject starSealTemplate;
    [SerializeField] private GameObject faithPointTemplate;

    [Header("Route visuals")]
    [SerializeField] private Sprite stonePlatformSprite;
    [SerializeField] private Sprite cloudPlatformSprite;
    [SerializeField] private Sprite entranceToriiSprite;
    [SerializeField] private Sprite goalToriiSprite;
    [SerializeField] private Sprite corruptedCrateSprite;
    [SerializeField] private Sprite talismanSpikeSprite;
    [SerializeField] private Sprite[] redOniFrames;

    [Header("Route tuning")]
    [SerializeField] private float lowerRouteY = 0f;
    [SerializeField] private float upperRouteY = 8f;
    [SerializeField] private float cloudDisappearDelay = 1.6f;
    [SerializeField] private float cloudRecoveryDelay = 2.8f;

    private const string RuntimeRootName = "Stage1_RoutePrototype_V26";
    private const float PlayerVisualWorldHeight = 1.8f;
    private const float GroundEnemyVisualWorldHeight = 1.35f;
    private const float FlyingEnemyVisualWorldHeight = 1.2f;
    private const float StarSealVisualWorldHeight = 0.72f;
    private static readonly string[] SupersededRuntimeRootNames =
    {
        "Stage1_RoutePrototype",
        "Stage1_RoutePrototype_V2",
        "Stage1_RoutePrototype_V3",
        "Stage1_RoutePrototype_V4",
        "Stage1_RoutePrototype_V5",
        "Stage1_RoutePrototype_V6",
        "Stage1_RoutePrototype_V7",
        "Stage1_RoutePrototype_V8",
        "Stage1_RoutePrototype_V9",
        "Stage1_RoutePrototype_V10",
        "Stage1_RoutePrototype_V11",
        "Stage1_RoutePrototype_V12",
        "Stage1_RoutePrototype_V13",
        "Stage1_RoutePrototype_V14",
        "Stage1_RoutePrototype_V15",
        "Stage1_RoutePrototype_V16",
        "Stage1_RoutePrototype_V17",
        "Stage1_RoutePrototype_V18",
        "Stage1_RoutePrototype_V19",
        "Stage1_RoutePrototype_V20",
        "Stage1_RoutePrototype_V21",
        "Stage1_RoutePrototype_V22",
        "Stage1_RoutePrototype_V23",
        "Stage1_RoutePrototype_V24",
        "Stage1_RoutePrototype_V25"
    };
    private GameManager gameManager;
    private PlayerController player;

    private void OnEnable()
    {
        Scene owningScene = gameObject.scene;

        if (!owningScene.IsValid()
            || !owningScene.isLoaded
            || owningScene.name != "Stage_1_Route_Prototype")
        {
            return;
        }

        foreach (string rootName in SupersededRuntimeRootNames)
        {
            GameObject previousRoot = FindSceneObject(rootName);

            if (previousRoot == null)
            {
                continue;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(previousRoot);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
            else
            {
                Destroy(previousRoot);
            }
#else
            Destroy(previousRoot);
#endif
        }

        if (FindSceneObject(RuntimeRootName) != null)
        {
            return;
        }

        ResolveExistingAssets();

        if (stoneTemplate == null || cloudTemplate == null || starSealTemplate == null)
        {
            Debug.LogError(
                $"Stage route prototype template lookup failed. "
                + $"Stone={stoneTemplate != null}, Cloud={cloudTemplate != null}, StarSeal={starSealTemplate != null}.");
            return;
        }

        DisableSupersededStageObjects();
        ConfigureSelectiveBackgroundCombination();
        BuildPlayableRoute();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            EditorSceneManager.SaveScene(gameObject.scene);
        }
#endif
    }

    private void ResolveExistingAssets()
    {
        stoneTemplate = stoneTemplate != null ? stoneTemplate : FindSceneObject("StartArea_StonePath_12u");
        woodTemplate = woodTemplate != null ? woodTemplate : FindSceneObject("JumpTutorial_LowWoodStep01");
        cloudTemplate = cloudTemplate != null ? cloudTemplate : FindSceneObject("RewardRoute_SpiritualCloud01");
        toriiTemplate = toriiTemplate != null ? toriiTemplate : FindSceneObject("EndGateVisual");
        endGateTemplate = endGateTemplate != null ? endGateTemplate : FindSceneObject("EndGate");
        starSealTemplate = starSealTemplate != null ? starSealTemplate : FindSceneObject("StarSeal_01_CombatReward");
        faithPointTemplate = faithPointTemplate != null
            ? faithPointTemplate
            : FindSceneObject("FaithPointPickup_Reward01");
#if UNITY_EDITOR
        rangedEnemyVisualPrefab = rangedEnemyVisualPrefab != null
            ? rangedEnemyVisualPrefab
            : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LightMonsterVisual.prefab");
        // Always prefer the already-clean transparent platform asset. The similarly named
        // Art/stage_icon source has an opaque background and is not suitable in a side view.
        stonePlatformSprite = ResolveSprite(
            null,
            "Assets/Sprites/Environment/Platforms/stone_stage_icon_transparent.png");
        // Force the clean transparent version so an older serialized reference cannot
        // restore the source image whose checkerboard was baked into the pixels.
        cloudPlatformSprite = ResolveSprite(
            null,
            "Assets/Sprites/Environment/Platforms/cloud_stage_icon_transparent.png");
        // The old sheet cutouts contain a baked checkerboard. Force the existing genuinely
        // transparent gate asset for both route markers instead of reusing stale scene refs.
        entranceToriiSprite = ResolveSprite(
            null,
            "Assets/Sprites/Environment/Platforms/gate_stage_icon_transparent.png");
        goalToriiSprite = ResolveSprite(
            null,
            "Assets/Sprites/Environment/Platforms/gate_stage_icon_transparent.png");
        corruptedCrateSprite = ResolveSprite(
            corruptedCrateSprite,
            "Assets/Art/Tools_icon/prop_corrupted_crate_01_cutout.png");
        talismanSpikeSprite = ResolveSprite(
            talismanSpikeSprite,
            "Assets/Art/Tools_icon/prop_broken_talisman_cluster_01_cutout.png");
        redOniFrames = ResolveRedOniFrames();
#endif
        gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
        player = FindAnyObjectByType<PlayerController>();
    }

    private void DisableSupersededStageObjects()
    {
        GameObject oldEndGate = endGateTemplate;

        if (oldEndGate != null)
        {
            oldEndGate.SetActive(false);
        }

        string[] oldSealNames =
        {
            "StarSeal_01_CombatReward",
            "StarSeal_02_RewardRoute",
            "StarSeal_03_FinalApproach"
        };

        foreach (string sealName in oldSealNames)
        {
            GameObject seal = GameObject.Find(sealName);

            if (seal != null)
            {
                seal.SetActive(false);
            }
        }

        GhostEnemy[] oldEnemies = FindObjectsByType<GhostEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (GhostEnemy enemy in oldEnemies)
        {
            if (enemy.gameObject.scene == gameObject.scene)
            {
                enemy.gameObject.SetActive(false);
            }
        }

        if (toriiTemplate != null)
        {
            toriiTemplate.SetActive(false);
        }

        DisableOldGroup("Geometry");
        DisableOldGroup("SpawnPoints");
        DisableOldGroup("Pickups");
        DisableOldGroup("Goal");
        DisableOldGroup("Notes");
        DisableOldHazardsExceptFallZone();
    }

    private static void DisableOldGroup(string groupName)
    {
        GameObject group = FindSceneObject(groupName);

        if (group != null)
        {
            group.SetActive(false);
        }
    }

    private static void DisableOldHazardsExceptFallZone()
    {
        GameObject hazards = FindSceneObject("Hazards");

        if (hazards == null)
        {
            return;
        }

        foreach (Transform child in hazards.transform)
        {
            if (child.name != "FallZone")
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private static void ConfigureSelectiveBackgroundCombination()
    {
        // Reuse the scene's existing early/middle/late art. One additional middle panel covers
        // the optional cave approach; the late panel is reserved for the far-right shrine area.
        SetBackgroundSection("BG_EarlySection_Background", true, 5f);
        SetBackgroundSection("BG_MiddleLoop_01", true, 29f);
        SetBackgroundSection("BG_MiddleLoop_02", true, 53f);
        SetBackgroundSection("BG_MiddleLoop_03", true, 77f);
        SetBackgroundSection("BG_MiddleLoop_04", true, 101f);
        SetBackgroundSection("BG_MiddleLoop_05", true, 125f);
        SetBackgroundSection("BG_MiddleLoop_06", false, 0f);
        SetBackgroundSection("BG_LateSection_Background", true, 149f);
    }

    private static void SetBackgroundSection(string objectName, bool active, float x)
    {
        GameObject section = FindSceneObject(objectName);

        if (section == null)
        {
            Debug.LogWarning($"Stage route background section is missing: {objectName}");
            return;
        }

        section.SetActive(active);

        if (active)
        {
            Vector3 position = section.transform.position;
            section.transform.position = new Vector3(x, position.y, position.z);
        }
    }

    private void BuildPlayableRoute()
    {
        GameObject root = new GameObject(RuntimeRootName);
        root.transform.SetParent(transform, false);
        CreateSection(root.transform, "HighRewardRoute");
        CreateSection(root.transform, "UpperRoute");
        CreateSection(root.transform, "LowerRoute");
        CreateSection(root.transform, "Goal");
        CreateSection(root.transform, "Boundaries");

        Transform high = root.transform.Find("HighRewardRoute");
        Transform upper = root.transform.Find("UpperRoute");
        Transform lower = root.transform.Find("LowerRoute");
        Transform goal = root.transform.Find("Goal");
        Transform boundaries = root.transform.Find("Boundaries");

        Vector3 playerStart = new Vector3(4f, upperRouteY + 1.1f, 0f);
        MoveStartPoint(playerStart);
        NormalizePlayerVisual();

        // The sketch's black line is solid terrain. Stone visuals stay separate from collision.
        CreateSolidTerrain("Upper_StartSolid", new Vector2(17f, upperRouteY - 1.5f), new Vector2(34f, 3f), upper);
        CreateSolidTerrain("Upper_BeforeSpikes", new Vector2(37f, upperRouteY - 1.5f), new Vector2(6f, 3f), upper);
        CreateHazard("Upper_SpikePit", new Vector2(42f, upperRouteY - 2.2f), new Vector2(4f, 0.8f), upper);
        CreateSolidTerrain("Upper_AfterSpikes", new Vector2(49f, upperRouteY - 1.5f), new Vector2(10f, 3f), upper);
        CreateSolidTerrain("Upper_LowStep", new Vector2(59f, upperRouteY - 1.5f), new Vector2(10f, 3f), upper);
        CreateSolidTerrain("Upper_HighStep", new Vector2(69f, upperRouteY - 0.5f), new Vector2(10f, 5f), upper);
        CreateSolidTerrain("Upper_StepLanding", new Vector2(79f, upperRouteY - 1.5f), new Vector2(10f, 3f), upper);
        CreateSecondCrossingPlatform("Upper_SecondCrossing", new Vector2(87f, upperRouteY + 0.05f), new Vector3(5.5f, 0.55f, 1f), upper);
        // Replace the old long flat finish stretch with four forgiving islands. This adds
        // movement rhythm without extending the scene bounds or requiring precise jumps.
        CreateSolidTerrain("Upper_FinalIsland_01", new Vector2(94.5f, upperRouteY - 1.5f), new Vector2(9f, 3f), upper);
        CreateSolidTerrain("Upper_FinalIsland_02", new Vector2(104f, upperRouteY - 0.5f), new Vector2(7f, 3f), upper);
        CreateSolidTerrain("Upper_FinalIsland_03", new Vector2(113.5f, upperRouteY - 1f), new Vector2(9f, 3f), upper);
        CreateSolidTerrain("Upper_FinalIsland_04", new Vector2(124f, upperRouteY - 1.5f), new Vector2(8f, 3f), upper);
        // The upper route is an out-and-back exploration lane. This visible stone wall removes
        // the unintended far-right descent and directs the return trip over the collapsing span.
        CreateSolidTerrain("Upper_RightReturnWall", new Vector2(129.5f, upperRouteY + 2f), new Vector2(3f, 8f), upper);

        CreateTorii("UpperLeft_EntranceTorii", new Vector2(2.5f, upperRouteY + 0.1f), new Vector2(5f, 4.9f), entranceToriiSprite, upper);
        CreateCratePracticeRoute(upper);
        CreateTalisman("StarSeal_01_UpperRisk", new Vector2(104f, upperRouteY + 2.6f), upper);

        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Upper_01", 17f, upperRouteY, 10f, 24f, upper);
        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Upper_05", 30f, upperRouteY, 25f, 32f, upper);
        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Upper_02", 54f, upperRouteY, 45f, 62f, upper);
        SpawnEnemy(flyingEnemyPrefab, "Ghost_Upper_03", 49f, upperRouteY + 3.2f, 45f, 60f, upper);
        SpawnEnemy(flyingEnemyPrefab, "Ghost_Upper_01", 76f, upperRouteY + 3.1f, 70f, 84f, upper);
        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Upper_03", 95f, upperRouteY, 91f, 98f, upper);
        SpawnEnemy(flyingEnemyPrefab, "Ghost_Upper_04", 105f, upperRouteY + 3.2f, 100f, 110f, upper);
        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Upper_04", 114f, upperRouteY + 0.5f, 110f, 117f, upper);
        SpawnEnemy(flyingEnemyPrefab, "Ghost_Upper_02", 123f, upperRouteY + 3.2f, 118f, 129f, upper);
        SpawnRangedRunner("WispRunner_Upper_01", 24f, upperRouteY, 12f, 32f, upper);
        SpawnRangedRunner("WispRunner_Upper_02", 98f, upperRouteY, 92f, 107f, upper);

        // Optional high routes turn the old upper lane into the readable middle/main route.
        // Both branches climb with forgiving overlaps and drop back onto the main route,
        // so missing a jump costs time but never creates a softlock.
        CreateSolidTerrain("HighRoute_A_Entry", new Vector2(75f, 11f), new Vector2(8f, 2f), high);
        CreateSolidTerrain("HighRoute_A_Middle", new Vector2(84f, 11.8f), new Vector2(7f, 2f), high);
        CreateSolidTerrain("HighRoute_A_Reward", new Vector2(93f, 11f), new Vector2(8f, 2f), high);
        CreateFaithPoint("HighRoute_A_Faith_01", new Vector2(76f, 13f), high);
        CreateFaithPoint("HighRoute_A_Faith_02", new Vector2(84f, 13.8f), high);
        CreateFaithPoint("HighRoute_A_Faith_03", new Vector2(92f, 13f), high);
        SpawnEnemy(flyingEnemyPrefab, "Ghost_HighRoute_A", 86f, 14f, 79f, 94f, high);

        CreateSolidTerrain("HighRoute_B_Entry", new Vector2(109f, 10.3f), new Vector2(7f, 2f), high);
        CreateSolidTerrain("HighRoute_B_Middle", new Vector2(117f, 11f), new Vector2(7f, 2f), high);
        CreateSolidTerrain("HighRoute_B_Reward", new Vector2(124f, 10.5f), new Vector2(7f, 2f), high);
        CreateFaithPoint("HighRoute_B_Faith_01", new Vector2(109f, 12.3f), high);
        CreateFaithPoint("HighRoute_B_Faith_02", new Vector2(117f, 13f), high);
        CreateFaithPoint("HighRoute_B_Faith_03", new Vector2(124f, 12.5f), high);
        SpawnEnemy(flyingEnemyPrefab, "Ghost_HighRoute_B", 118f, 14f, 110f, 126f, high);

        // Lower route is reached only when the upper span collapses on the return crossing.
        CreateSolidTerrain("Lower_RightSolid", new Vector2(116f, lowerRouteY - 1.5f), new Vector2(26f, 3f), lower);
        CreateSolidTerrain("Lower_MiddleSolid_01", new Vector2(92.5f, lowerRouteY - 1f), new Vector2(17f, 2f), lower);
        CreateSolidTerrain("Lower_MiddleSolid_02", new Vector2(77f, lowerRouteY - 0.4f), new Vector2(12f, 1.2f), lower);
        CreateSolidTerrain("Lower_MiddleSolid_03", new Vector2(60f, lowerRouteY - 1f), new Vector2(14f, 2f), lower);
        CreateSolidTerrain("Lower_CloudLanding", new Vector2(38f, lowerRouteY - 1.5f), new Vector2(22f, 3f), lower);

        CreateHazard("Lower_CloudSpikes", new Vector2(22f, lowerRouteY - 1.8f), new Vector2(12f, 0.8f), lower);
        // Keep every cloud inside the open gap between the two stone platforms.
        // Smaller, closely spaced steps read as a temporary bridge without resting on terrain.
        for (int index = 0; index < 11; index++)
        {
            float x = 25.75f - index * 1.45f;
            CreateCloud(
                $"TemporaryCloud_{index + 1:00}",
                new Vector2(x, lowerRouteY),
                new Vector3(1.15f, 0.3f, 1f),
                lower);
        }

        CreateSolidTerrain("Lower_LeftGoalSolid", new Vector2(5f, lowerRouteY - 1.5f), new Vector2(10f, 3f), lower);
        CreateRedOniForeshadow(new Vector2(7.4f, lowerRouteY + 1.1f), goal);

        // The lower-right talisman is an optional cave reward. Its floor stays level with the
        // descent landing so the player can inspect the cave and still return to the main route.
        CreateSolidTerrain("Lower_CaveFloor", new Vector2(140f, lowerRouteY - 1.5f), new Vector2(20f, 3f), lower);
        CreateSolidTerrain("Lower_CaveRoof", new Vector2(142f, lowerRouteY + 4.5f), new Vector2(16f, 2f), lower);
        CreateSolidTerrain("Lower_CaveBackWall", new Vector2(149f, lowerRouteY + 1.75f), new Vector2(2f, 3.5f), lower);

        GameObject caveCrate01 = CreateCrate("LowerCaveCrate_01", new Vector2(136f, lowerRouteY + 0.75f), false, lower);
        GameObject caveCrate02 = CreateCrate("LowerCaveCrate_02", new Vector2(138f, lowerRouteY + 0.75f), false, lower);
        GameObject hiddenCaveTalisman = CreateTalisman(
            "StarSeal_02_HiddenCave",
            new Vector2(144f, lowerRouteY + 1.45f),
            lower);

        GameObject caveRewardController = new GameObject("HiddenCaveRewardReveal");
        caveRewardController.transform.SetParent(lower, false);
        caveRewardController.transform.position = new Vector3(143f, lowerRouteY + 1.5f, 0f);
        caveRewardController.AddComponent<CaveRewardReveal>().Configure(
            hiddenCaveTalisman,
            new[] { caveCrate01, caveCrate02 },
            4.5f);

        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Lower_01", 120f, lowerRouteY, 105f, 128f, lower);
        SpawnEnemy(flyingEnemyPrefab, "Ghost_Lower_03", 114f, lowerRouteY + 3.2f, 108f, 124f, lower);
        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Lower_CaveGuard", 132.5f, lowerRouteY, 130f, 134f, lower);
        SpawnEnemy(flyingEnemyPrefab, "Ghost_Lower_01", 101f, lowerRouteY + 3.2f, 95f, 107f, lower);
        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Lower_02", 92f, lowerRouteY, 88f, 100f, lower);
        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Lower_04", 78f, lowerRouteY + 0.2f, 72f, 82f, lower);
        SpawnEnemy(flyingEnemyPrefab, "Ghost_Lower_02", 72f, lowerRouteY + 3.2f, 66f, 80f, lower);
        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Lower_03", 57f, lowerRouteY, 54f, 66f, lower);
        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Lower_05", 43f, lowerRouteY, 36f, 48f, lower);
        SpawnRangedRunner("WispRunner_Lower_01", 84f, lowerRouteY, 73f, 92f, lower);

        CreateTorii("LowerLeft_ClearTorii", new Vector2(3f, lowerRouteY + 0.1f), new Vector2(4.5f, 4.4f), goalToriiSprite, goal);
        CreateEndGate("LowerLeft_PrototypeEndGate", new Vector2(3f, lowerRouteY + 0.2f), goal);

        CreateWorldBoundary("WorldBoundary_Left", new Vector2(-0.5f, 4f), new Vector2(1f, 26f), boundaries);
        CreateWorldBoundary("WorldBoundary_Right", new Vector2(150.5f, 4f), new Vector2(1f, 26f), boundaries);
        CreateWorldBoundary("WorldBoundary_Top", new Vector2(74.25f, 17f), new Vector2(154.5f, 1f), boundaries);
        ConfigureFallZone();
        ConfigureRouteCamera();

        if (player != null)
        {
            player.transform.position = playerStart;
            player.ResetMotion();
        }
    }

    private void CreateSolidTerrain(string name, Vector2 position, Vector2 size, Transform parent)
    {
        if (stonePlatformSprite == null)
        {
            CreatePlatform(name, position, new Vector3(size.x, size.y, 1f), parent, stoneTemplate);
            return;
        }

        GameObject terrain = new GameObject(name);
        terrain.transform.SetParent(parent, false);
        terrain.transform.position = position;

        BoxCollider2D collider = terrain.AddComponent<BoxCollider2D>();
        collider.size = size;

        const float visibleStoneWidth = 5.6f;
        int segmentCount = Mathf.Max(1, Mathf.CeilToInt(size.x / visibleStoneWidth));
        float segmentWidth = size.x / segmentCount;
        float left = -size.x * 0.5f + segmentWidth * 0.5f;

        for (int index = 0; index < segmentCount; index++)
        {
            float visualWidth = segmentWidth * 1.16f;
            float spriteAspect = stonePlatformSprite.bounds.size.y > 0f
                ? stonePlatformSprite.bounds.size.x / stonePlatformSprite.bounds.size.y
                : 2.4f;
            float visualHeight = Mathf.Min(1.7f, visualWidth / Mathf.Max(0.1f, spriteAspect));
            GameObject visual = CreateSpriteVisual(
                $"StoneVisual_{index + 1:00}",
                stonePlatformSprite,
                terrain.transform,
                new Vector2(visualWidth, visualHeight),
                2);
            visual.transform.localPosition += new Vector3(
                left + index * segmentWidth,
                size.y * 0.5f - visualHeight * 0.5f + 0.08f,
                0f);
        }
    }

    private void CreateCratePracticeRoute(Transform parent)
    {
        // Keep every crate optional: the player can attack through them or jump over them.
        // Spreading the six crates also prevents the former pyramid from sealing the route.
        CreateCrate("UpperCrate_01", new Vector2(25f, upperRouteY + 0.75f), false, parent);
        CreateCrate("UpperCrate_02", new Vector2(27f, upperRouteY + 0.75f), true, parent);
        CreateCrate("UpperCrate_03", new Vector2(29f, upperRouteY + 0.75f), false, parent);
        CreateCrate("UpperCrate_04", new Vector2(48f, upperRouteY + 0.75f), false, parent);
        CreateCrate("UpperCrate_05", new Vector2(80f, upperRouteY + 0.75f), false, parent);
        CreateCrate("UpperCrate_06", new Vector2(121f, upperRouteY + 0.75f), true, parent);
    }

    private static void MoveStartPoint(Vector3 position)
    {
        GameObject startPoint = FindSceneObject("StartPoint");

        if (startPoint != null)
        {
            startPoint.transform.position = position;
        }
    }

    private static void CreateSection(Transform parent, string name)
    {
        GameObject section = new GameObject(name);
        section.transform.SetParent(parent, false);
    }

    private static void CreateWorldBoundary(string name, Vector2 position, Vector2 size, Transform parent)
    {
        GameObject boundary = new GameObject(name);
        boundary.transform.SetParent(parent, false);
        boundary.transform.position = position;
        BoxCollider2D collider = boundary.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private static void ConfigureFallZone()
    {
        GameObject fallZone = FindSceneObject("FallZone");

        if (fallZone == null)
        {
            return;
        }

        fallZone.transform.position = new Vector3(74f, -8f, 0f);
        BoxCollider2D collider = fallZone.GetComponent<BoxCollider2D>();

        if (collider != null)
        {
            collider.isTrigger = true;
            collider.offset = Vector2.zero;
            collider.size = new Vector2(160f, 2f);
        }
    }

    private void ConfigureRouteCamera()
    {
        CameraFollow cameraFollow = FindAnyObjectByType<CameraFollow>();

        if (cameraFollow != null && player != null)
        {
            cameraFollow.ConfigureRouteBounds(player.transform, 8f, 141f, 2.3f, 10.3f);
        }
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Scene activeScene = SceneManager.GetActiveScene();

        foreach (Transform candidate in transforms)
        {
            if (candidate.name == objectName && candidate.gameObject.scene == activeScene)
            {
                return candidate.gameObject;
            }
        }

        return null;
    }

    private GameObject CreatePlatform(string name, Vector2 position, Vector3 scale, Transform parent, GameObject template)
    {
        GameObject source = template != null ? template : stoneTemplate;
        GameObject platform = Instantiate(source, position, Quaternion.identity, parent);
        platform.name = name;
        platform.transform.localScale = scale;
        platform.SetActive(true);
        return platform;
    }

    private void CreateCloud(string name, Vector2 position, Vector3 scale, Transform parent)
    {
        if (cloudPlatformSprite == null)
        {
            GameObject fallbackCloud = CreatePlatform(name, position, scale, parent, cloudTemplate);
            TemporaryCloudPlatform fallbackBehavior = fallbackCloud.GetComponent<TemporaryCloudPlatform>();

            if (fallbackBehavior == null)
            {
                fallbackBehavior = fallbackCloud.AddComponent<TemporaryCloudPlatform>();
            }

            fallbackBehavior.Configure(cloudDisappearDelay, cloudRecoveryDelay);
            return;
        }

        GameObject cloud = new GameObject(name);
        cloud.transform.SetParent(parent, false);
        cloud.transform.position = position;

        BoxCollider2D collider = cloud.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(scale.x, scale.y);

        float spriteAspect = cloudPlatformSprite.bounds.size.y > 0f
            ? cloudPlatformSprite.bounds.size.x / cloudPlatformSprite.bounds.size.y
            : 1.75f;
        float visualWidth = scale.x + 0.225f;
        float visualHeight = visualWidth / Mathf.Max(0.1f, spriteAspect);
        collider.offset = Vector2.up * Mathf.Max(
            0f,
            (visualHeight - collider.size.y) * 0.5f + 0.05f);

        GameObject visual = CreateSpriteVisual(
            "CloudVisual",
            cloudPlatformSprite,
            cloud.transform,
            new Vector2(visualWidth, visualHeight),
            3);
        visual.transform.localPosition += Vector3.up * 0.05f;

        TemporaryCloudPlatform behavior = cloud.GetComponent<TemporaryCloudPlatform>();

        if (behavior == null)
        {
            behavior = cloud.AddComponent<TemporaryCloudPlatform>();
        }

        behavior.Configure(cloudDisappearDelay, cloudRecoveryDelay);
    }

    private void CreateRedOniForeshadow(Vector2 position, Transform parent)
    {
        if (redOniFrames == null || redOniFrames.Length == 0)
        {
            Debug.LogWarning("Red Oni route frames are missing; the goal foreshadow visual was skipped.");
            return;
        }

        GameObject root = new GameObject("RedOni_GoalForeshadow");
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        GameObject visual = CreateSpriteVisual(
            "RedOniVisual",
            redOniFrames[0],
            root.transform,
            new Vector2(2.2f, 2.2f),
            -1);
        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.color = new Color(1f, 1f, 1f, 0.82f);
#if UNITY_EDITOR
        // The foreshadow is background presentation, so keep it out of the shared
        // Sprite-Lit batch. This also makes off-screen Game Camera verification stable.
        renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
#endif
        visual.AddComponent<SpriteFrameAnimator>().Configure(redOniFrames, 3.2f);
        root.AddComponent<VisualPatrolMotion>().Configure(
            5.75f,
            8.4f,
            0.75f,
            renderer,
            true,
            false);
    }

    private void CreateSecondCrossingPlatform(string name, Vector2 position, Vector3 scale, Transform parent)
    {
        GameObject platform = CreatePlatform(name, position, scale, parent, woodTemplate);

        if (platform.GetComponent<SecondCrossingPlatform>() == null)
        {
            platform.AddComponent<SecondCrossingPlatform>();
        }
    }

    private void CreateHazard(string name, Vector2 position, Vector2 scale, Transform parent)
    {
        GameObject hazard = new GameObject(name);
        hazard.transform.SetParent(parent, false);
        hazard.transform.position = position;
        BoxCollider2D collider = hazard.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(scale.x, Mathf.Max(0.45f, scale.y));
        collider.offset = Vector2.up * 0.05f;
        hazard.AddComponent<HazardDamage>();

        if (talismanSpikeSprite == null)
        {
            GameObject fallbackVisual = CreateSpriteVisual(
                "FallbackSpikeVisual",
                RuntimeStageSpriteFactory.GetSpikeSprite(),
                hazard.transform,
                scale,
                4);
            fallbackVisual.transform.localPosition += Vector3.up * 0.1f;
            return;
        }

        const float compressedWidth = 0.72f;
        int visualCount = Mathf.Max(2, Mathf.CeilToInt(scale.x / compressedWidth));
        float spacing = scale.x / visualCount;
        float startX = -scale.x * 0.5f + spacing * 0.5f;

        for (int index = 0; index < visualCount; index++)
        {
            GameObject visual = CreateSpriteVisual(
                $"CompressedTalismanSpike_{index + 1:00}",
                talismanSpikeSprite,
                hazard.transform,
                new Vector2(spacing * 1.12f, 1.05f),
                4);
            visual.transform.localPosition += new Vector3(startX + spacing * index, 0.15f, 0f);
        }
    }

    private GameObject CreateCrate(string name, Vector2 position, bool reward, Transform parent)
    {
        GameObject crate;

        if (corruptedCrateSprite == null)
        {
            crate = CreatePlatform(name, position, new Vector3(1.25f, 1.25f, 1f), parent, woodTemplate);
        }
        else
        {
            crate = new GameObject(name);
            crate.transform.SetParent(parent, false);
            crate.transform.position = position;

            BoxCollider2D collider = crate.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.45f, 1.45f);

            CreateSpriteVisual(
                "CorruptedCrateVisual",
                corruptedCrateSprite,
                crate.transform,
                new Vector2(1.55f, 1.68f),
                5);
        }

        BreakableBlock breakable = crate.GetComponent<BreakableBlock>();

        if (breakable == null)
        {
            breakable = crate.AddComponent<BreakableBlock>();
        }

        if (reward)
        {
            breakable.ConfigureFaithPointDrop(2, 2, gameManager);
        }
        else
        {
            breakable.Configure(2, 0, 0, gameManager);
        }

        return crate;
    }

    private GameObject CreateTalisman(string name, Vector2 position, Transform parent)
    {
        GameObject talisman = Instantiate(starSealTemplate, position, Quaternion.identity, parent);
        talisman.name = name;
        talisman.SetActive(true);
        NormalizeVisualHeight(talisman, StarSealVisualWorldHeight);
        return talisman;
    }

    private GameObject CreateFaithPoint(string name, Vector2 position, Transform parent)
    {
        if (faithPointTemplate == null)
        {
            Debug.LogWarning($"FaithPoint route guide skipped because its scene template is missing: {name}");
            return null;
        }

        GameObject faithPoint = Instantiate(faithPointTemplate, position, Quaternion.identity, parent);
        faithPoint.name = name;
        faithPoint.SetActive(true);
        NormalizeVisualHeight(faithPoint, 0.46f);
        return faithPoint;
    }

    private void NormalizePlayerVisual()
    {
        if (player == null)
        {
            return;
        }

        Transform playerVisual = player.transform.Find("PlayerVisual");

        if (playerVisual != null)
        {
            NormalizeVisualHeight(playerVisual.gameObject, PlayerVisualWorldHeight);
        }
    }

    private float GetPlayerVisualHeight()
    {
        float height = player != null ? GetCombinedSpriteHeight(player.gameObject) : 0f;
        return height > 0.1f ? height : 1.8f;
    }

    private static void NormalizeVisualHeight(GameObject target, float desiredHeight)
    {
        if (target == null || desiredHeight <= 0f)
        {
            return;
        }

        float currentHeight = GetCombinedSpriteHeight(target);

        if (currentHeight <= 0.01f)
        {
            return;
        }

        float factor = desiredHeight / currentHeight;
        target.transform.localScale = new Vector3(
            target.transform.localScale.x * factor,
            target.transform.localScale.y * factor,
            target.transform.localScale.z);
    }

    private static float GetCombinedSpriteHeight(GameObject target)
    {
        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);

        if (renderers.Length == 0)
        {
            return 0f;
        }

        Bounds bounds = default;
        bool hasVisibleSprite = false;

        foreach (SpriteRenderer renderer in renderers)
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.sprite == null)
            {
                continue;
            }

            if (!hasVisibleSprite)
            {
                bounds = renderer.bounds;
                hasVisibleSprite = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasVisibleSprite ? bounds.size.y : 0f;
    }

    private void CreateTorii(string name, Vector2 position, Vector2 visualSize, Sprite sprite, Transform parent)
    {
        if (sprite == null && toriiTemplate == null)
        {
            return;
        }

        if (sprite == null)
        {
            GameObject fallbackTorii = Instantiate(toriiTemplate, position, Quaternion.identity, parent);
            fallbackTorii.name = name;
            fallbackTorii.SetActive(true);
            return;
        }

        GameObject torii = new GameObject(name);
        torii.transform.SetParent(parent, false);
        torii.transform.position = position;
        // The source includes a floating stone base. Rendering behind the real terrain and
        // aligning its internal walk surface to the collider makes the torii read as attached.
        CreateSpriteVisual("ToriiVisual", sprite, torii.transform, visualSize, 1);
    }

    private static GameObject CreateSpriteVisual(
        string name,
        Sprite sprite,
        Transform parent,
        Vector2 worldSize,
        int sortingOrder)
    {
        GameObject visual = new GameObject(name);
        visual.transform.SetParent(parent, false);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = sortingOrder;

        if (sprite != null && sprite.bounds.size.x > 0f && sprite.bounds.size.y > 0f)
        {
            Vector3 localScale = new Vector3(
                worldSize.x / sprite.bounds.size.x,
                worldSize.y / sprite.bounds.size.y,
                1f);
            visual.transform.localScale = localScale;
            visual.transform.localPosition = -Vector3.Scale(sprite.bounds.center, localScale);
        }

        return visual;
    }

#if UNITY_EDITOR
    private static Sprite[] ResolveRedOniFrames()
    {
        Sprite[] sprites = new Sprite[8];

        for (int index = 0; index < sprites.Length; index++)
        {
            string assetPath = $"Assets/Art/red_oni_route_{index:00}.png";
            ConfigureSingleSprite(assetPath);
            sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            if (sprites[index] == null)
            {
                Debug.LogWarning($"Red Oni animation frame is missing: {assetPath}");
            }
        }

        return System.Array.FindAll(sprites, sprite => sprite != null);
    }

    private static void ConfigureSingleSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer == null)
        {
            Debug.LogWarning($"Red Oni frame importer is missing: {assetPath}");
            return;
        }

        bool importerNeedsUpdate = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || !importer.alphaIsTransparency
            || importer.filterMode != FilterMode.Point
            || importer.textureCompression != TextureImporterCompression.Uncompressed
            || importer.wrapMode != TextureWrapMode.Clamp
            || importer.spritePixelsPerUnit != 100f;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.spritePixelsPerUnit = 100f;

        if (importerNeedsUpdate)
        {
            importer.SaveAndReimport();
        }
    }

    private static Sprite ResolveSprite(Sprite current, string assetPath)
    {
        if (current != null)
        {
            return current;
        }

        Sprite directSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (directSprite != null)
        {
            return directSprite;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
            {
                return sprite;
            }
        }

        Debug.LogWarning($"Stage route visual sprite could not be loaded: {assetPath}");
        return null;
    }

    public static void ValidatePrototypeScene()
    {
        const string scenePath = "Assets/Scenes/Stage_1_Route_Prototype.unity";
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject root = FindSceneObject(RuntimeRootName);
        System.Collections.Generic.List<string> failures = new System.Collections.Generic.List<string>();

        if (root == null)
        {
            StageOnePlayablePassSetup[] setups = FindObjectsByType<StageOnePlayablePassSetup>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (setups.Length == 0)
            {
                failures.Add("Stage route setup component is missing.");
            }
            else
            {
                foreach (string oldRootName in SupersededRuntimeRootNames)
                {
                    GameObject oldRoot = FindSceneObject(oldRootName);

                    if (oldRoot != null)
                    {
                        DestroyImmediate(oldRoot);
                    }
                }

                StageOnePlayablePassSetup setup = setups[0];
                setup.ResolveExistingAssets();

                if (setup.stoneTemplate == null || setup.cloudTemplate == null || setup.starSealTemplate == null)
                {
                    failures.Add("Stage route templates could not be resolved for the V10 rebuild.");
                }
                else
                {
                    setup.DisableSupersededStageObjects();
                    ConfigureSelectiveBackgroundCombination();
                    setup.BuildPlayableRoute();
                    EditorSceneManager.MarkSceneDirty(setup.gameObject.scene);
                    EditorSceneManager.SaveScene(setup.gameObject.scene);
                    root = FindSceneObject(RuntimeRootName);
                }
            }
        }

        if (root == null)
        {
            failures.Add($"Missing generated root {RuntimeRootName}.");
        }
        else
        {
            ValidateCount(root, typeof(TemporaryCloudPlatform), 11, "temporary clouds", failures);
            ValidateCount(root, typeof(SecondCrossingPlatform), 1, "collapsing descent platforms", failures);
            ValidateCount(root, typeof(BreakableBlock), 8, "breakable crates", failures);
            ValidateCount(root, typeof(HazardDamage), 2, "hazard zones", failures);
            ValidateCount(root, typeof(GhostEnemy), 20, "route enemies", failures);
            ValidateCount(root, typeof(RangedRunnerEnemy), 3, "ranged route runners", failures);
            ValidateRangedRunnerRoutes(root, failures);
            ValidateEnemyBehavior(root, "UpperRoute/PaperDoll_Upper_01", false, failures);
            ValidateEnemyBehavior(root, "UpperRoute/Ghost_Upper_03", true, failures);
            ValidateEnemyBehavior(root, "LowerRoute/PaperDoll_Lower_04", false, failures);
            ValidateEnemyBehavior(root, "LowerRoute/Ghost_Lower_02", true, failures);
            ValidateEnemyBehavior(root, "HighRewardRoute/Ghost_HighRoute_A", true, failures);
            ValidateEnemyBehavior(root, "HighRewardRoute/Ghost_HighRoute_B", true, failures);
            ValidateGroundVisualSurface(root, "UpperRoute/PaperDoll_Upper_01", 8f, failures);
            ValidateGroundVisualSurface(root, "UpperRoute/PaperDoll_Upper_02", 8f, failures);
            ValidateGroundVisualSurface(root, "UpperRoute/PaperDoll_Upper_03", 8f, failures);
            ValidateGroundVisualSurface(root, "UpperRoute/PaperDoll_Upper_04", 8.5f, failures);
            ValidateGroundVisualSurface(root, "LowerRoute/PaperDoll_Lower_01", 0f, failures);
            ValidateGroundVisualSurface(root, "LowerRoute/PaperDoll_Lower_02", 0f, failures);
            ValidateGroundVisualSurface(root, "LowerRoute/PaperDoll_Lower_03", 0f, failures);
            ValidateGroundVisualSurface(root, "LowerRoute/PaperDoll_Lower_04", 0.2f, failures);
            ValidateGroundVisualSurface(root, "LowerRoute/PaperDoll_Lower_05", 0f, failures);
            ValidateGroundVisualSurface(root, "LowerRoute/PaperDoll_Lower_CaveGuard", 0f, failures);
            ValidateTransformY(root, "UpperRoute/UpperLeft_EntranceTorii", 8.1f, failures);
            ValidateTransformY(root, "Goal/LowerLeft_ClearTorii", 0.1f, failures);
            ValidateNamedSprite(root, "UpperRoute/UpperLeft_EntranceTorii", "gate_stage_icon_transparent", failures);
            ValidateNamedSprite(root, "Goal/LowerLeft_ClearTorii", "gate_stage_icon_transparent", failures);
            ValidateNamedSprite(root, "UpperRoute/UpperCrate_01", "prop_corrupted_crate_01_cutout", failures);
            ValidateNamedSprite(root, "LowerRoute/TemporaryCloud_01", "cloud_stage_icon_transparent", failures);
            ValidateNamedSprite(root, "UpperRoute/Upper_StartSolid", "stone_stage_icon_transparent", failures);
            ValidateRedOniAnimation(root, failures);
            ValidateRedOniPatrol(root, failures);
            ValidateCloudBridgeClearance(root, failures);
            ValidateHighRewardRoutes(root, failures);

            Transform hiddenReward = root.transform.Find("LowerRoute/StarSeal_02_HiddenCave");
            CaveRewardReveal caveReveal = root.GetComponentInChildren<CaveRewardReveal>(true);

            if (hiddenReward == null || hiddenReward.gameObject.activeSelf)
            {
                failures.Add("The lower-right cave talisman must exist and start hidden.");
            }

            if (caveReveal == null || !caveReveal.IsConfigured)
            {
                failures.Add("The lower-right cave reveal controller is missing or incomplete.");
            }

            if (root.transform.Find("LowerRoute/Lower_CaveRoof") == null
                || root.transform.Find("LowerRoute/Lower_CaveBackWall") == null)
            {
                failures.Add("The lower-right cave shell is incomplete.");
            }

            ValidateMatchingPlatformTop(
                root,
                "LowerRoute/Lower_RightSolid",
                "LowerRoute/Lower_CaveFloor",
                failures);

            if (root.transform.Find("RightDrop") != null)
            {
                failures.Add("The obsolete far-right descent route still exists.");
            }

            if (root.transform.Find("UpperRoute/Upper_RightReturnWall") == null)
            {
                failures.Add("The upper return wall is missing, so the far-right edge can bypass the collapsing route.");
            }

            ValidateSelectiveBackgroundCombination(failures);

            ValidateWorldBoundaries(root, failures);

            Transform upperStart = root.transform.Find("UpperRoute/Upper_StartSolid");

            if (upperStart == null || upperStart.GetComponent<BoxCollider2D>() == null)
            {
                failures.Add("Upper start solid terrain is missing its BoxCollider2D.");
            }
        }

        PlayerController routePlayer = FindAnyObjectByType<PlayerController>();

        if (routePlayer == null)
        {
            failures.Add("PlayerController is missing.");
        }
        else
        {
            SerializedObject serializedPlayer = new SerializedObject(routePlayer);
            SerializedProperty moveSpeedProperty = serializedPlayer.FindProperty("moveSpeed");
            SerializedProperty jumpForceProperty = serializedPlayer.FindProperty("jumpForce");
            Rigidbody2D body = routePlayer.GetComponent<Rigidbody2D>();

            if (moveSpeedProperty == null || jumpForceProperty == null || body == null)
            {
                failures.Add("Player movement tuning could not be inspected.");
            }
            else
            {
                float gravity = Mathf.Abs(Physics2D.gravity.y * body.gravityScale);
                float jumpForce = jumpForceProperty.floatValue;
                float horizontalRange = moveSpeedProperty.floatValue * (2f * jumpForce / gravity);
                float jumpApex = jumpForce * jumpForce / (2f * gravity);

                if (horizontalRange < 4.3f)
                {
                    failures.Add($"Required 4.3-unit gap exceeds the calculated {horizontalRange:0.00}-unit jump range.");
                }

                if (jumpApex >= 3f)
                {
                    failures.Add($"The {jumpApex:0.00}-unit jump apex can reverse-climb the intended 3-unit one-way drop.");
                }

                ValidateRouteLinks(root, horizontalRange, jumpApex, failures);
                ValidateVisualScale(root, routePlayer, failures);
            }
        }

        ValidateCameraBounds(failures);

        if (FindSceneObject("LowerLeft_PrototypeEndGate") == null)
        {
            failures.Add("Lower-left EndGate is missing.");
        }

        if (FindSceneObject("FallZone") == null)
        {
            failures.Add("FallZone is missing.");
        }

        if (failures.Count > 0)
        {
            throw new System.InvalidOperationException(
                "Stage route V26 validation failed:\n- " + string.Join("\n- ", failures));
        }

        Debug.Log(
            "Stage route V26 validation passed: three animated ranged runners patrol wide platforms, reposition around the player, and fire telegraphed spirit shots; two optional high reward branches remain above the readable middle/main route and safely return to it, while the lower danger route, animated Red Oni foreshadow, eleven temporary clouds, single collapsing descent, reversible lower-right cave reward, "
            + "selective early/middle/late backgrounds, platform-anchored torii, fixed actor scale, grounded chase "
            + "enemies, flying dive enemies, increased encounter density, world/camera bounds, route links, hazards, "
            + "clouds, crates, FallZone, EndGate, and jump margin are present.");
    }

    private static void ValidateCount(
        GameObject root,
        System.Type componentType,
        int expected,
        string label,
        System.Collections.Generic.List<string> failures)
    {
        int count = root.GetComponentsInChildren(componentType, true).Length;

        if (count != expected)
        {
            failures.Add($"Expected {expected} {label}, found {count}.");
        }
    }

    private static void ValidateRedOniAnimation(
        GameObject root,
        System.Collections.Generic.List<string> failures)
    {
        Transform visual = root.transform.Find("Goal/RedOni_GoalForeshadow/RedOniVisual");
        SpriteFrameAnimator animator = visual != null ? visual.GetComponent<SpriteFrameAnimator>() : null;

        if (animator == null)
        {
            failures.Add("The lower-goal Red Oni animation is missing.");
            return;
        }

        SerializedObject serializedAnimator = new SerializedObject(animator);
        SerializedProperty framesProperty = serializedAnimator.FindProperty("frames");

        if (framesProperty == null || framesProperty.arraySize != 8)
        {
            failures.Add($"The lower-goal Red Oni animation must contain 8 frames, found {framesProperty?.arraySize ?? 0}.");
        }

        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        Transform goalPlatform = root.transform.Find("LowerRoute/Lower_LeftGoalSolid");
        BoxCollider2D goalCollider = goalPlatform != null ? goalPlatform.GetComponent<BoxCollider2D>() : null;

        if (renderer == null || goalCollider == null)
        {
            failures.Add("The Red Oni grounding reference could not be inspected.");
            return;
        }

        float groundTop = goalCollider.bounds.max.y;
        float footClearance = renderer.bounds.min.y - groundTop;

        if (footClearance < -0.15f || footClearance > 0.2f)
        {
            failures.Add($"The Red Oni must sit on the goal platform; foot clearance is {footClearance:0.00} units.");
        }

        PlayerController routePlayer = FindAnyObjectByType<PlayerController>();
        float playerHeight = routePlayer != null ? GetCombinedSpriteHeight(routePlayer.gameObject) : 0f;
        float oniHeight = renderer.bounds.size.y;
        float heightRatio = playerHeight > 0f ? oniHeight / playerHeight : 0f;

        if (heightRatio < 1.18f || heightRatio > 1.28f)
        {
            failures.Add($"The Red Oni visual height must stay near 1.225x the player (about 1.5x visual area); ratio is {heightRatio:0.00}x.");
        }
    }

    private static void ValidateRedOniPatrol(
        GameObject root,
        System.Collections.Generic.List<string> failures)
    {
        Transform oniRoot = root.transform.Find("Goal/RedOni_GoalForeshadow");
        VisualPatrolMotion patrol = oniRoot != null ? oniRoot.GetComponent<VisualPatrolMotion>() : null;
        Transform visual = oniRoot != null ? oniRoot.Find("RedOniVisual") : null;
        SpriteRenderer renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        Transform goalPlatform = root.transform.Find("LowerRoute/Lower_LeftGoalSolid");
        BoxCollider2D goalCollider = goalPlatform != null ? goalPlatform.GetComponent<BoxCollider2D>() : null;

        if (patrol == null || renderer == null || goalCollider == null)
        {
            failures.Add("The Red Oni visual patrol or its goal-platform reference is missing.");
            return;
        }

        if (patrol.MoveSpeed < 0.5f || patrol.MoveSpeed > 1.1f)
        {
            failures.Add($"The Red Oni patrol speed must remain readable and calm; speed is {patrol.MoveSpeed:0.00}.");
        }

        if (patrol.SpriteFacesRightByDefault)
        {
            failures.Add("The Red Oni source art faces left by default, so its patrol facing configuration is reversed.");
        }

        float halfVisualWidth = renderer.bounds.extents.x;

        if (patrol.LeftBoundX - halfVisualWidth < goalCollider.bounds.min.x
            || patrol.RightBoundX + halfVisualWidth > goalCollider.bounds.max.x)
        {
            failures.Add("The Red Oni patrol bounds allow its visible body to leave the lower goal platform.");
        }

        if (oniRoot.position.x < patrol.LeftBoundX || oniRoot.position.x > patrol.RightBoundX)
        {
            failures.Add("The Red Oni must start inside its configured patrol range.");
        }
    }

    private static void ValidateCloudBridgeClearance(
        GameObject root,
        System.Collections.Generic.List<string> failures)
    {
        if (!TryGetColliderBounds(root, "LowerRoute/Lower_CloudLanding", out Bounds landingBounds)
            || !TryGetColliderBounds(root, "LowerRoute/Lower_LeftGoalSolid", out Bounds goalBounds))
        {
            failures.Add("The cloud bridge terrain colliders could not be inspected.");
            return;
        }

        Bounds previousCloudBounds = default;

        for (int index = 0; index < 11; index++)
        {
            string cloudPath = $"LowerRoute/TemporaryCloud_{index + 1:00}";

            if (!TryGetColliderBounds(root, cloudPath, out Bounds cloudBounds))
            {
                failures.Add($"The collider for {cloudPath} could not be inspected.");
                continue;
            }

            if (BoundsOverlapHorizontally(cloudBounds, landingBounds)
                || BoundsOverlapHorizontally(cloudBounds, goalBounds))
            {
                failures.Add($"{cloudPath} must remain fully clear of both stone platforms.");
            }


            float cloudRise = cloudBounds.max.y - landingBounds.max.y;

            if (cloudRise < 0.35f || cloudRise > 0.5f)
            {
                failures.Add($"{cloudPath} must keep its walk surface near the stone platform height; rise is {cloudRise:0.00} units.");
            }

            if (index > 0)
            {
                float gap = previousCloudBounds.min.x - cloudBounds.max.x;

                if (gap < 0.15f || gap > 0.45f)
                {
                    failures.Add($"{cloudPath} has an invalid stepping gap of {gap:0.00} units.");
                }
            }

            previousCloudBounds = cloudBounds;
        }
    }

    private static bool BoundsOverlapHorizontally(Bounds first, Bounds second)
    {
        return Mathf.Min(first.max.x, second.max.x) - Mathf.Max(first.min.x, second.min.x) > 0f;
    }

    private static void ValidateNamedSprite(
        GameObject root,
        string path,
        string expectedSpriteName,
        System.Collections.Generic.List<string> failures)
    {
        Transform target = root.transform.Find(path);
        SpriteRenderer renderer = target != null ? target.GetComponentInChildren<SpriteRenderer>(true) : null;

        if (renderer == null || renderer.sprite == null || !renderer.sprite.name.Contains(expectedSpriteName))
        {
            failures.Add($"{path} is not using {expectedSpriteName}.");
        }
    }

    private static void ValidateMatchingPlatformTop(
        GameObject root,
        string firstPath,
        string secondPath,
        System.Collections.Generic.List<string> failures)
    {
        Transform first = root.transform.Find(firstPath);
        Transform second = root.transform.Find(secondPath);
        BoxCollider2D firstCollider = first != null ? first.GetComponent<BoxCollider2D>() : null;
        BoxCollider2D secondCollider = second != null ? second.GetComponent<BoxCollider2D>() : null;

        if (firstCollider == null || secondCollider == null)
        {
            failures.Add($"Could not compare platform tops for {firstPath} and {secondPath}.");
            return;
        }

        float firstTop = first.position.y + firstCollider.offset.y + firstCollider.size.y * 0.5f;
        float secondTop = second.position.y + secondCollider.offset.y + secondCollider.size.y * 0.5f;

        if (Mathf.Abs(firstTop - secondTop) > 0.05f)
        {
            failures.Add(
                $"The cave detour cannot return safely: platform tops differ by {Mathf.Abs(firstTop - secondTop):0.00} units.");
        }
    }

    private static void ValidateWorldBoundaries(
        GameObject root,
        System.Collections.Generic.List<string> failures)
    {
        string[] boundaryPaths =
        {
            "Boundaries/WorldBoundary_Left",
            "Boundaries/WorldBoundary_Right",
            "Boundaries/WorldBoundary_Top"
        };

        foreach (string path in boundaryPaths)
        {
            Transform boundary = root.transform.Find(path);
            BoxCollider2D collider = boundary != null ? boundary.GetComponent<BoxCollider2D>() : null;

            if (collider == null || collider.isTrigger)
            {
                failures.Add($"Solid world boundary is missing or invalid: {path}.");
            }
        }

        GameObject fallZone = FindSceneObject("FallZone");
        BoxCollider2D fallCollider = fallZone != null ? fallZone.GetComponent<BoxCollider2D>() : null;

        if (fallCollider == null || !fallCollider.isTrigger || fallCollider.bounds.size.x < 155f)
        {
            failures.Add("FallZone does not cover the full horizontal route.");
        }
    }

    private static void ValidateCameraBounds(System.Collections.Generic.List<string> failures)
    {
        CameraFollow cameraFollow = FindAnyObjectByType<CameraFollow>();

        if (cameraFollow == null)
        {
            failures.Add("CameraFollow is missing.");
            return;
        }

        SerializedObject serializedCamera = new SerializedObject(cameraFollow);
        SerializedProperty minX = serializedCamera.FindProperty("minX");
        SerializedProperty maxX = serializedCamera.FindProperty("maxX");
        SerializedProperty minY = serializedCamera.FindProperty("minY");
        SerializedProperty maxY = serializedCamera.FindProperty("maxY");
        SerializedProperty lockVertical = serializedCamera.FindProperty("lockVertical");

        if (minX == null || maxX == null || minY == null || maxY == null || lockVertical == null
            || lockVertical.boolValue
            || minX.floatValue > 8.1f
            || maxX.floatValue < 140.9f
            || minY.floatValue > 2.4f
            || maxY.floatValue < 10.2f)
        {
            failures.Add("Camera bounds do not cover the route levels and the full horizontal stage.");
        }
    }

    private static void ValidateSelectiveBackgroundCombination(
        System.Collections.Generic.List<string> failures)
    {
        (string name, bool active, float x)[] expectedSections =
        {
            ("BG_EarlySection_Background", true, 5f),
            ("BG_MiddleLoop_01", true, 29f),
            ("BG_MiddleLoop_02", true, 53f),
            ("BG_MiddleLoop_03", true, 77f),
            ("BG_MiddleLoop_04", true, 101f),
            ("BG_MiddleLoop_05", true, 125f),
            ("BG_MiddleLoop_06", false, 0f),
            ("BG_LateSection_Background", true, 149f)
        };

        foreach ((string name, bool active, float x) expected in expectedSections)
        {
            GameObject section = FindSceneObject(expected.name);

            if (section == null)
            {
                failures.Add($"Background section is missing: {expected.name}.");
                continue;
            }

            if (section.activeSelf != expected.active)
            {
                failures.Add($"Background section active state is incorrect: {expected.name}.");
            }

            if (expected.active && Mathf.Abs(section.transform.position.x - expected.x) > 0.05f)
            {
                failures.Add($"Background section position is incorrect: {expected.name}.");
            }
        }
    }

    private static void ValidateRouteLinks(
        GameObject root,
        float horizontalRange,
        float jumpApex,
        System.Collections.Generic.List<string> failures)
    {
        float safeHorizontalGap = Mathf.Min(4.4f, horizontalRange * 0.92f);

        ValidateRouteLink(root, "UpperRoute/Upper_BeforeSpikes", "UpperRoute/Upper_AfterSpikes", safeHorizontalGap, jumpApex, failures);
        ValidateRouteLink(root, "UpperRoute/Upper_LowStep", "UpperRoute/Upper_HighStep", safeHorizontalGap, jumpApex, failures);
        ValidateRouteLink(root, "UpperRoute/Upper_StepLanding", "UpperRoute/Upper_SecondCrossing", safeHorizontalGap, jumpApex, failures);
        ValidateRouteLink(root, "UpperRoute/Upper_SecondCrossing", "UpperRoute/Upper_FinalIsland_01", safeHorizontalGap, jumpApex, failures);
        ValidateRouteLink(root, "UpperRoute/Upper_FinalIsland_01", "UpperRoute/Upper_FinalIsland_02", safeHorizontalGap, jumpApex, failures);
        ValidateRouteLink(root, "UpperRoute/Upper_FinalIsland_02", "UpperRoute/Upper_FinalIsland_03", safeHorizontalGap, jumpApex, failures);
        ValidateRouteLink(root, "UpperRoute/Upper_FinalIsland_03", "UpperRoute/Upper_FinalIsland_04", safeHorizontalGap, jumpApex, failures);
        ValidateRouteLink(root, "LowerRoute/Lower_RightSolid", "LowerRoute/Lower_MiddleSolid_01", safeHorizontalGap, jumpApex, failures);
        ValidateRouteLink(root, "LowerRoute/Lower_MiddleSolid_01", "LowerRoute/Lower_MiddleSolid_02", safeHorizontalGap, jumpApex, failures);
        ValidateRouteLink(root, "LowerRoute/Lower_MiddleSolid_02", "LowerRoute/Lower_MiddleSolid_03", safeHorizontalGap, jumpApex, failures);
        ValidateRouteLink(root, "LowerRoute/Lower_MiddleSolid_03", "LowerRoute/Lower_CloudLanding", safeHorizontalGap, jumpApex, failures);
        ValidateRouteLink(root, "LowerRoute/TemporaryCloud_11", "LowerRoute/Lower_LeftGoalSolid", safeHorizontalGap, jumpApex, failures);
        ValidateRouteLink(root, "LowerRoute/Lower_CaveFloor", "LowerRoute/Lower_RightSolid", safeHorizontalGap, jumpApex, failures);

        ValidateHorizontalOverlap(root, "UpperRoute/Upper_SecondCrossing", "LowerRoute/Lower_MiddleSolid_01", 2f, failures);
    }

    private static void ValidateHighRewardRoutes(
        GameObject root,
        System.Collections.Generic.List<string> failures)
    {
        string[] requiredPlatforms =
        {
            "HighRewardRoute/HighRoute_A_Entry",
            "HighRewardRoute/HighRoute_A_Middle",
            "HighRewardRoute/HighRoute_A_Reward",
            "HighRewardRoute/HighRoute_B_Entry",
            "HighRewardRoute/HighRoute_B_Middle",
            "HighRewardRoute/HighRoute_B_Reward"
        };

        foreach (string path in requiredPlatforms)
        {
            if (!TryGetColliderBounds(root, path, out _))
            {
                failures.Add($"High reward route platform is missing collision: {path}.");
            }
        }

        string[] faithPointPaths =
        {
            "HighRewardRoute/HighRoute_A_Faith_01",
            "HighRewardRoute/HighRoute_A_Faith_02",
            "HighRewardRoute/HighRoute_A_Faith_03",
            "HighRewardRoute/HighRoute_B_Faith_01",
            "HighRewardRoute/HighRoute_B_Faith_02",
            "HighRewardRoute/HighRoute_B_Faith_03"
        };

        foreach (string path in faithPointPaths)
        {
            Transform pickup = root.transform.Find(path);

            if (pickup == null || pickup.GetComponent<PickupItem>() == null)
            {
                failures.Add($"High reward route FaithPoint is missing or invalid: {path}.");
            }
        }

        const float safeGap = 4.4f;
        const float jumpApex = 2.8f;
        ValidateRouteLink(root, "UpperRoute/Upper_HighStep", "HighRewardRoute/HighRoute_A_Entry", safeGap, jumpApex, failures);
        ValidateRouteLink(root, "HighRewardRoute/HighRoute_A_Entry", "HighRewardRoute/HighRoute_A_Middle", safeGap, jumpApex, failures);
        ValidateRouteLink(root, "HighRewardRoute/HighRoute_A_Middle", "HighRewardRoute/HighRoute_A_Reward", safeGap, jumpApex, failures);
        ValidateRouteLink(root, "UpperRoute/Upper_FinalIsland_02", "HighRewardRoute/HighRoute_B_Entry", safeGap, jumpApex, failures);
        ValidateRouteLink(root, "HighRewardRoute/HighRoute_B_Entry", "HighRewardRoute/HighRoute_B_Middle", safeGap, jumpApex, failures);
        ValidateRouteLink(root, "HighRewardRoute/HighRoute_B_Middle", "HighRewardRoute/HighRoute_B_Reward", safeGap, jumpApex, failures);
    }

    private static void ValidateRouteLink(
        GameObject root,
        string fromPath,
        string toPath,
        float safeHorizontalGap,
        float jumpApex,
        System.Collections.Generic.List<string> failures)
    {
        if (!TryGetColliderBounds(root, fromPath, out Bounds fromBounds)
            || !TryGetColliderBounds(root, toPath, out Bounds toBounds))
        {
            failures.Add($"Route link is missing collision: {fromPath} -> {toPath}.");
            return;
        }

        float horizontalGap = Mathf.Max(
            0f,
            Mathf.Max(fromBounds.min.x, toBounds.min.x) - Mathf.Min(fromBounds.max.x, toBounds.max.x));
        float upwardStep = toBounds.max.y - fromBounds.max.y;

        if (horizontalGap > safeHorizontalGap + 0.05f)
        {
            failures.Add($"Route gap is too wide ({horizontalGap:0.00}): {fromPath} -> {toPath}.");
        }

        if (upwardStep > jumpApex - 0.1f)
        {
            failures.Add($"Route step is too high ({upwardStep:0.00}): {fromPath} -> {toPath}.");
        }
    }

    private static void ValidateHorizontalOverlap(
        GameObject root,
        string firstPath,
        string secondPath,
        float minimumOverlap,
        System.Collections.Generic.List<string> failures)
    {
        if (!TryGetColliderBounds(root, firstPath, out Bounds firstBounds)
            || !TryGetColliderBounds(root, secondPath, out Bounds secondBounds))
        {
            failures.Add($"Drop route collision is missing: {firstPath} -> {secondPath}.");
            return;
        }

        float overlap = Mathf.Min(firstBounds.max.x, secondBounds.max.x)
            - Mathf.Max(firstBounds.min.x, secondBounds.min.x);

        if (overlap < minimumOverlap)
        {
            failures.Add($"Drop route overlap is too narrow ({overlap:0.00}): {firstPath} -> {secondPath}.");
        }
    }

    private static bool TryGetColliderBounds(GameObject root, string path, out Bounds bounds)
    {
        Transform target = root.transform.Find(path);
        Collider2D collider = target != null ? target.GetComponentInChildren<Collider2D>(true) : null;

        if (collider == null)
        {
            bounds = default;
            return false;
        }

        bounds = collider.bounds;
        return true;
    }

    private static void ValidateVisualScale(
        GameObject root,
        PlayerController routePlayer,
        System.Collections.Generic.List<string> failures)
    {
        float playerHeight = GetCombinedSpriteHeight(routePlayer.gameObject);

        if (playerHeight <= 0.1f)
        {
            failures.Add("Player visual height could not be measured.");
            return;
        }

        ValidateVisualRatio(root, "UpperRoute/PaperDoll_Upper_01", playerHeight, 0.62f, 0.9f, failures);
        ValidateVisualRatio(root, "UpperRoute/Ghost_Upper_01", playerHeight, 0.55f, 0.82f, failures);
        ValidateVisualRatio(root, "UpperRoute/StarSeal_01_UpperRisk", playerHeight, 0.36f, 0.6f, failures);
    }

    private static void ValidateEnemyBehavior(
        GameObject root,
        string path,
        bool expectedFlying,
        System.Collections.Generic.List<string> failures)
    {
        Transform target = root.transform.Find(path);
        GhostEnemy enemy = target != null ? target.GetComponent<GhostEnemy>() : null;

        if (enemy == null)
        {
            failures.Add($"Enemy behavior is missing for {path}.");
            return;
        }

        SerializedObject serializedEnemy = new SerializedObject(enemy);
        SerializedProperty stateMachine = serializedEnemy.FindProperty("useStateMachine");
        SerializedProperty movementMode = serializedEnemy.FindProperty("movementMode");
        SerializedProperty routeBounds = serializedEnemy.FindProperty("useRouteMovementBounds");
        SerializedProperty detectRange = serializedEnemy.FindProperty("detectRange");
        SerializedProperty diveSpeed = serializedEnemy.FindProperty("flyingDiveSpeed");

        bool validMode = movementMode != null
            && movementMode.enumValueIndex == (expectedFlying ? 0 : 1);

        if (stateMachine == null || !stateMachine.boolValue
            || routeBounds == null || !routeBounds.boolValue
            || detectRange == null || detectRange.floatValue < 4f
            || !validMode)
        {
            failures.Add($"Route chase configuration is incomplete for {path}.");
        }

        if (expectedFlying && (diveSpeed == null || diveSpeed.floatValue < 3f))
        {
            failures.Add($"Flying dive speed is not configured for {path}.");
        }
    }

    private static void ValidateRangedRunnerRoutes(
        GameObject root,
        System.Collections.Generic.List<string> failures)
    {
        RangedRunnerEnemy[] runners = root.GetComponentsInChildren<RangedRunnerEnemy>(true);

        foreach (RangedRunnerEnemy runner in runners)
        {
            if (runner.PatrolWidth < 6f)
            {
                failures.Add(
                    $"Ranged runner {runner.name} has an invalid saved patrol width "
                    + $"({runner.PatrolWidth:0.00}).");
            }
        }
    }

    private static void ValidateGroundVisualSurface(
        GameObject root,
        string path,
        float surfaceY,
        System.Collections.Generic.List<string> failures)
    {
        Transform target = root.transform.Find(path);
        SpriteRenderer[] renderers = target != null
            ? target.GetComponentsInChildren<SpriteRenderer>(true)
            : new SpriteRenderer[0];
        bool hasBounds = false;
        Bounds bounds = default;

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

        if (!hasBounds || Mathf.Abs(bounds.min.y - surfaceY) > 0.08f)
        {
            failures.Add($"Ground enemy visual is not foot-aligned for {path}.");
        }
    }

    private static void ValidateTransformY(
        GameObject root,
        string path,
        float expectedY,
        System.Collections.Generic.List<string> failures)
    {
        Transform target = root.transform.Find(path);

        if (target == null || Mathf.Abs(target.position.y - expectedY) > 0.05f)
        {
            failures.Add($"Route visual anchor is incorrect for {path}.");
        }
    }

    private static void ValidateVisualRatio(
        GameObject root,
        string path,
        float playerHeight,
        float minimumRatio,
        float maximumRatio,
        System.Collections.Generic.List<string> failures)
    {
        Transform target = root.transform.Find(path);
        float visualHeight = target != null ? GetCombinedSpriteHeight(target.gameObject) : 0f;
        float ratio = playerHeight > 0f ? visualHeight / playerHeight : 0f;

        if (ratio < minimumRatio || ratio > maximumRatio)
        {
            failures.Add($"Visual scale ratio is out of range for {path}: {ratio:0.00} of player height.");
        }
    }
#endif

    private void CreateEndGate(string name, Vector2 position, Transform parent)
    {
        if (endGateTemplate == null)
        {
            Debug.LogWarning("Stage route prototype could not find the existing EndGate template.");
            return;
        }

        GameObject endGate = Instantiate(endGateTemplate, position, Quaternion.identity, parent);
        endGate.name = name;
        endGate.SetActive(true);
    }

    private void SpawnEnemy(
        GameObject prefab,
        string name,
        float x,
        float anchorY,
        float minimumX,
        float maximumX,
        Transform parent)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"Stage 1 skipped {name}: enemy prefab reference is missing.");
            return;
        }

        bool isFlyingEnemy = prefab == flyingEnemyPrefab;
        Vector2 spawnPosition = new Vector2(x, isFlyingEnemy ? anchorY : anchorY + 1f);
        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity, parent);
        enemy.name = name;
        enemy.SetActive(true);
        NormalizeVisualHeight(
            enemy,
            isFlyingEnemy ? FlyingEnemyVisualWorldHeight : GroundEnemyVisualWorldHeight);

        if (!isFlyingEnemy)
        {
            AlignVisualBottomToSurface(enemy, anchorY);
        }

        GhostEnemy enemyBehavior = enemy.GetComponent<GhostEnemy>();

        if (enemyBehavior != null)
        {
            enemyBehavior.ConfigureRouteBehavior(
                isFlyingEnemy,
                minimumX,
                maximumX,
                enemy.transform.position.y);
        }
    }

    private void SpawnRangedRunner(
        string name,
        float x,
        float anchorY,
        float minimumX,
        float maximumX,
        Transform parent)
    {
        if (rangedEnemyVisualPrefab == null)
        {
            Debug.LogWarning($"Stage 1 skipped {name}: ranged enemy visual prefab is missing.");
            return;
        }

        GameObject enemy = Instantiate(
            rangedEnemyVisualPrefab,
            new Vector2(x, anchorY + 0.8f),
            Quaternion.identity,
            parent);
        enemy.name = name;
        enemy.SetActive(true);
        NormalizeVisualHeight(enemy, 1.15f);

        SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
        BoxCollider2D collider = enemy.GetComponent<BoxCollider2D>();

        if (collider == null)
        {
            collider = enemy.AddComponent<BoxCollider2D>();
        }

        if (renderer != null && renderer.sprite != null)
        {
            Vector2 spriteSize = renderer.sprite.bounds.size;
            collider.size = new Vector2(spriteSize.x * 0.58f, spriteSize.y * 0.72f);
        }

        collider.isTrigger = true;
        AlignVisualBottomToSurface(enemy, anchorY);

        RangedRunnerEnemy runner = enemy.AddComponent<RangedRunnerEnemy>();
        enemy.AddComponent<GhostHealth>();

        Sprite projectileSprite = null;
        SpriteRenderer faithRenderer = faithPointTemplate != null
            ? faithPointTemplate.GetComponentInChildren<SpriteRenderer>(true)
            : null;

        if (faithRenderer != null)
        {
            projectileSprite = faithRenderer.sprite;
        }

        runner.ConfigureRoute(minimumX, maximumX, enemy.transform.position.y, projectileSprite);
    }

    private static void AlignVisualBottomToSurface(GameObject target, float surfaceY)
    {
        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;

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

        if (!hasBounds)
        {
            return;
        }

        Vector3 position = target.transform.position;
        position.y += surfaceY - bounds.min.y;
        target.transform.position = position;
    }

}
