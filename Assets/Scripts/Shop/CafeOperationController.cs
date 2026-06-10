using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CafeOperationController : MonoBehaviour
{
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

    private readonly List<CafeGuestState> guests = new List<CafeGuestState>();
    private readonly List<CafeMenuItem> menuItems = new List<CafeMenuItem>();
    private readonly List<string> recentGuestMessages = new List<string>();

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
    public bool IsOpenForBusiness { get; private set; }

    public event Action StateChanged;
    public event Action BusinessOpened;
    public event Action<int> GuestServed;

    private void Awake()
    {
        EnsureInitialData();
        ResolveResourceInventory().EnsureCafeStarterIngredients();
    }

    public bool TryServe(int guestIndex, int menuIndex, out string resultMessage)
    {
        EnsureInitialData();

        if (!IsOpenForBusiness)
        {
            resultMessage = "先に開業してください。";
            return false;
        }

        if (guestIndex < 0 || guestIndex >= guests.Count)
        {
            resultMessage = "客席を選んでください。";
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

        if (!HasRequiredIngredients(inventory, menuItem))
        {
            resultMessage = $"食材が足りません: {BuildMissingIngredientSummary(inventory, menuItem)}";
            return false;
        }

        ConsumeIngredients(inventory, menuItem);
        inventory.AddFaithPoints(menuItem.FaithPointReward);
        guest.AddAffection(1);
        string serveMessage = GetServeMessage(guestIndex);
        guest.SetLatestMessage(serveMessage);
        guest.MarkServed();
        AddRecentGuestMessage(guest.DisplayName, serveMessage);

        resultMessage = $"{guest.DisplayName} に {menuItem.DisplayName} を提供しました。";
        StateChanged?.Invoke();
        GuestServed?.Invoke(guestIndex);
        return true;
    }

    public void MarkGuestLeaving(int guestIndex)
    {
        if (guestIndex < 0 || guestIndex >= guests.Count || !guests[guestIndex].IsServed)
        {
            return;
        }

        guests[guestIndex].MarkLeaving();
        StateChanged?.Invoke();
    }

    public void ClearGuestSeat(int guestIndex)
    {
        if (guestIndex < 0 || guestIndex >= guests.Count)
        {
            return;
        }

        guests[guestIndex].MarkEmpty();
        StateChanged?.Invoke();
    }

    public bool TryOpenForBusiness()
    {
        if (IsOpenForBusiness)
        {
            return false;
        }

        IsOpenForBusiness = true;
        AssignActiveGuestsForBusiness();
        AssignRandomRequests();
        BusinessOpened?.Invoke();
        StateChanged?.Invoke();
        return true;
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

    public string BuildGuestSeatSummary()
    {
        EnsureInitialData();

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
                summary.Append($"{guest.DisplayName}  {guest.ServiceStateLabel}");
                summary.Append(seatConnected ? $"  好感度 {guest.Affection}" : "  (座席未接続)");
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

        return summary.Length > 0 ? summary.ToString() : "まだメッセージはありません。";
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
        if (guests.Count == 0)
        {
            List<CafeGuestTemplate> guestCatalog = BuildGuestCatalog();

            for (int i = 0; i < ActiveSeatNames.Length && i < guestCatalog.Count; i++)
            {
                guests.Add(guestCatalog[i].CreateState(ActiveSeatNames[i]));
            }
        }

        if (menuItems.Count == 0)
        {
            menuItems.Add(new CafeMenuItem(
                "inari_coffee",
                "稲荷コーヒー",
                2,
                inariCoffeeIcon,
                new CafeIngredientRequirement(ResourceInventory.CoffeeBeanId, 1)));
            menuItems.Add(new CafeMenuItem(
                "kitsunebi_latte",
                "狐火ラテ",
                3,
                kitsunebiLatteIcon,
                new CafeIngredientRequirement(ResourceInventory.CoffeeBeanId, 1),
                new CafeIngredientRequirement(ResourceInventory.MilkId, 1)));
            menuItems.Add(new CafeMenuItem(
                "yozakura_cake",
                "夜桜ケーキ",
                3,
                yozakuraCakeIcon,
                new CafeIngredientRequirement(ResourceInventory.FlourId, 1),
                new CafeIngredientRequirement(ResourceInventory.SugarId, 1)));
        }

    }

    private void AssignRandomRequests()
    {
        for (int i = 0; i < guests.Count; i++)
        {
            guests[i].SetRequestedMenu(GetRandomMenuItem());
        }
    }

    private void AssignActiveGuestsForBusiness()
    {
        List<CafeGuestTemplate> guestPool = BuildGuestCatalog();
        List<CafeGuestTemplate> selectedGuests = new List<CafeGuestTemplate>();

        ShuffleGuestTemplates(guestPool);

        for (int i = 0; i < guestPool.Count && selectedGuests.Count < ActiveSeatNames.Length; i++)
        {
            selectedGuests.Add(guestPool[i]);
        }

        guests.Clear();

        for (int i = 0; i < selectedGuests.Count && i < ActiveSeatNames.Length; i++)
        {
            guests.Add(selectedGuests[i].CreateState(ActiveSeatNames[i]));
        }
    }

    private List<CafeGuestTemplate> BuildGuestCatalog()
    {
        return new List<CafeGuestTemplate>
        {
            new CafeGuestTemplate(
                "worshipper",
                "参拝客",
                "稲荷コーヒー",
                "ごちそうさまでした。神社が少し明るくなった気がします。",
                worshipperIcon),
            new CafeGuestTemplate(
                "traveler",
                "旅人",
                "夜桜ケーキ",
                "静かで落ち着く場所ですね。",
                travelerIcon),
            new CafeGuestTemplate(
                "small_yokai",
                "小さな妖怪",
                "狐火ラテ",
                "狐火ラテ、あったかい……",
                smallYokaiIcon),
            new CafeGuestTemplate(
                "priest_regular",
                "不思議な常連",
                "夜桜ケーキ",
                "また来ます。次は、もっと赤い料理を。",
                priestIcon),
            new CafeGuestTemplate(
                "student_girl_uniform",
                "制服の学生",
                "狐火ラテ",
                "部活の帰りに、こんな静かな場所があるなんて知りませんでした。",
                studentGirlUniformIcon),
            new CafeGuestTemplate(
                "tanuki_yokai",
                "たぬき妖怪",
                "稲荷コーヒー",
                "この香り、化ける前の眠気にも効きそうだぽん。",
                tanukiYokaiIcon),
            new CafeGuestTemplate(
                "girl_kimono",
                "着物の女の子",
                "夜桜ケーキ",
                "お花の香りがして、夜のお祭りみたいですね。",
                girlKimonoIcon),
            new CafeGuestTemplate(
                "child_girl_kimono",
                "小さな参拝客",
                "狐火ラテ",
                "あったかい飲み物、手までぽかぽかします。",
                childGirlKimonoIcon),
            new CafeGuestTemplate(
                "kappa_yokai",
                "河童の客",
                "狐火ラテ",
                "水辺の匂いがして、ここは落ち着くな。",
                kappaYokaiIcon),
            new CafeGuestTemplate(
                "middle_aged_office_worker",
                "仕事帰りの会社員",
                "稲荷コーヒー",
                "仕事帰りに、こういう静かな店があると助かります。",
                middleAgedOfficeWorkerIcon)
        };
    }

    private void ShuffleGuestTemplates(List<CafeGuestTemplate> guestTemplates)
    {
        for (int i = 0; i < guestTemplates.Count; i++)
        {
            int swapIndex = UnityEngine.Random.Range(i, guestTemplates.Count);
            CafeGuestTemplate temporary = guestTemplates[i];
            guestTemplates[i] = guestTemplates[swapIndex];
            guestTemplates[swapIndex] = temporary;
        }
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

    private string GetServeMessage(int guestIndex)
    {
        if (guestIndex < 0 || guestIndex >= guests.Count)
        {
            return string.Empty;
        }

        switch (guests[guestIndex].GuestId)
        {
            case "worshipper":
                return "ごちそうさまでした。神社が少し明るくなった気がします。";
            case "traveler":
                return "静かで落ち着く場所ですね。";
            case "small_yokai":
                return "狐火ラテ、あったかい……";
            case "priest_regular":
                return "また来ます。次は、もっと赤い料理を。";
            case "student_girl_uniform":
                return "部活の帰りに、こんな静かな場所があるなんて知りませんでした。";
            case "tanuki_yokai":
                return "この香り、化ける前の眠気にも効きそうだぽん。";
            case "girl_kimono":
                return "お花の香りがして、夜のお祭りみたいですね。";
            case "child_girl_kimono":
                return "あったかい飲み物、手までぽかぽかします。";
            case "kappa_yokai":
                return "水辺の匂いがして、ここは落ち着くな。";
            case "middle_aged_office_worker":
                return "仕事帰りに、こういう静かな店があると助かります。";
            default:
                return string.Empty;
        }
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
    public string GuestId { get; }
    private readonly string displayName;
    private readonly string favoriteMenu;
    private readonly string firstMessage;
    private readonly Sprite icon;

    public CafeGuestTemplate(
        string guestId,
        string displayName,
        string favoriteMenu,
        string firstMessage,
        Sprite icon)
    {
        GuestId = guestId;
        this.displayName = displayName;
        this.favoriteMenu = favoriteMenu;
        this.firstMessage = firstMessage;
        this.icon = icon;
    }

    public CafeGuestState CreateState(string seatName)
    {
        return new CafeGuestState(seatName, GuestId, displayName, favoriteMenu, firstMessage, icon);
    }
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
    [SerializeField] private string guestId;
    [SerializeField] private string displayName;
    [SerializeField] private int affection;
    [SerializeField] private string favoriteMenu;
    [SerializeField] private string latestMessage;
    [SerializeField] private string requestedMenuId;
    [SerializeField] private string requestedMenuDisplayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private CafeGuestServiceState serviceState = CafeGuestServiceState.WaitingOrder;

    public string SeatName => seatName;
    public string GuestId => guestId;
    public string DisplayName => displayName;
    public int Affection => affection;
    public string FavoriteMenu => favoriteMenu;
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
        string guestId,
        string displayName,
        string favoriteMenu,
        string firstMessage,
        Sprite icon)
    {
        this.seatName = seatName;
        this.guestId = guestId;
        this.displayName = displayName;
        this.favoriteMenu = favoriteMenu;
        this.icon = icon;
        latestMessage = firstMessage;
        serviceState = CafeGuestServiceState.WaitingOrder;
    }

    public CafeGuestState(string seatName, string displayName, string favoriteMenu, Sprite icon)
        : this(seatName, displayName, displayName, favoriteMenu, "まだメッセージはありません。", icon)
    {
    }

    public void AddAffection(int amount)
    {
        if (amount > 0)
        {
            affection += amount;
        }
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
