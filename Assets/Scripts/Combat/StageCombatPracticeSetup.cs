using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class StageCombatPracticeSetup
{
    private enum PracticeRewardType
    {
        None,
        FaithPoint,
        Heart
    }

    private const string StageName = "Stage_0_0";
    private const string RootName = "CombatPracticeBreakables_Runtime";
    private const string CorruptedLanternSpritePath = "Assets/Art/Tools_icon/prop_corrupted_lantern_01_cutout.png";
    private const string CursedOfferingSpritePath = "Assets/Art/Tools_icon/prop_cursed_offering_pile_01_cutout.png";
    private const string BrokenTalismanSpritePath = "Assets/Art/Tools_icon/prop_broken_talisman_cluster_01_cutout.png";
    private const string CorruptedCrateSpritePath = "Assets/Art/Tools_icon/prop_corrupted_crate_01_cutout.png";

    private static Sprite practiceBlockSprite;
    private static readonly Dictionary<string, Sprite> loadedPracticeSprites = new Dictionary<string, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SpawnPracticeBlocks()
    {
        if (SceneManager.GetActiveScene().name != StageName || GameObject.Find(RootName) != null)
        {
            return;
        }

        GameObject root = new GameObject(RootName);
        GameManager gameManager = GameManager.Instance != null ? GameManager.Instance : Object.FindAnyObjectByType<GameManager>();

        SpawnBlock(root.transform, "PracticeOnly_GhostLantern_01", new Vector2(3.8f, 1.2f), 1, PracticeRewardType.None, 0, gameManager, CorruptedLanternSpritePath, new Color(0.8f, 0.92f, 1f, 0.92f));
        SpawnBlock(root.transform, "PracticeOnly_CursedOfferingPile_02", new Vector2(6.2f, 1.2f), 2, PracticeRewardType.None, 0, gameManager, CursedOfferingSpritePath, new Color(0.72f, 0.86f, 1f, 0.94f));
        SpawnBlock(root.transform, "Reward_CursedTalismanBundle_01", new Vector2(9.2f, 1.2f), 1, PracticeRewardType.FaithPoint, 1, gameManager, BrokenTalismanSpritePath, new Color(1f, 0.86f, 0.5f, 0.95f));
        SpawnBlock(root.transform, "Reward_CorruptedCrateFlame_Heart_01", new Vector2(11.4f, 1.2f), 2, PracticeRewardType.Heart, 1, gameManager, CorruptedCrateSpritePath, new Color(1f, 0.62f, 0.66f, 0.96f));
    }

    private static void SpawnBlock(Transform parent, string name, Vector2 position, int hitPoints, PracticeRewardType rewardType, int rewardAmount, GameManager gameManager, string spriteAssetPath, Color fallbackColor)
    {
        GameObject blockObject = new GameObject(name);
        blockObject.transform.SetParent(parent, false);
        blockObject.transform.position = position;

        Sprite practiceSprite = LoadPracticeSprite(spriteAssetPath);
        bool usingImportedSprite = practiceSprite != null;
        blockObject.transform.localScale = usingImportedSprite ? new Vector3(0.085f, 0.085f, 1f) : new Vector3(0.58f, 0.64f, 1f);

        SpriteRenderer spriteRenderer = blockObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = usingImportedSprite ? practiceSprite : GetPracticeBlockSprite();
        spriteRenderer.color = usingImportedSprite ? Color.white : fallbackColor;
        spriteRenderer.sortingOrder = 5;

        BoxCollider2D collider = blockObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = usingImportedSprite ? new Vector2(8.2f, 7.8f) : new Vector2(1f, 1f);
        collider.offset = usingImportedSprite ? new Vector2(0f, -0.2f) : Vector2.zero;

        BreakableBlock breakableBlock = blockObject.AddComponent<BreakableBlock>();

        if (rewardType == PracticeRewardType.FaithPoint)
        {
            breakableBlock.ConfigureFaithPointDrop(hitPoints, rewardAmount, gameManager);
        }
        else if (rewardType == PracticeRewardType.Heart)
        {
            breakableBlock.ConfigureHeartDrop(hitPoints, rewardAmount, gameManager);
        }
        else
        {
            breakableBlock.Configure(hitPoints, 0, 0, gameManager);
        }
    }

    private static Sprite LoadPracticeSprite(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        if (loadedPracticeSprites.TryGetValue(assetPath, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite loadedSprite = null;

#if UNITY_EDITOR
        loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (loadedSprite == null)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            if (texture != null)
            {
                loadedSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                loadedSprite.name = $"{texture.name}_RuntimeSprite";
            }
        }
#endif

        loadedPracticeSprites[assetPath] = loadedSprite;
        return loadedSprite;
    }

    private static Sprite GetPracticeBlockSprite()
    {
        if (practiceBlockSprite != null)
        {
            return practiceBlockSprite;
        }

        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        texture.name = "RuntimePracticeBlock";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                bool border = x == 0 || y == 0 || x == texture.width - 1 || y == texture.height - 1;
                bool slash = x == y || x == texture.width - y - 1;
                texture.SetPixel(x, y, border || slash ? Color.white : new Color(1f, 1f, 1f, 0.48f));
            }
        }

        texture.Apply();
        practiceBlockSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 16f);
        practiceBlockSprite.name = "RuntimePracticeBlockSprite";
        return practiceBlockSprite;
    }
}
