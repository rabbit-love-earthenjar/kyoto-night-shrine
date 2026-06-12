using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CafeOperationPanelController : MonoBehaviour
{
    private readonly List<Button> guestButtons = new List<Button>();
    private readonly List<Image> guestButtonIcons = new List<Image>();
    private readonly List<Text> guestButtonTexts = new List<Text>();
    private readonly List<Button> menuButtons = new List<Button>();
    private readonly List<Text> menuButtonTexts = new List<Text>();

    private CafeOperationController operationController;
    private GameObject panelObject;
    private GameObject messageBoardObject;
    private GameObject heartFoxFeedbackObject;
    private Image heartFoxFeedbackIcon;
    private Text heartFoxFeedbackPlaceholder;
    private Text faithPointText;
    private Text statusText;
    private Text messageBoardText;
    private Button openBusinessButton;
    private Text openBusinessButtonText;
    private Button serveButton;
    private int selectedGuestIndex = -1;
    private int selectedMenuIndex = -1;
    private string shownCafeFeedbackMessage;
    private int shownCafeFeedbackVersion = -1;
    private Coroutine heartFoxFeedbackCoroutine;

    public void Initialize(Transform canvasRoot, CafeOperationController controller)
    {
        operationController = controller;

        if (panelObject == null)
        {
            CreatePanel(canvasRoot);
        }
    }

    public void Show()
    {
        if (panelObject == null || operationController == null)
        {
            return;
        }

        panelObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (panelObject != null)
        {
            HideHeartFoxFeedback();

            if (messageBoardObject != null)
            {
                messageBoardObject.SetActive(false);
            }

            if (operationController != null)
            {
                operationController.SetSelectedGuestIndex(-1);
            }

            panelObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (operationController != null)
        {
            operationController.StateChanged -= Refresh;
        }
    }

    private void CreatePanel(Transform canvasRoot)
    {
        panelObject = CreatePanelObject("CafeOperationPanel", canvasRoot, new Vector2(900f, 560f));

        CreateText("夜神社カフェ 営業", panelObject.transform, new Vector2(0f, 234f), new Vector2(600f, 44f), 30);
        faithPointText = CreateText(string.Empty, panelObject.transform, new Vector2(310f, 234f), new Vector2(220f, 38f), 20);

        CreateText("今日の来訪者", panelObject.transform, new Vector2(-275f, 184f), new Vector2(260f, 34f), 22);
        CreateText("メニュー", panelObject.transform, new Vector2(190f, 184f), new Vector2(300f, 34f), 22);

        for (int i = 0; i < operationController.MaxVisitorSlots; i++)
        {
            int guestIndex = i;
            Button button = CreateButton(
                $"GuestButton_{i + 1:00}",
                panelObject.transform,
                new Vector2(-275f, 122f - i * 76f),
                new Vector2(330f, 70f),
                () => SelectGuest(guestIndex));

            Image guestIcon = CreateIcon("GuestIcon", null, button.transform, new Vector2(-135f, 0f), new Vector2(48f, 48f));
            guestIcon.enabled = false;

            Text buttonText = CreateText(string.Empty, button.transform, new Vector2(24f, 0f), new Vector2(250f, 66f), 13);
            guestButtons.Add(button);
            guestButtonIcons.Add(guestIcon);
            guestButtonTexts.Add(buttonText);
        }

        for (int i = 0; i < operationController.MenuItems.Count; i++)
        {
            int menuIndex = i;
            Button button = CreateButton(
                $"MenuButton_{i + 1:00}",
                panelObject.transform,
                new Vector2(190f, 122f - i * 76f),
                new Vector2(330f, 64f),
                () => SelectMenu(menuIndex));

            CafeMenuItem menuItem = operationController.MenuItems[i];

            if (menuItem.Icon != null)
            {
                CreateIcon("MenuIcon", menuItem.Icon, button.transform, new Vector2(-135f, 0f), new Vector2(48f, 48f));
            }

            Text buttonText = CreateText(string.Empty, button.transform, new Vector2(24f, 0f), new Vector2(250f, 60f), 15);
            menuButtons.Add(button);
            menuButtonTexts.Add(buttonText);
        }

        statusText = CreateText(
            "開業すると、来訪者が入店します。",
            panelObject.transform,
            new Vector2(0f, -166f),
            new Vector2(760f, 70f),
            17);

        openBusinessButton = CreateButtonWithLabel("OpenBusinessButton", "開業", panelObject.transform, new Vector2(-330f, -230f), new Vector2(130f, 44f), OpenForBusiness, out openBusinessButtonText);
        serveButton = CreateButtonWithLabel("ServeButton", "Serve", panelObject.transform, new Vector2(-180f, -230f), new Vector2(130f, 44f), Serve);
        CreateButtonWithLabel("MessageBoardButton", "メッセージ", panelObject.transform, new Vector2(70f, -230f), new Vector2(180f, 44f), ToggleMessageBoard);
        CreateButtonWithLabel("CloseButton", "Close", panelObject.transform, new Vector2(280f, -230f), new Vector2(150f, 44f), Hide);

        CreateMessageBoard(panelObject.transform);
        CreateHeartFoxFeedback(panelObject.transform);
        operationController.StateChanged += Refresh;
        panelObject.SetActive(false);
    }

    private void CreateMessageBoard(Transform parent)
    {
        messageBoardObject = CreatePanelObject("MessageBoard", parent, new Vector2(760f, 330f));
        CreateText("来訪者のメッセージ", messageBoardObject.transform, new Vector2(0f, 126f), new Vector2(640f, 36f), 24);
        messageBoardText = CreateText(string.Empty, messageBoardObject.transform, new Vector2(0f, 12f), new Vector2(690f, 190f), 18);
        CreateButtonWithLabel("CloseMessageBoard", "Close", messageBoardObject.transform, new Vector2(0f, -132f), new Vector2(160f, 40f), ToggleMessageBoard);
        messageBoardObject.SetActive(false);
    }

    private void SelectGuest(int guestIndex)
    {
        selectedGuestIndex = guestIndex;
        operationController.SetSelectedGuestIndex(selectedGuestIndex);
        Refresh();
    }

    private void SelectMenu(int menuIndex)
    {
        selectedMenuIndex = menuIndex;
        Refresh();
    }

    private void Serve()
    {
        bool served = operationController.TryServe(selectedGuestIndex, selectedMenuIndex, out string resultMessage);

        if (served)
        {
            int reward = operationController.MenuItems[selectedMenuIndex].FaithPointReward;
            statusText.text = $"{resultMessage}\n信仰値 +{reward}";
        }
        else
        {
            statusText.text = resultMessage;
        }

        Refresh();

        if (served && operationController.LastServeGrantedHeartFox)
        {
            ShowHeartFoxFeedback();
        }
        else if (!served)
        {
            HideHeartFoxFeedback();
        }
    }

    private void CreateHeartFoxFeedback(Transform parent)
    {
        heartFoxFeedbackObject = CreatePanelObject("HeartFoxRewardFeedback", parent, new Vector2(260f, 44f));

        RectTransform rect = heartFoxFeedbackObject.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(305f, 184f);

        heartFoxFeedbackIcon = CreateIcon("HeartFoxIcon", null, heartFoxFeedbackObject.transform, new Vector2(-100f, 0f), new Vector2(34f, 34f));
        heartFoxFeedbackPlaceholder = CreateText("狐", heartFoxFeedbackObject.transform, new Vector2(-100f, 0f), new Vector2(34f, 34f), 20);
        heartFoxFeedbackPlaceholder.color = new Color(1f, 0.72f, 0.78f, 1f);
        CreateText("こころ狐を受け取りました。", heartFoxFeedbackObject.transform, new Vector2(30f, 0f), new Vector2(200f, 34f), 15);
        heartFoxFeedbackObject.SetActive(false);
    }

    private void ShowHeartFoxFeedback()
    {
        if (heartFoxFeedbackObject == null)
        {
            return;
        }

        Sprite icon = operationController != null ? operationController.HeartFoxIcon : null;

        if (heartFoxFeedbackIcon != null)
        {
            heartFoxFeedbackIcon.sprite = icon;
            heartFoxFeedbackIcon.enabled = icon != null;
        }

        if (heartFoxFeedbackPlaceholder != null)
        {
            heartFoxFeedbackPlaceholder.enabled = icon == null;
        }

        if (icon == null && operationController != null)
        {
            operationController.WarnMissingHeartFoxIconOnce();
        }

        heartFoxFeedbackObject.SetActive(true);

        if (heartFoxFeedbackCoroutine != null)
        {
            StopCoroutine(heartFoxFeedbackCoroutine);
        }

        heartFoxFeedbackCoroutine = StartCoroutine(HideHeartFoxFeedbackAfterDelay());
    }

    private IEnumerator HideHeartFoxFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(2.2f);
        heartFoxFeedbackCoroutine = null;

        if (heartFoxFeedbackObject != null)
        {
            heartFoxFeedbackObject.SetActive(false);
        }
    }

    private void HideHeartFoxFeedback()
    {
        if (heartFoxFeedbackCoroutine != null)
        {
            StopCoroutine(heartFoxFeedbackCoroutine);
            heartFoxFeedbackCoroutine = null;
        }

        if (heartFoxFeedbackObject != null)
        {
            heartFoxFeedbackObject.SetActive(false);
        }
    }

    private void OpenForBusiness()
    {
        if (operationController.TryOpenForBusiness())
        {
            statusText.text = "営業を開始しました。来訪者が入店します。";
            Refresh();
            Hide();
            return;
        }

        Refresh();
    }

    private void ToggleMessageBoard()
    {
        if (messageBoardObject == null)
        {
            return;
        }

        bool shouldShow = !messageBoardObject.activeSelf;

        if (shouldShow)
        {
            messageBoardText.text = operationController.BuildMessageBoardSummary();
        }

        messageBoardObject.SetActive(shouldShow);
    }

    private void Refresh()
    {
        if (operationController == null)
        {
            return;
        }

        faithPointText.text = $"信仰値: {operationController.FaithPoints}  こころ狐: {operationController.HeartFoxCount}";
        bool isOpenForBusiness = operationController.IsOpenForBusiness;
        bool hasVisitors = HasCurrentVisitors();

        if (selectedGuestIndex >= operationController.Guests.Count
            || (selectedGuestIndex >= 0 && !operationController.Guests[selectedGuestIndex].CanServe))
        {
            selectedGuestIndex = -1;
            operationController.SetSelectedGuestIndex(-1);
        }

        if (openBusinessButton != null)
        {
            openBusinessButton.interactable = !isOpenForBusiness;
            openBusinessButtonText.text = isOpenForBusiness ? "営業中" : "開業";
        }

        if (serveButton != null)
        {
            bool canServeSelectedGuest = isOpenForBusiness
                && selectedGuestIndex >= 0
                && selectedGuestIndex < operationController.Guests.Count
                && operationController.Guests[selectedGuestIndex].CanServe
                && selectedMenuIndex >= 0
                && selectedMenuIndex < operationController.MenuItems.Count;
            serveButton.interactable = canServeSelectedGuest;
        }

        for (int i = 0; i < guestButtons.Count; i++)
        {
            if (i >= operationController.Guests.Count)
            {
                guestButtons[i].gameObject.SetActive(false);
                guestButtonTexts[i].text = string.Empty;
                guestButtons[i].interactable = false;

                if (i < guestButtonIcons.Count)
                {
                    guestButtonIcons[i].enabled = false;
                }

                continue;
            }

            guestButtons[i].gameObject.SetActive(true);
            CafeGuestState guest = operationController.Guests[i];
            if (!guest.IsOccupied)
            {
                guestButtonTexts[i].text = $"{guest.SeatName}\n空席";
                guestButtons[i].interactable = false;

                if (i < guestButtonIcons.Count)
                {
                    guestButtonIcons[i].enabled = false;
                }

                SetButtonSelected(guestButtons[i], false);
                continue;
            }

            string request = GetGuestRequestStatus(guest);
            guestButtonTexts[i].text =
                $"{guest.SeatName}  {guest.DisplayName} [{guest.VisitorTypeLabel}]\n" +
                $"{guest.ServiceStateLabel}: {request}  /  好感度 {guest.Affection}\n" +
                $"好き: {guest.FavoriteMenuSummary}";
            guestButtons[i].interactable = isOpenForBusiness && guest.CanServe;

            if (i < guestButtonIcons.Count)
            {
                guestButtonIcons[i].sprite = guest.Icon;
                guestButtonIcons[i].enabled = guest.Icon != null;
            }

            SetButtonSelected(guestButtons[i], i == selectedGuestIndex);
        }

        if (!hasVisitors)
        {
            statusText.text = "今は来訪者がいません。";
        }
        else if (panelObject.activeSelf
            && !string.IsNullOrEmpty(operationController.LastCafeFeedbackMessage)
            && shownCafeFeedbackVersion != operationController.CafeFeedbackVersion)
        {
            shownCafeFeedbackVersion = operationController.CafeFeedbackVersion;
            shownCafeFeedbackMessage = operationController.LastCafeFeedbackMessage;
            statusText.text = shownCafeFeedbackMessage;
        }

        for (int i = 0; i < menuButtons.Count; i++)
        {
            CafeMenuItem menuItem = operationController.MenuItems[i];
            menuButtonTexts[i].text = $"{menuItem.DisplayName}  +{menuItem.FaithPointReward} 信仰値\n{operationController.BuildIngredientRequirementSummary(menuItem)}";
            menuButtons[i].interactable = isOpenForBusiness;
            SetButtonSelected(menuButtons[i], i == selectedMenuIndex);
        }

        if (messageBoardObject != null && messageBoardObject.activeSelf)
        {
            messageBoardText.text = operationController.BuildMessageBoardSummary();
        }
    }

    private string GetGuestRequestStatus(CafeGuestState guest)
    {
        if (guest == null)
        {
            return string.Empty;
        }

        if (guest.IsLeaving)
        {
            return "退店中";
        }

        if (guest.IsServed)
        {
            return "メッセージを残しています";
        }

        return string.IsNullOrEmpty(guest.RequestedMenuDisplayName)
            ? "開業待ち"
            : guest.RequestedMenuDisplayName;
    }

    private bool HasCurrentVisitors()
    {
        for (int i = 0; i < operationController.Guests.Count; i++)
        {
            if (operationController.Guests[i].IsOccupied)
            {
                return true;
            }
        }

        return false;
    }

    private void SetButtonSelected(Button button, bool isSelected)
    {
        Image image = button.GetComponent<Image>();
        image.color = isSelected
            ? new Color(0.82f, 0.64f, 0.28f, 1f)
            : new Color(0.23f, 0.2f, 0.18f, 1f);
    }

    private GameObject CreatePanelObject(string objectName, Transform parent, Vector2 size)
    {
        GameObject panel = new GameObject(objectName);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.07f, 0.055f, 0.05f, 0.94f);
        return panel;
    }

    private Button CreateButton(string objectName, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.23f, 0.2f, 0.18f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.onClick.AddListener(action);
        return button;
    }

    private Button CreateButtonWithLabel(
        string objectName,
        string label,
        Transform parent,
        Vector2 position,
        Vector2 size,
        UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(objectName, parent, position, size, action);
        Text text = CreateText(label, button.transform, Vector2.zero, size - new Vector2(10f, 8f), 18);
        text.color = Color.white;
        return button;
    }

    private Button CreateButtonWithLabel(
        string objectName,
        string label,
        Transform parent,
        Vector2 position,
        Vector2 size,
        UnityEngine.Events.UnityAction action,
        out Text labelText)
    {
        Button button = CreateButton(objectName, parent, position, size, action);
        labelText = CreateText(label, button.transform, Vector2.zero, size - new Vector2(10f, 8f), 18);
        labelText.color = Color.white;
        return button;
    }

    private Image CreateIcon(string objectName, Sprite sprite, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject iconObject = new GameObject(objectName);
        iconObject.transform.SetParent(parent, false);

        RectTransform rect = iconObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = iconObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private Text CreateText(string text, Transform parent, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.fontSize = fontSize;
        uiText.color = Color.white;
        uiText.font = GetUiFont();
        return uiText;
    }

    private Font GetUiFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Meiryo", "Arial" }, 18);
        }

        return font;
    }
}
