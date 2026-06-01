using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CafeOperationController : MonoBehaviour
{
    [Header("Optional guest icons for the operation UI")]
    [SerializeField] private Sprite worshipperIcon;
    [SerializeField] private Sprite travelerIcon;
    [SerializeField] private Sprite smallYokaiIcon;
    [SerializeField] private Sprite regularIcon;

    [Header("Optional menu icons for the operation UI")]
    [SerializeField] private Sprite inariCoffeeIcon;
    [SerializeField] private Sprite kitsunebiLatteIcon;
    [SerializeField] private Sprite yozakuraCakeIcon;

    private readonly List<CafeGuestState> guests = new List<CafeGuestState>();
    private readonly List<CafeMenuItem> menuItems = new List<CafeMenuItem>();

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

    private void Awake()
    {
        EnsureInitialData();
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

        inventory.AddFaithPoints(menuItem.FaithPointReward);
        guest.AddAffection(1);
        guest.SetLatestMessage(GetServeMessage(guestIndex));

        resultMessage = $"{guest.DisplayName} に {menuItem.DisplayName} を提供しました。";
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
        BusinessOpened?.Invoke();
        StateChanged?.Invoke();
        return true;
    }

    public string BuildGuestSeatSummary()
    {
        EnsureInitialData();

        StringBuilder summary = new StringBuilder("カウンター前の4席\n");

        for (int i = 0; i < guests.Count; i++)
        {
            CafeGuestState guest = guests[i];
            bool seatConnected = GameObject.Find(guest.SeatName) != null;
            summary.Append($"{guest.SeatName}: {guest.DisplayName}");
            summary.Append(seatConnected ? $"  好感度 {guest.Affection}" : "  (座席未接続)");

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

        for (int i = 0; i < guests.Count; i++)
        {
            CafeGuestState guest = guests[i];
            summary.Append($"{guest.DisplayName}: {guest.LatestMessage}");

            if (i < guests.Count - 1)
            {
                summary.Append("\n");
            }
        }

        return summary.ToString();
    }

    private void EnsureInitialData()
    {
        if (guests.Count == 0)
        {
            guests.Add(new CafeGuestState("GuestSeat_01", "参拝客", "稲荷コーヒー", worshipperIcon));
            guests.Add(new CafeGuestState("GuestSeat_02", "旅人", "夜桜ケーキ", travelerIcon));
            guests.Add(new CafeGuestState("GuestSeat_03", "小さな妖怪", "狐火ラテ", smallYokaiIcon));
            guests.Add(new CafeGuestState("GuestSeat_04", "不思議な常連", "夜桜ケーキ", regularIcon));
        }

        if (menuItems.Count == 0)
        {
            menuItems.Add(new CafeMenuItem("inari_coffee", "稲荷コーヒー", 2, inariCoffeeIcon));
            menuItems.Add(new CafeMenuItem("kitsunebi_latte", "狐火ラテ", 3, kitsunebiLatteIcon));
            menuItems.Add(new CafeMenuItem("yozakura_cake", "夜桜ケーキ", 3, yozakuraCakeIcon));
        }
    }

    private string GetServeMessage(int guestIndex)
    {
        switch (guestIndex)
        {
            case 0:
                return "ごちそうさまでした。神社が少し明るくなった気がします。";
            case 1:
                return "静かで落ち着く場所ですね。";
            case 2:
                return "狐火ラテ、あったかい……";
            case 3:
                return "また来ます。次は、もっと赤い料理を。";
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

[Serializable]
public class CafeGuestState
{
    [SerializeField] private string seatName;
    [SerializeField] private string displayName;
    [SerializeField] private int affection;
    [SerializeField] private string favoriteMenu;
    [SerializeField] private string latestMessage;
    [SerializeField] private Sprite icon;

    public string SeatName => seatName;
    public string DisplayName => displayName;
    public int Affection => affection;
    public string FavoriteMenu => favoriteMenu;
    public string LatestMessage => latestMessage;
    public Sprite Icon => icon;

    public CafeGuestState(string seatName, string displayName, string favoriteMenu, Sprite icon)
    {
        this.seatName = seatName;
        this.displayName = displayName;
        this.favoriteMenu = favoriteMenu;
        this.icon = icon;
        latestMessage = "まだメッセージはありません。";
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
}

[Serializable]
public class CafeMenuItem
{
    [SerializeField] private string menuId;
    [SerializeField] private string displayName;
    [SerializeField] private int faithPointReward;
    [SerializeField] private Sprite icon;

    public string MenuId => menuId;
    public string DisplayName => displayName;
    public int FaithPointReward => faithPointReward;
    public Sprite Icon => icon;

    public CafeMenuItem(string menuId, string displayName, int faithPointReward, Sprite icon)
    {
        this.menuId = menuId;
        this.displayName = displayName;
        this.faithPointReward = Mathf.Max(0, faithPointReward);
        this.icon = icon;
    }
}
