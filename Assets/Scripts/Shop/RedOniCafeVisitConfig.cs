using System.Collections.Generic;
using UnityEngine;

// Tunable data for the demo v0.1 "night result returns to the day" beat:
// after the Red Oni is defeated it visits the cafe, unlocks one dish, and the
// next cafe result leads to the demo end panel. Text values are placeholders.
[CreateAssetMenu(fileName = "RedOniCafeVisitConfig", menuName = "Night Shrine/Red Oni Cafe Visit Config")]
public class RedOniCafeVisitConfig : ScriptableObject
{
    public const string ResourcePath = "Story/RedOniCafeVisitConfig";
    public const string DishMenuId = "red_oni_spicy_dish";
    public const string DishRecipeId = "RedOniSpicyDish";

    [Header("Cafe visitor")]
    [SerializeField] private string visitorDisplayName = "赤鬼";
    [SerializeField, TextArea(2, 4)] private string visitorLine = "（仮）赤鬼のひとことがここに入ります。";
    [SerializeField] private Sprite visitorSprite;
    [SerializeField] private Vector2 visitorPosition = new Vector2(3.2f, -1.55f);
    [SerializeField] private float visitorScale = 0.48f;
    [SerializeField] private int visitorSortingOrder = 3;

    [Header("Unlocked dish")]
    [SerializeField] private string dishDisplayName = "（仮）赤鬼の激辛料理";
    [SerializeField] private Sprite dishIcon;
    [SerializeField] private float dishCraftSeconds = 5f;
    [SerializeField] private List<CafeIngredientRequirement> dishIngredients = new List<CafeIngredientRequirement>
    {
        new CafeIngredientRequirement(ResourceInventory.FlourId, 1),
        new CafeIngredientRequirement(ResourceInventory.MilkId, 1),
        new CafeIngredientRequirement(ResourceInventory.SugarId, 1)
    };

    [Header("Demo end panel")]
    [SerializeField] private string resultNextButtonLabel = "次へ";
    [SerializeField] private string demoEndTitle = "デモ版はここまで";
    [SerializeField, TextArea(3, 6)] private string demoEndBody =
        "ここまでプレイいただき、ありがとうございました。\n\n（仮）次章の予告がここに入ります。";
    [SerializeField] private string demoEndButtonLabel = "タイトルへ戻る";
    [SerializeField] private string titleSceneName = "StartScene";

    private static RedOniCafeVisitConfig cachedConfig;

    public string VisitorDisplayName => visitorDisplayName;
    public string VisitorLine => visitorLine;
    public Sprite VisitorSprite => visitorSprite;
    public Vector2 VisitorPosition => visitorPosition;
    public float VisitorScale => Mathf.Max(0.01f, visitorScale);
    public int VisitorSortingOrder => visitorSortingOrder;
    public string DishDisplayName => dishDisplayName;
    public Sprite DishIcon => dishIcon;
    public float DishCraftSeconds => Mathf.Max(0.1f, dishCraftSeconds);
    public IReadOnlyList<CafeIngredientRequirement> DishIngredients => dishIngredients;
    public string ResultNextButtonLabel => resultNextButtonLabel;
    public string DemoEndTitle => demoEndTitle;
    public string DemoEndBody => demoEndBody;
    public string DemoEndButtonLabel => demoEndButtonLabel;
    public string TitleSceneName => titleSceneName;

    // The single source for "the night result has reached the day": the existing save story stage.
    public static bool IsRedOniDefeated => SaveManager.HasReachedStoryStage(SaveManager.RedOniDefeatedStoryStage);
    public static bool IsDemoComplete => SaveManager.HasReachedStoryStage(SaveManager.DemoCompleteStoryStage);

    public static RedOniCafeVisitConfig Load()
    {
        if (cachedConfig != null)
        {
            return cachedConfig;
        }

        cachedConfig = Resources.Load<RedOniCafeVisitConfig>(ResourcePath);

        if (cachedConfig == null)
        {
            Debug.LogWarning($"RedOniCafeVisitConfig was not found at Resources/{ResourcePath}; using placeholder defaults.");
            cachedConfig = CreateInstance<RedOniCafeVisitConfig>();
            cachedConfig.hideFlags = HideFlags.DontSave;
        }

        return cachedConfig;
    }
}
