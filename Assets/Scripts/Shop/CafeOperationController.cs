using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CafeOperationController : MonoBehaviour
{
    private const string VisitorAffectionSaveKeyPrefix = "CafeVisitorAffection.";
    private static readonly string[] ActiveSeatNames =
    {
        "GuestSeat_01",
        "GuestSeat_02",
        "GuestSeat_03",
        "GuestSeat_04"
    };

    [Header("Optional guest icons for the operation UI")]
    [SerializeField] private Sprite worshipperIcon;
    [SerializeField] private Sprite travelerIcon;
    [SerializeField] private Sprite smallYokaiIcon;
    [SerializeField] private Sprite priestIcon;
    [SerializeField] private Sprite studentGirlUniformIcon;
    [SerializeField] private Sprite tanukiYokaiIcon;
    [SerializeField] private Sprite girlKimonoIcon;
    [SerializeField] private Sprite childGirlKimonoIcon;
    [SerializeField] private Sprite kappaYokaiIcon;
    [SerializeField] private Sprite middleAgedOfficeWorkerIcon;

    [Header("Optional menu icons for the operation UI")]
    [SerializeField] private Sprite inariCoffeeIcon;
    [SerializeField] private Sprite kitsunebiLatteIcon;
    [SerializeField] private Sprite yozakuraCakeIcon;

    [Header("Optional gratitude icon")]
    [SerializeField] private Sprite heartFoxIcon;

    [Header("Visitor pool")]
    [SerializeField] private bool specialVisitorsUnlocked;
    [SerializeField] private bool persistVisitorAffection = true;

    [Header("Cafe production")]
    [SerializeField] private float productionSeconds = 2f;

    private readonly List<CafeGuestState> guests = new List<CafeGuestState>();
    private readonly List<CafeMenuItem> menuItems = new List<CafeMenuItem>();
    private readonly List<string> recentGuestMessages = new List<string>();
    private readonly List<string> sessionUnlockedFurnitureIds = new List<string>();
    private readonly List<string> sessionUnlockedFurnitureDisplayNames = new List<string>();
    private bool warnedMissingHeartFoxIcon;
    private bool searchedHeartFoxIcon;
    private int activeProductionCount;

    public IReadOnlyList<CafeGuestState> Guests
    {
        get
        {
            EnsureInitialData();
            return guests;
        }
    }

    public IReadOnlyList<CafeMenuItem> MenuItems
    {
        get
        {
            EnsureInitialData();
            return menuItems;
        }
    }

    public int FaithPoints => ResolveResourceInventory().FaithPoints;
    public int HeartFoxCount => ResolveResourceInventory().HeartFoxCount;
    public int MaxVisitorSlots => ActiveSeatNames.Length;
    public Sprite HeartFoxIcon => ResolveHeartFoxIcon();
    public string LastVisitorMessage { get; private set; }
    public string LastCafeFeedbackMessage { get; private set; }
    public int CafeFeedbackVersion { get; private set; }
    public bool LastServeGrantedHeartFox { get; private set; }
    public bool IsOpenForBusiness { get; private set; }
    public bool IsProducing { get; private set; }
    public string CurrentProductionMenuName { get; private set; }
    public int CurrentFoxAltarLevel => CafeSceneController.GetStoredFoxAltarLevel();
    public float ProductionSpeedMultiplier => CafeSceneController.GetProductionSpeedMultiplier(CurrentFoxAltarLevel);
    public int ProductionOutputAmount => CafeSceneController.GetProductionOutputAmount(CurrentFoxAltarLevel);
    public float ProductionSeconds => Mathf.Max(0.2f, productionSeconds / ProductionSpeedMultiplier);
    public int SelectedGuestIndex { get; private set; } = -1;
    public int SessionServedVisitorCount { get; private set; }
    public int SessionGainedFaithPoints { get; private set; }
    public int SessionGainedHeartFox { get; private set; }
    public int SessionAffectionIncreaseCount { get; private set; }
    public IReadOnlyList<string> SessionUnlockedFurnitureIds => sessionUnlockedFurnitureIds;

    public event Action StateChanged;
    public event Action BusinessOpened;
    public event Action<int> GuestServed;
    public event Action<int> SelectedGuestChanged;

    public Sprite GetMenuIcon(string menuId)
    {
        EnsureInitialData();

        for (int i = 0; i < menuItems.Count; i++)
        {
            CafeMenuItem menuItem = menuItems[i];

            if (menuItem.MenuId == menuId)
            {
                return menuItem.Icon;
            }
        }

        return null;
    }

    private void Awake()
    {
        EnsureInitialData();
        ResolveResourceInventory().EnsureCafeStarterIngredients();
        RefreshVisitors();
    }

    public bool TryServe(int guestIndex, int menuIndex, out string resultMessage)
    {
        EnsureInitialData();
        LastVisitorMessage = string.Empty;
        LastServeGrantedHeartFox = false;

        if (!IsOpenForBusiness)
        {
            resultMessage = "先に開業してください。";
            return false;
        }

        if (guestIndex < 0 || guestIndex >= guests.Count)
        {
            resultMessage = "来訪者の席を選んでください。";
            return false;
        }

        if (menuIndex < 0 || menuIndex >= menuItems.Count)
        {
            resultMessage = "メニューを選んでください。";
            return false;
        }

        CafeGuestState guest = guests[guestIndex];
        CafeMenuItem menuItem = menuItems[menuIndex];
        ResourceInventory inventory = ResolveResourceInventory();

        if (!guest.IsOccupied)
        {
            resultMessage = "この席は空席です。";
            return false;
        }

        if (!guest.CanServe)
        {
            resultMessage = $"{guest.DisplayName} は今は提供待ちではありません。";
            return false;
        }

        if (string.IsNullOrEmpty(guest.RequestedMenuId))
        {
            guest.SetRequestedMenu(GetRandomMenuItem());
        }

        if (guest.RequestedMenuId != menuItem.MenuId)
        {
            resultMessage = $"{guest.DisplayName} の注文は {guest.RequestedMenuDisplayName} です。";
            return false;
        }

        string finishedItemId = GetFinishedItemIdForMenu(menuItem);

        if (string.IsNullOrEmpty(finishedItemId)
            || !inventory.HasFinishedItem(finishedItemId, 1))
        {
            resultMessage = $"{menuItem.DisplayName} の完成品がありません。\n先に制作してください。";
            return false;
        }

        inventory.SpendFinishedItem(finishedItemId, 1);
        inventory.AddFaithPoints(menuItem.FaithPointReward);
        bool servedLikedMenu = guest.LikesMenu(menuItem);
        bool gaveHeartFox = servedLikedMenu && guest.CanGiveHeartFox;

        SessionServedVisitorCount++;
        SessionGainedFaithPoints += menuItem.FaithPointReward;

        if (servedLikedMenu)
        {
            guest.AddAffection(1);
            SaveVisitorAffection(guest);
            SessionAffectionIncreaseCount++;
        }

        if (gaveHeartFox)
        {
            inventory.AddHeartFox(1);
            LastServeGrantedHeartFox = true;
            SessionGainedHeartFox++;
        }

        string serveMessage = guest.GetRandomMessage();
        LastVisitorMessage = serveMessage;
        guest.SetLatestMessage(serveMessage);
        guest.MarkServed();
        AddRecentGuestMessage(guest.DisplayName, serveMessage);

        resultMessage = $"{guest.DisplayName} に {menuItem.DisplayName} を提供しました。\n来訪者は少し安心したようです。";

        if (servedLikedMenu)
        {
            resultMessage += "\n気に入ってくれたようです。";
        }

        if (gaveHeartFox)
        {
            resultMessage += "\nこころ狐を受け取りました。";
        }

        if (!string.IsNullOrEmpty(serveMessage))
        {
            resultMessage += $"\n{guest.DisplayName}: {serveMessage}";
        }

        StateChanged?.Invoke();
        GuestServed?.Invoke(guestIndex);
        return true;
    }

    public bool TryStartProduction(int menuIndex, out CafeMenuItem menuItem, out string resultMessage)
    {
        EnsureInitialData();
        menuItem = null;

        if (menuIndex < 0 || menuIndex >= menuItems.Count)
        {
            resultMessage = "制作するメニューを選んでください。";
            return false;
        }

        menuItem = menuItems[menuIndex];
        ResourceInventory inventory = ResolveResourceInventory();

        if (!HasRequiredIngredients(inventory, menuItem))
        {
            string missingIngredients = BuildMissingIngredientSummary(inventory, menuItem);
            resultMessage = string.IsNullOrEmpty(missingIngredients)
                ? "材料が足りません。"
                : $"材料が足りません。\n{missingIngredients}";
            return false;
        }

        ConsumeIngredients(inventory, menuItem);
        activeProductionCount++;
        IsProducing = activeProductionCount > 0;
        CurrentProductionMenuName = activeProductionCount > 1
            ? "複数メニュー"
            : menuItem.DisplayName;
        resultMessage = $"{menuItem.DisplayName} を制作しています。";
        StateChanged?.Invoke();
        return true;
    }

    public void CompleteProduction(CafeMenuItem menuItem, out string resultMessage)
    {
        if (menuItem == null)
        {
            FinishProductionTracking();
            resultMessage = "制作を完了できませんでした。";
            StateChanged?.Invoke();
            return;
        }

        string finishedItemId = GetFinishedItemIdForMenu(menuItem);

        if (string.IsNullOrEmpty(finishedItemId))
        {
            FinishProductionTracking();
            resultMessage = "完成品の保存先が見つかりません。";
            StateChanged?.Invoke();
            return;
        }

        int outputAmount = ProductionOutputAmount;
        ResolveResourceInventory().AddFinishedItem(finishedItemId, outputAmount);
        FinishProductionTracking();
        resultMessage = outputAmount > 1
            ? $"{menuItem.DisplayName} が {outputAmount} 個完成しました。"
            : $"{menuItem.DisplayName} が完成しました。";
        StateChanged?.Invoke();
    }

    private void FinishProductionTracking()
    {
        activeProductionCount = Mathf.Max(0, activeProductionCount - 1);
        IsProducing = activeProductionCount > 0;
        CurrentProductionMenuName = IsProducing ? "複数メニュー" : string.Empty;
    }

    public void MarkGuestLeaving(int guestIndex)
    {
        if (guestIndex < 0 || guestIndex >= guests.Count || !guests[guestIndex].IsServed)
        {
            return;
        }

        guests[guestIndex].MarkLeaving();
        ClearSelectedGuestIfNeeded(guestIndex);
        StateChanged?.Invoke();
    }

    public void ClearGuestSeat(int guestIndex)
    {
        if (guestIndex < 0 || guestIndex >= guests.Count)
        {
            return;
        }

        guests[guestIndex].MarkEmpty();
        ClearSelectedGuestIfNeeded(guestIndex);
        StateChanged?.Invoke();
    }

    public void SetSelectedGuestIndex(int guestIndex)
    {
        int normalizedIndex = guestIndex;

        if (normalizedIndex < 0
            || normalizedIndex >= guests.Count
            || !guests[normalizedIndex].CanServe)
        {
            normalizedIndex = -1;
        }

        if (SelectedGuestIndex == normalizedIndex)
        {
            return;
        }

        SelectedGuestIndex = normalizedIndex;
        SelectedGuestChanged?.Invoke(SelectedGuestIndex);
    }

    public void SetCafeFeedbackMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        LastCafeFeedbackMessage = message;
        CafeFeedbackVersion++;
        StateChanged?.Invoke();
    }

    public void RecordSessionFurnitureUnlock(string unlockId, string displayName)
    {
        if (string.IsNullOrEmpty(unlockId) || sessionUnlockedFurnitureIds.Contains(unlockId))
        {
            return;
        }

        sessionUnlockedFurnitureIds.Add(unlockId);
        sessionUnlockedFurnitureDisplayNames.Add(string.IsNullOrEmpty(displayName) ? unlockId : displayName);
    }

    public string BuildCafeDayResultSummary()
    {
        return
            "今日も、少しだけ灯りが増えました。\n\n" +
            $"来訪者: {SessionServedVisitorCount}人\n" +
            $"信仰値: +{SessionGainedFaithPoints}\n" +
            $"こころ狐: +{SessionGainedHeartFox}\n" +
            $"好感度アップ: {SessionAffectionIncreaseCount}人\n" +
            $"解放された家具: {BuildSessionUnlockedFurnitureSummary()}";
    }

    public void ResetCafeSessionResults()
    {
        SessionServedVisitorCount = 0;
        SessionGainedFaithPoints = 0;
        SessionGainedHeartFox = 0;
        SessionAffectionIncreaseCount = 0;
        sessionUnlockedFurnitureIds.Clear();
        sessionUnlockedFurnitureDisplayNames.Clear();
    }

    private string BuildSessionUnlockedFurnitureSummary()
    {
        if (sessionUnlockedFurnitureDisplayNames.Count == 0)
        {
            return "新しい家具はありません";
        }

        return string.Join(" / ", sessionUnlockedFurnitureDisplayNames);
    }

    public bool TryRefillGuestSeat(int guestIndex)
    {
        EnsureInitialData();

        if (!IsOpenForBusiness || guestIndex < 0 || guestIndex >= guests.Count)
        {
            return false;
        }

        string previousVisitorId = guests[guestIndex] != null ? guests[guestIndex].VisitorId : string.Empty;
        List<CafeGuestTemplate> guestPool = BuildAvailableVisitorPool();
        RemoveCurrentVisitorsFromPool(guestPool, guestIndex);
        RemoveRecentVisitorFromPoolIfPossible(guestPool, previousVisitorId);

        if (guestPool.Count == 0)
        {
            return false;
        }

        CafeGuestTemplate selectedVisitor = SelectWeightedVisitor(guestPool);
        CafeGuestState visitorState = selectedVisitor.CreateState(ActiveSeatNames[guestIndex]);
        visitorState.SetAffection(LoadVisitorAffection(visitorState.VisitorId));
        visitorState.SetRequestedMenu(GetRandomMenuItem());
        guests[guestIndex] = visitorState;
        StateChanged?.Invoke();
        return true;
    }

    public bool TryOpenForBusiness()
    {
        if (IsOpenForBusiness)
        {
            return false;
        }

        IsOpenForBusiness = true;

        if (guests.Count == 0)
        {
            RefreshVisitors();
        }
        else
        {
            AssignRandomRequests();
        }

        BusinessOpened?.Invoke();
        StateChanged?.Invoke();
        return true;
    }

    public void RefreshVisitors(int maxVisitors = -1)
    {
        EnsureInitialData();

        List<CafeGuestTemplate> guestPool = BuildAvailableVisitorPool();
        guests.Clear();
        SetSelectedGuestIndex(-1);

        int visitorLimit = maxVisitors < 0
            ? ActiveSeatNames.Length
            : Mathf.Clamp(maxVisitors, 0, ActiveSeatNames.Length);

        while (guestPool.Count > 0 && guests.Count < visitorLimit)
        {
            CafeGuestTemplate selectedVisitor = SelectWeightedVisitor(guestPool);
            CafeGuestState visitorState = selectedVisitor.CreateState(ActiveSeatNames[guests.Count]);
            visitorState.SetAffection(LoadVisitorAffection(visitorState.VisitorId));
            guests.Add(visitorState);
            guestPool.Remove(selectedVisitor);
        }

        AssignRandomRequests();
        StateChanged?.Invoke();
    }

    private void ClearSelectedGuestIfNeeded(int guestIndex)
    {
        if (SelectedGuestIndex == guestIndex)
        {
            SetSelectedGuestIndex(-1);
        }
    }

    public string BuildIngredientRequirementSummary(CafeMenuItem menuItem)
    {
        if (menuItem == null)
        {
            return string.Empty;
        }

        StringBuilder summary = new StringBuilder();

        for (int i = 0; i < menuItem.Ingredients.Count; i++)
        {
            CafeIngredientRequirement requirement = menuItem.Ingredients[i];
            summary.Append($"{GetIngredientDisplayName(requirement.IngredientId)} x{requirement.Amount}");

            if (i < menuItem.Ingredients.Count - 1)
            {
                summary.Append(" / ");
            }
        }

        return summary.ToString();
    }

    public string BuildFinishedItemStorageSummary()
    {
        ResourceInventory inventory = ResolveResourceInventory();
        return
            $"完成品: 稲荷 {inventory.GetFinishedItemCount(ResourceInventory.InariCoffeeId)} / " +
            $"狐火 {inventory.GetFinishedItemCount(ResourceInventory.KitsunebiLatteId)} / " +
            $"夜桜 {inventory.GetFinishedItemCount(ResourceInventory.YozakuraCakeId)}";
    }

    public string BuildProductionBonusSummary()
    {
        return $"供台Lv.{CurrentFoxAltarLevel} 制作 x{ProductionSpeedMultiplier:0.00} / 完成 x{ProductionOutputAmount}";
    }

    public string GetFinishedItemIdForMenu(CafeMenuItem menuItem)
    {
        if (menuItem == null)
        {
            return string.Empty;
        }

        switch (menuItem.MenuId)
        {
            case "inari_coffee":
                return ResourceInventory.InariCoffeeId;
            case "kitsunebi_latte":
                return ResourceInventory.KitsunebiLatteId;
            case "yozakura_cake":
                return ResourceInventory.YozakuraCakeId;
            default:
                return string.Empty;
        }
    }

    public int GetFinishedItemCountForMenu(CafeMenuItem menuItem)
    {
        string finishedItemId = GetFinishedItemIdForMenu(menuItem);
        return string.IsNullOrEmpty(finishedItemId)
            ? 0
            : ResolveResourceInventory().GetFinishedItemCount(finishedItemId);
    }

    public string BuildGuestSeatSummary()
    {
        EnsureInitialData();

        if (guests.Count == 0)
        {
            return "今は来訪者がいません。";
        }

        StringBuilder summary = new StringBuilder("カウンター前の4席\n");

        for (int i = 0; i < guests.Count; i++)
        {
            CafeGuestState guest = guests[i];
            bool seatConnected = GameObject.Find(guest.SeatName) != null;
            summary.Append($"{guest.SeatName}: ");

            if (!guest.IsOccupied)
            {
                summary.Append("空席");
            }
            else
            {
                summary.Append($"{guest.DisplayName} [{guest.VisitorTypeLabel}]  {guest.ServiceStateLabel}");
                summary.Append(seatConnected ? $"  好感度 {guest.Affection}" : "  (座席未接続)");
                summary.Append($"  好き: {guest.FavoriteMenuSummary}");
            }

            if (i < guests.Count - 1)
            {
                summary.Append("\n");
            }
        }

        return summary.ToString();
    }

    public string BuildMenuSummary()
    {
        EnsureInitialData();

        StringBuilder summary = new StringBuilder();

        for (int i = 0; i < menuItems.Count; i++)
        {
            CafeMenuItem menuItem = menuItems[i];
            summary.Append($"{menuItem.DisplayName}: +{menuItem.FaithPointReward} 信仰値");

            if (i < menuItems.Count - 1)
            {
                summary.Append("\n");
            }
        }

        return summary.ToString();
    }

    public string BuildMessageBoardSummary()
    {
        EnsureInitialData();

        StringBuilder summary = new StringBuilder();

        if (recentGuestMessages.Count > 0)
        {
            for (int i = 0; i < recentGuestMessages.Count; i++)
            {
                summary.Append(recentGuestMessages[i]);

                if (i < recentGuestMessages.Count - 1)
                {
                    summary.Append("\n");
                }
            }

            return summary.ToString();
        }

        for (int i = 0; i < guests.Count; i++)
        {
            CafeGuestState guest = guests[i];

            if (!guest.IsOccupied)
            {
                continue;
            }

            if (summary.Length > 0)
            {
                summary.Append("\n");
            }

            summary.Append($"{guest.DisplayName}: {guest.LatestMessage}");
        }

        return summary.Length > 0 ? summary.ToString() : "今は来訪者がいません。";
    }

    private void AddRecentGuestMessage(string guestName, string message)
    {
        if (string.IsNullOrEmpty(guestName) || string.IsNullOrEmpty(message))
        {
            return;
        }

        recentGuestMessages.Insert(0, $"{guestName}: {message}");

        while (recentGuestMessages.Count > 8)
        {
            recentGuestMessages.RemoveAt(recentGuestMessages.Count - 1);
        }
    }

    private void EnsureInitialData()
    {
        if (menuItems.Count == 0)
        {
            menuItems.Add(new CafeMenuItem(
                "inari_coffee",
                "稲荷コーヒー",
                2,
                ResolveMenuIcon("inari_coffee", inariCoffeeIcon),
                new CafeIngredientRequirement(ResourceInventory.CoffeeBeanId, 1)));
            menuItems.Add(new CafeMenuItem(
                "kitsunebi_latte",
                "狐火ラテ",
                3,
                ResolveMenuIcon("kitsunebi_latte", kitsunebiLatteIcon),
                new CafeIngredientRequirement(ResourceInventory.CoffeeBeanId, 1),
                new CafeIngredientRequirement(ResourceInventory.MilkId, 1)));
            menuItems.Add(new CafeMenuItem(
                "yozakura_cake",
                "夜桜ケーキ",
                3,
                ResolveMenuIcon("yozakura_cake", yozakuraCakeIcon),
                new CafeIngredientRequirement(ResourceInventory.FlourId, 1),
                new CafeIngredientRequirement(ResourceInventory.SugarId, 1)));
        }
    }

    private Sprite ResolveMenuIcon(string menuId, Sprite assignedIcon)
    {
        if (assignedIcon != null)
        {
            return assignedIcon;
        }

#if UNITY_EDITOR
        string assetPath = GetMenuIconAssetPath(menuId);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

        if (texture != null)
        {
            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = $"{menuId}_RuntimeMenuSprite";
            return sprite;
        }
#endif

        return null;
    }

    private string GetMenuIconAssetPath(string menuId)
    {
        switch (menuId)
        {
            case "inari_coffee":
                return "Assets/Art/cafe_icon/menu_runtime/inari_coffee.png";
            case "kitsunebi_latte":
                return "Assets/Art/cafe_icon/menu_runtime/kitsunebi_latte.png";
            case "yozakura_cake":
                return "Assets/Art/cafe_icon/menu_runtime/yozakura_cake.png";
            default:
                return string.Empty;
        }
    }

    private void AssignRandomRequests()
    {
        for (int i = 0; i < guests.Count; i++)
        {
            guests[i].SetRequestedMenu(GetRandomMenuItem());
        }
    }

    private List<CafeGuestTemplate> BuildAvailableVisitorPool()
    {
        List<CafeGuestTemplate> allVisitors = BuildGuestCatalog();
        List<CafeGuestTemplate> availableVisitors = new List<CafeGuestTemplate>();

        for (int i = 0; i < allVisitors.Count; i++)
        {
            CafeGuestTemplate visitor = allVisitors[i];

            if (visitor.VisitorType == CafeVisitorType.Special && !specialVisitorsUnlocked)
            {
                continue;
            }

            if (!visitor.CanAppearInRandomPool)
            {
                continue;
            }

            availableVisitors.Add(visitor);
        }

        return availableVisitors;
    }

    private CafeGuestTemplate SelectWeightedVisitor(List<CafeGuestTemplate> visitorPool)
    {
        int totalWeight = 0;

        for (int i = 0; i < visitorPool.Count; i++)
        {
            totalWeight += Mathf.Max(1, visitorPool[i].Weight);
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);

        for (int i = 0; i < visitorPool.Count; i++)
        {
            roll -= Mathf.Max(1, visitorPool[i].Weight);

            if (roll < 0)
            {
                return visitorPool[i];
            }
        }

        return visitorPool[visitorPool.Count - 1];
    }

    private void RemoveCurrentVisitorsFromPool(List<CafeGuestTemplate> visitorPool, int refillSeatIndex)
    {
        for (int i = visitorPool.Count - 1; i >= 0; i--)
        {
            string candidateId = visitorPool[i].VisitorId;

            for (int guestIndex = 0; guestIndex < guests.Count; guestIndex++)
            {
                if (guestIndex == refillSeatIndex)
                {
                    continue;
                }

                CafeGuestState guest = guests[guestIndex];

                if (guest != null && guest.IsOccupied && guest.VisitorId == candidateId)
                {
                    visitorPool.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private void RemoveRecentVisitorFromPoolIfPossible(List<CafeGuestTemplate> visitorPool, string recentVisitorId)
    {
        if (string.IsNullOrEmpty(recentVisitorId) || visitorPool.Count <= 1)
        {
            return;
        }

        for (int i = visitorPool.Count - 1; i >= 0; i--)
        {
            if (visitorPool[i].VisitorId == recentVisitorId)
            {
                visitorPool.RemoveAt(i);
                return;
            }
        }
    }

    private int LoadVisitorAffection(string visitorId)
    {
        if (!persistVisitorAffection || string.IsNullOrEmpty(visitorId))
        {
            return 0;
        }

        return Mathf.Max(0, PlayerPrefs.GetInt(GetVisitorAffectionSaveKey(visitorId), 0));
    }

    private void SaveVisitorAffection(CafeGuestState guest)
    {
        if (!persistVisitorAffection || guest == null || string.IsNullOrEmpty(guest.VisitorId))
        {
            return;
        }

        PlayerPrefs.SetInt(GetVisitorAffectionSaveKey(guest.VisitorId), Mathf.Max(0, guest.Affection));
        PlayerPrefs.Save();
    }

    private static string GetVisitorAffectionSaveKey(string visitorId)
    {
        return VisitorAffectionSaveKeyPrefix + visitorId;
    }

    private List<CafeGuestTemplate> BuildGuestCatalog()
    {
        return new List<CafeGuestTemplate>
        {
            new CafeGuestTemplate(
                "elder_woman_worshipper",
                "年配の参拝者",
                CafeVisitorType.Living,
                new[] { "yozakura_cake" },
                new[]
                {
                    "今日も、あの人に少し近づけた気がします。",
                    "花の甘さで、懐かしい声を思い出しました。"
                },
                30,
                true,
                true,
                worshipperIcon,
                "worshipper"),
            new CafeGuestTemplate(
                "foreign_backpacker",
                "異国の旅人",
                CafeVisitorType.Living,
                new[] { "inari_coffee" },
                new[]
                {
                    "言葉は分からなくても、温かさは分かります。",
                    "迷っていた夜道が、少しだけ明るく見えます。"
                },
                26,
                true,
                true,
                travelerIcon,
                "traveler"),
            new CafeGuestTemplate(
                "nekomata_orange_cat",
                "橙の猫又",
                CafeVisitorType.Yokai,
                new[] { "kitsunebi_latte" },
                new[]
                {
                    "この匂い、昔の家を思い出すにゃ。",
                    "湯気の向こうが、少しだけ家みたいだにゃ。"
                },
                24,
                true,
                true,
                smallYokaiIcon,
                "small_yokai"),
            new CafeGuestTemplate(
                "small_ghost",
                "小さな幽霊",
                CafeVisitorType.Spirit,
                new[] { "kitsunebi_latte" },
                new[]
                {
                    "ここは、少しだけ息がしやすいですね。",
                    "こわくない明かりって、あるんですね。"
                },
                22,
                true,
                true,
                childGirlKimonoIcon,
                "child_girl_kimono"),
            new CafeGuestTemplate(
                "student_girl_uniform",
                "制服の学生",
                CafeVisitorType.Living,
                new[] { "kitsunebi_latte" },
                new[]
                {
                    "夜の帰り道が、少し怖くなくなりました。",
                    "ここに来ると、深呼吸を思い出します。"
                },
                14,
                true,
                true,
                studentGirlUniformIcon,
                "student_girl_uniform"),
            new CafeGuestTemplate(
                "tanuki_yokai",
                "たぬき妖怪",
                CafeVisitorType.Yokai,
                new[] { "inari_coffee" },
                new[]
                {
                    "湯気の向こうで、尻尾までほっとしたぽん。",
                    "化けるのを忘れるくらい、落ち着くぽん。"
                },
                18,
                true,
                true,
                tanukiYokaiIcon,
                "tanuki_yokai"),
            new CafeGuestTemplate(
                "girl_kimono",
                "着物の女の子",
                CafeVisitorType.Living,
                new[] { "yozakura_cake" },
                new[]
                {
                    "花の香りで、迷子じゃない気がしました。",
                    "夜のお祭りが、少しだけ戻ってきたみたいです。"
                },
                12,
                true,
                true,
                girlKimonoIcon,
                "girl_kimono"),
            new CafeGuestTemplate(
                "child_girl_kimono",
                "小さな参拝客",
                CafeVisitorType.Living,
                new[] { "kitsunebi_latte" },
                new[]
                {
                    "ここにいると、手をつないでもらったみたいです。",
                    "あたたかい灯りは、迷子にやさしいですね。"
                },
                12,
                true,
                true,
                childGirlKimonoIcon,
                "child_girl_kimono"),
            new CafeGuestTemplate(
                "kappa_yokai",
                "河童の来訪者",
                CafeVisitorType.Yokai,
                new[] { "yozakura_cake" },
                new[]
                {
                    "甘いものは、水辺の月みたいで不思議だな。",
                    "皿の水まで、なんだか穏やかです。"
                },
                16,
                true,
                true,
                kappaYokaiIcon,
                "kappa_yokai"),
            new CafeGuestTemplate(
                "middle_aged_office_worker",
                "仕事帰りの会社員",
                CafeVisitorType.Living,
                new[] { "inari_coffee" },
                new[]
                {
                    "今日の疲れを、ここに少し置いていけそうです。",
                    "帰る前に、少しだけ自分に戻れました。"
                },
                10,
                true,
                true,
                middleAgedOfficeWorkerIcon,
                "middle_aged_office_worker"),
            new CafeGuestTemplate(
                "black_priest",
                "黒衣の司祭",
                CafeVisitorType.Special,
                new[] { "赤鬼の膳" },
                new[]
                {
                    "まだ、その時ではありません。"
                },
                1,
                false,
                true,
                priestIcon,
                "priest_regular")
        };
    }

    private CafeMenuItem GetRandomMenuItem()
    {
        EnsureInitialData();
        return menuItems.Count > 0 ? menuItems[UnityEngine.Random.Range(0, menuItems.Count)] : null;
    }

    private bool HasRequiredIngredients(ResourceInventory inventory, CafeMenuItem menuItem)
    {
        for (int i = 0; i < menuItem.Ingredients.Count; i++)
        {
            CafeIngredientRequirement requirement = menuItem.Ingredients[i];

            if (!inventory.HasIngredient(requirement.IngredientId, requirement.Amount))
            {
                return false;
            }
        }

        return true;
    }

    private void ConsumeIngredients(ResourceInventory inventory, CafeMenuItem menuItem)
    {
        for (int i = 0; i < menuItem.Ingredients.Count; i++)
        {
            CafeIngredientRequirement requirement = menuItem.Ingredients[i];
            inventory.SpendIngredient(requirement.IngredientId, requirement.Amount);
        }
    }

    private string BuildMissingIngredientSummary(ResourceInventory inventory, CafeMenuItem menuItem)
    {
        StringBuilder summary = new StringBuilder();

        for (int i = 0; i < menuItem.Ingredients.Count; i++)
        {
            CafeIngredientRequirement requirement = menuItem.Ingredients[i];
            int currentAmount = inventory.GetIngredientCount(requirement.IngredientId);

            if (currentAmount >= requirement.Amount)
            {
                continue;
            }

            if (summary.Length > 0)
            {
                summary.Append(" / ");
            }

            summary.Append($"{GetIngredientDisplayName(requirement.IngredientId)} {currentAmount}/{requirement.Amount}");
        }

        return summary.ToString();
    }

    private string GetIngredientDisplayName(string ingredientId)
    {
        switch (ingredientId)
        {
            case ResourceInventory.CoffeeBeanId:
                return "コーヒー豆";
            case ResourceInventory.MilkId:
                return "ミルク";
            case ResourceInventory.SugarId:
                return "砂糖";
            case ResourceInventory.FlourId:
                return "小麦粉";
            default:
                return ingredientId;
        }
    }

    public void WarnMissingHeartFoxIconOnce()
    {
        if (warnedMissingHeartFoxIcon)
        {
            return;
        }

        warnedMissingHeartFoxIcon = true;
        Debug.LogWarning("HeartFox icon is missing. Assign item_heart_fox_icon.png when the sprite is ready; using a text placeholder for now.");
    }

    private Sprite ResolveHeartFoxIcon()
    {
        if (heartFoxIcon != null)
        {
            return heartFoxIcon;
        }

        if (searchedHeartFoxIcon)
        {
            return null;
        }

        searchedHeartFoxIcon = true;

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("item_heart_fox_icon t:Sprite");

        if (guids.Length > 0)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            heartFoxIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
#endif

        return heartFoxIcon;
    }

    private ResourceInventory ResolveResourceInventory()
    {
        ResourceInventory inventory = ResourceInventory.Instance;

        if (inventory == null)
        {
            inventory = FindAnyObjectByType<ResourceInventory>();
        }

        if (inventory == null)
        {
            GameObject inventoryObject = new GameObject("ResourceInventory");
            inventory = inventoryObject.AddComponent<ResourceInventory>();
        }

        return inventory;
    }
}

public class CafeGuestTemplate
{
    public string VisitorId { get; }
    public CafeVisitorType VisitorType { get; }
    public int Weight { get; }
    public bool CanGiveHeartFox { get; }
    public bool CanAppearInRandomPool { get; }

    private readonly string displayName;
    private readonly string visualId;
    private readonly string[] favoriteMenus;
    private readonly string[] messageList;
    private readonly Sprite icon;

    public CafeGuestTemplate(
        string visitorId,
        string displayName,
        CafeVisitorType visitorType,
        string[] favoriteMenus,
        string[] messageList,
        int weight,
        bool canGiveHeartFox,
        bool canAppearInRandomPool,
        Sprite icon,
        string visualId)
    {
        VisitorId = visitorId;
        VisitorType = visitorType;
        this.displayName = displayName;
        this.favoriteMenus = favoriteMenus ?? Array.Empty<string>();
        this.messageList = messageList ?? Array.Empty<string>();
        Weight = Mathf.Max(1, weight);
        CanGiveHeartFox = canGiveHeartFox;
        CanAppearInRandomPool = canAppearInRandomPool;
        this.icon = icon;
        this.visualId = string.IsNullOrEmpty(visualId) ? visitorId : visualId;
    }

    public CafeGuestState CreateState(string seatName)
    {
        return new CafeGuestState(
            seatName,
            VisitorId,
            visualId,
            displayName,
            VisitorType,
            favoriteMenus,
            messageList,
            Weight,
            CanGiveHeartFox,
            icon);
    }
}

[Serializable]
public enum CafeVisitorType
{
    Living,
    Spirit,
    Yokai,
    Special
}

[Serializable]
public enum CafeGuestServiceState
{
    WaitingOrder,
    Served,
    Leaving,
    Empty
}

[Serializable]
public class CafeGuestState
{
    [SerializeField] private string seatName;
    [SerializeField] private string visitorId;
    [SerializeField] private string visualId;
    [SerializeField] private string displayName;
    [SerializeField] private CafeVisitorType visitorType;
    [SerializeField] private int affection;
    [SerializeField] private List<string> favoriteMenus = new List<string>();
    [SerializeField] private List<string> messageList = new List<string>();
    [SerializeField] private int weight = 1;
    [SerializeField] private bool canGiveHeartFox = true;
    [SerializeField] private string latestMessage;
    [SerializeField] private string requestedMenuId;
    [SerializeField] private string requestedMenuDisplayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private CafeGuestServiceState serviceState = CafeGuestServiceState.WaitingOrder;

    public string SeatName => seatName;
    public string GuestId => visitorId;
    public string VisitorId => visitorId;
    public string VisualId => visualId;
    public string DisplayName => displayName;
    public CafeVisitorType VisitorType => visitorType;
    public string VisitorTypeLabel => GetVisitorTypeLabel(visitorType);
    public int Affection => affection;
    public string FavoriteMenu => favoriteMenus.Count > 0 ? favoriteMenus[0] : string.Empty;
    public IReadOnlyList<string> FavoriteMenus => favoriteMenus;
    public string FavoriteMenuSummary => BuildFavoriteMenuSummary();
    public int Weight => weight;
    public bool CanGiveHeartFox => canGiveHeartFox;
    public string LatestMessage => latestMessage;
    public string RequestedMenuId => requestedMenuId;
    public string RequestedMenuDisplayName => requestedMenuDisplayName;
    public Sprite Icon => icon;
    public CafeGuestServiceState ServiceState => serviceState;
    public bool IsOccupied => serviceState != CafeGuestServiceState.Empty;
    public bool CanServe => serviceState == CafeGuestServiceState.WaitingOrder;
    public bool IsServed => serviceState == CafeGuestServiceState.Served;
    public bool IsLeaving => serviceState == CafeGuestServiceState.Leaving;
    public string ServiceStateLabel
    {
        get
        {
            switch (serviceState)
            {
                case CafeGuestServiceState.WaitingOrder:
                    return "注文待ち";
                case CafeGuestServiceState.Served:
                    return "留言中";
                case CafeGuestServiceState.Leaving:
                    return "帰り支度中";
                case CafeGuestServiceState.Empty:
                    return "空席";
                default:
                    return string.Empty;
            }
        }
    }

    public CafeGuestState(
        string seatName,
        string visitorId,
        string visualId,
        string displayName,
        CafeVisitorType visitorType,
        string[] favoriteMenus,
        string[] messageList,
        int weight,
        bool canGiveHeartFox,
        Sprite icon)
    {
        this.seatName = seatName;
        this.visitorId = visitorId;
        this.visualId = string.IsNullOrEmpty(visualId) ? visitorId : visualId;
        this.displayName = displayName;
        this.visitorType = visitorType;
        this.icon = icon;
        this.weight = Mathf.Max(1, weight);
        this.canGiveHeartFox = canGiveHeartFox;
        this.favoriteMenus = new List<string>(favoriteMenus ?? Array.Empty<string>());
        this.messageList = new List<string>(messageList ?? Array.Empty<string>());
        latestMessage = this.messageList.Count > 0 ? this.messageList[0] : "……ありがとう。";
        serviceState = CafeGuestServiceState.WaitingOrder;
    }

    public CafeGuestState(string seatName, string displayName, string favoriteMenu, Sprite icon)
        : this(
            seatName,
            displayName,
            displayName,
            displayName,
            CafeVisitorType.Living,
            new[] { favoriteMenu },
            new[] { "……ありがとう。" },
            1,
            true,
            icon)
    {
    }

    public void AddAffection(int amount)
    {
        if (amount > 0)
        {
            affection += amount;
        }
    }

    public void SetAffection(int amount)
    {
        affection = Mathf.Max(0, amount);
    }

    public bool LikesMenu(CafeMenuItem menuItem)
    {
        if (menuItem == null)
        {
            return false;
        }

        for (int i = 0; i < favoriteMenus.Count; i++)
        {
            string favoriteMenu = favoriteMenus[i];

            if (string.Equals(favoriteMenu, menuItem.DisplayName, StringComparison.Ordinal)
                || string.Equals(favoriteMenu, menuItem.MenuId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public string GetRandomMessage()
    {
        if (messageList.Count == 0)
        {
            return string.IsNullOrEmpty(latestMessage) ? "……ありがとう。" : latestMessage;
        }

        return messageList[UnityEngine.Random.Range(0, messageList.Count)];
    }

    public void SetLatestMessage(string message)
    {
        latestMessage = message ?? string.Empty;
    }

    public void SetRequestedMenu(CafeMenuItem menuItem)
    {
        if (!IsOccupied)
        {
            requestedMenuId = string.Empty;
            requestedMenuDisplayName = string.Empty;
            return;
        }

        requestedMenuId = menuItem != null ? menuItem.MenuId : string.Empty;
        requestedMenuDisplayName = menuItem != null ? menuItem.DisplayName : "未定";
    }

    public void MarkServed()
    {
        if (serviceState == CafeGuestServiceState.WaitingOrder)
        {
            serviceState = CafeGuestServiceState.Served;
        }
    }

    public void MarkLeaving()
    {
        if (serviceState == CafeGuestServiceState.Served)
        {
            serviceState = CafeGuestServiceState.Leaving;
        }
    }

    public void MarkEmpty()
    {
        serviceState = CafeGuestServiceState.Empty;
        requestedMenuId = string.Empty;
        requestedMenuDisplayName = string.Empty;
        icon = null;
    }

    private string GetVisitorTypeLabel(CafeVisitorType type)
    {
        switch (type)
        {
            case CafeVisitorType.Living:
                return "Living";
            case CafeVisitorType.Spirit:
                return "Spirit";
            case CafeVisitorType.Yokai:
                return "Yokai";
            case CafeVisitorType.Special:
                return "Special";
            default:
                return string.Empty;
        }
    }

    private string BuildFavoriteMenuSummary()
    {
        if (favoriteMenus.Count == 0)
        {
            return "未設定";
        }

        StringBuilder summary = new StringBuilder();

        for (int i = 0; i < favoriteMenus.Count; i++)
        {
            if (i > 0)
            {
                summary.Append(" / ");
            }

            summary.Append(GetMenuDisplayName(favoriteMenus[i]));
        }

        return summary.ToString();
    }

    private string GetMenuDisplayName(string menuIdOrName)
    {
        switch (menuIdOrName)
        {
            case "inari_coffee":
                return "稲荷コーヒー";
            case "kitsunebi_latte":
                return "狐火ラテ";
            case "yozakura_cake":
                return "夜桜ケーキ";
            default:
                return string.IsNullOrEmpty(menuIdOrName) ? "未設定" : menuIdOrName;
        }
    }
}

[Serializable]
public class CafeMenuItem
{
    [SerializeField] private string menuId;
    [SerializeField] private string displayName;
    [SerializeField] private int faithPointReward;
    [SerializeField] private Sprite icon;
    [SerializeField] private List<CafeIngredientRequirement> ingredients = new List<CafeIngredientRequirement>();

    public string MenuId => menuId;
    public string DisplayName => displayName;
    public int FaithPointReward => faithPointReward;
    public Sprite Icon => icon;
    public IReadOnlyList<CafeIngredientRequirement> Ingredients => ingredients;

    public CafeMenuItem(
        string menuId,
        string displayName,
        int faithPointReward,
        Sprite icon,
        params CafeIngredientRequirement[] ingredients)
    {
        this.menuId = menuId;
        this.displayName = displayName;
        this.faithPointReward = Mathf.Max(0, faithPointReward);
        this.icon = icon;
        this.ingredients.AddRange(ingredients);
    }
}

[Serializable]
public class CafeIngredientRequirement
{
    [SerializeField] private string ingredientId;
    [SerializeField] private int amount;

    public string IngredientId => ingredientId;
    public int Amount => amount;

    public CafeIngredientRequirement(string ingredientId, int amount)
    {
        this.ingredientId = ingredientId;
        this.amount = Mathf.Max(0, amount);
    }
}
