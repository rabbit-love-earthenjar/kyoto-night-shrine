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

    [Header("Existing scene templates")]
    [SerializeField] private GameObject stoneTemplate;
    [SerializeField] private GameObject woodTemplate;
    [SerializeField] private GameObject cloudTemplate;
    [SerializeField] private GameObject toriiTemplate;
    [SerializeField] private GameObject endGateTemplate;
    [SerializeField] private GameObject starSealTemplate;
    [SerializeField] private Sprite grassTerrainSprite;

    [Header("Route tuning")]
    [SerializeField] private float lowerRouteY = 0f;
    [SerializeField] private float upperRouteY = 8f;
    [SerializeField] private float cloudDisappearDelay = 1f;
    [SerializeField] private float cloudRecoveryDelay = 2.8f;

    private const string PreviousRuntimeRootName = "Stage1_RoutePrototype_V2";
    private const string RuntimeRootName = "Stage1_RoutePrototype_V3";
    private GameManager gameManager;
    private PlayerController player;

    private void OnEnable()
    {
        if (SceneManager.GetActiveScene().name != "Stage_1_Route_Prototype")
        {
            return;
        }

        GameObject previousRoot = FindSceneObject(PreviousRuntimeRootName);

        if (previousRoot != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(previousRoot);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
                EditorSceneManager.SaveScene(gameObject.scene);
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
#if UNITY_EDITOR
        grassTerrainSprite = grassTerrainSprite != null
            ? grassTerrainSprite
            : AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Backgrounds/Cutouts/sheet2_01_grass_tile_01.png");
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

    private void BuildPlayableRoute()
    {
        GameObject root = new GameObject(RuntimeRootName);
        root.transform.SetParent(transform, false);
        CreateSection(root.transform, "UpperRoute");
        CreateSection(root.transform, "RightDrop");
        CreateSection(root.transform, "LowerRoute");
        CreateSection(root.transform, "Goal");

        Transform upper = root.transform.Find("UpperRoute");
        Transform drop = root.transform.Find("RightDrop");
        Transform lower = root.transform.Find("LowerRoute");
        Transform goal = root.transform.Find("Goal");

        Vector3 playerStart = new Vector3(4f, upperRouteY + 1.1f, 0f);
        MoveStartPoint(playerStart);

        // The sketch's black line is solid terrain. Grass art is tiled over thick colliders.
        CreateGrassTerrain("Upper_StartSolid", new Vector2(17f, upperRouteY - 1.5f), new Vector2(34f, 3f), upper);
        CreateGrassTerrain("Upper_BeforeSpikes", new Vector2(37f, upperRouteY - 1.5f), new Vector2(6f, 3f), upper);
        CreateHazard("Upper_SpikePit", new Vector2(42f, upperRouteY - 2.2f), new Vector2(4f, 0.8f), upper);
        CreateGrassTerrain("Upper_AfterSpikes", new Vector2(49f, upperRouteY - 1.5f), new Vector2(10f, 3f), upper);
        CreateGrassTerrain("Upper_LowStep", new Vector2(59f, upperRouteY - 1.5f), new Vector2(10f, 3f), upper);
        CreateGrassTerrain("Upper_HighStep", new Vector2(69f, upperRouteY - 0.5f), new Vector2(10f, 5f), upper);
        CreateGrassTerrain("Upper_StepLanding", new Vector2(79f, upperRouteY - 1.5f), new Vector2(10f, 3f), upper);
        CreateSecondCrossingPlatform("Upper_SecondCrossing", new Vector2(87f, upperRouteY + 0.05f), new Vector3(5.5f, 0.55f, 1f), upper);
        CreateGrassTerrain("Upper_FinalSolid", new Vector2(109f, upperRouteY - 1.5f), new Vector2(38f, 3f), upper);

        CreateTorii("UpperLeft_EntranceTorii", new Vector2(2f, upperRouteY + 1.25f), upper);
        CreateCratePyramid(new Vector2(27f, upperRouteY + 0.8f), upper);
        CreateTalisman("StarSeal_01_UpperRisk", new Vector2(103f, upperRouteY + 2f), upper);

        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Upper_01", new Vector2(17f, upperRouteY + 1.1f), upper);
        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Upper_02", new Vector2(54f, upperRouteY + 1.1f), upper);
        SpawnEnemy(flyingEnemyPrefab, "Ghost_Upper_01", new Vector2(76f, upperRouteY + 3.1f), upper);
        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Upper_03", new Vector2(101f, upperRouteY + 1.1f), upper);
        SpawnEnemy(flyingEnemyPrefab, "Ghost_Upper_02", new Vector2(117f, upperRouteY + 3.2f), upper);

        // The far-right route only descends. Each drop is taller than the player's jump apex.
        CreateGrassTerrain("RightDrop_UpperLedge", new Vector2(130f, upperRouteY - 1.5f), new Vector2(4f, 3f), drop);
        CreateGrassTerrain("RightDrop_MiddleLedge", new Vector2(134f, upperRouteY - 4.5f), new Vector2(4f, 3f), drop);
        CreateGrassTerrain("RightDrop_LowerLedge", new Vector2(130f, lowerRouteY + 1.5f), new Vector2(4f, 3f), drop);

        // Lower route is travelled right-to-left after the one-way descent.
        CreateGrassTerrain("Lower_RightSolid", new Vector2(116f, lowerRouteY - 1.5f), new Vector2(26f, 3f), lower);
        CreateGrassTerrain("Lower_MiddleSolid_01", new Vector2(94f, lowerRouteY - 1f), new Vector2(14f, 2f), lower);
        CreateGrassTerrain("Lower_MiddleSolid_02", new Vector2(77f, lowerRouteY - 0.4f), new Vector2(12f, 1.2f), lower);
        CreateGrassTerrain("Lower_MiddleSolid_03", new Vector2(60f, lowerRouteY - 1f), new Vector2(14f, 2f), lower);
        CreateGrassTerrain("Lower_CloudLanding", new Vector2(38f, lowerRouteY - 1.5f), new Vector2(22f, 3f), lower);

        CreateHazard("Lower_CloudSpikes", new Vector2(22f, lowerRouteY - 1.8f), new Vector2(12f, 0.8f), lower);
        for (int index = 0; index < 4; index++)
        {
            float x = 28f - index * 4f;
            CreateCloud($"TemporaryCloud_{index + 1:00}", new Vector2(x, lowerRouteY + 0.7f), new Vector3(3.4f, 0.4f, 1f), lower);
        }

        CreateGrassTerrain("Lower_LeftGoalSolid", new Vector2(5f, lowerRouteY - 1.5f), new Vector2(10f, 3f), lower);
        CreateCrate("LowerCrate_01", new Vector2(105f, lowerRouteY + 1.1f), false, lower);
        CreateCrate("LowerCrate_02", new Vector2(107f, lowerRouteY + 1.1f), true, lower);
        CreateTalisman("StarSeal_02_LowerRoute", new Vector2(112f, lowerRouteY + 1.8f), lower);

        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Lower_01", new Vector2(120f, lowerRouteY + 1.1f), lower);
        SpawnEnemy(flyingEnemyPrefab, "Ghost_Lower_01", new Vector2(101f, lowerRouteY + 3.2f), lower);
        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Lower_02", new Vector2(92f, lowerRouteY + 1.1f), lower);
        SpawnEnemy(flyingEnemyPrefab, "Ghost_Lower_02", new Vector2(72f, lowerRouteY + 3.2f), lower);
        SpawnEnemy(groundEnemyPrefab, "PaperDoll_Lower_03", new Vector2(57f, lowerRouteY + 1.1f), lower);

        CreateTorii("LowerLeft_ClearTorii", new Vector2(3f, lowerRouteY + 1.25f), goal);
        CreateEndGate("LowerLeft_PrototypeEndGate", new Vector2(3f, lowerRouteY + 0.2f), goal);

        if (player != null)
        {
            player.transform.position = playerStart;
            player.ResetMotion();
        }
    }

    private void CreateGrassTerrain(string name, Vector2 position, Vector2 size, Transform parent)
    {
        if (grassTerrainSprite == null)
        {
            CreatePlatform(name, position, new Vector3(size.x, size.y, 1f), parent, stoneTemplate);
            return;
        }

        GameObject terrain = new GameObject(name);
        terrain.transform.SetParent(parent, false);
        terrain.transform.position = position;

        SpriteRenderer renderer = terrain.AddComponent<SpriteRenderer>();
        renderer.sprite = grassTerrainSprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.size = size;
        renderer.sortingOrder = 2;

        BoxCollider2D collider = terrain.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private void CreateCratePyramid(Vector2 basePosition, Transform parent)
    {
        CreateCrate("UpperCrate_01", basePosition + new Vector2(-2f, 0f), false, parent);
        CreateCrate("UpperCrate_02", basePosition, true, parent);
        CreateCrate("UpperCrate_03", basePosition + new Vector2(2f, 0f), false, parent);
        CreateCrate("UpperCrate_04", basePosition + new Vector2(-1f, 1.8f), false, parent);
        CreateCrate("UpperCrate_05", basePosition + new Vector2(1f, 1.8f), false, parent);
        CreateCrate("UpperCrate_06", basePosition + new Vector2(0f, 3.6f), true, parent);
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
        GameObject cloud = CreatePlatform(name, position, scale, parent, cloudTemplate);
        TemporaryCloudPlatform behavior = cloud.GetComponent<TemporaryCloudPlatform>();

        if (behavior == null)
        {
            behavior = cloud.AddComponent<TemporaryCloudPlatform>();
        }

        behavior.Configure(cloudDisappearDelay, cloudRecoveryDelay);
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
        hazard.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        SpriteRenderer renderer = hazard.AddComponent<SpriteRenderer>();
        renderer.sprite = RuntimeStageSpriteFactory.GetSpikeSprite();
        renderer.color = new Color(0.2f, 0.08f, 0.16f, 1f);
        renderer.sortingOrder = 3;

        BoxCollider2D collider = hazard.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 0.7f);
        hazard.AddComponent<HazardDamage>();
    }

    private void CreateCrate(string name, Vector2 position, bool reward, Transform parent)
    {
        GameObject crate = CreatePlatform(name, position, new Vector3(1.25f, 1.25f, 1f), parent, woodTemplate);
        SpriteRenderer renderer = crate.GetComponent<SpriteRenderer>();

        if (renderer != null)
        {
            renderer.color = new Color(0.72f, 0.42f, 0.22f, 1f);
            renderer.sortingOrder = 5;
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
    }

    private void CreateTalisman(string name, Vector2 position, Transform parent)
    {
        GameObject talisman = Instantiate(starSealTemplate, position, Quaternion.identity, parent);
        talisman.name = name;
        talisman.SetActive(true);
    }

    private void CreateTorii(string name, Vector2 position, Transform parent)
    {
        if (toriiTemplate == null)
        {
            return;
        }

        GameObject torii = Instantiate(toriiTemplate, position, Quaternion.identity, parent);
        torii.name = name;
        torii.SetActive(true);
    }

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

    private static void SpawnEnemy(GameObject prefab, string name, Vector2 position, Transform parent)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"Stage 1 skipped {name}: enemy prefab reference is missing.");
            return;
        }

        GameObject enemy = Instantiate(prefab, position, Quaternion.identity, parent);
        enemy.name = name;
        enemy.SetActive(true);
    }

}
