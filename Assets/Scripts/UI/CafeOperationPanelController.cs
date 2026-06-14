using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CafeOperationPanelController : MonoBehaviour
{
    private const string InariCoffeeMenuId = "inari_coffee";
    private const string KitsunebiLatteMenuId = "kitsunebi_latte";

    [Header("Popup Roots")]
    [SerializeField] private GameObject productionPopupRoot;
    [SerializeField] private GameObject recipePanelRoot;
    [SerializeField] private GameObject progressRoot;
    [SerializeField] private GameObject completeCheckRoot;

    [Header("Buttons")]
    [SerializeField] private Button coffeeMachineButton;
    [SerializeField] private Button inariCoffeeRecipeButton;
    [SerializeField] private Button kitsunebiLatteRecipeButton;
    [SerializeField] private Button closeButton;

    [Header("Progress")]
    [SerializeField] private Image progressFillImage;
    [SerializeField] private float completeHoldSeconds = 1f;

    [Header("Optional Text")]
    [SerializeField] private Text statusText;
    [SerializeField] private Text inariCoffeeRecipeText;
    [SerializeField] private Text kitsunebiLatteRecipeText;

    private CafeOperationController operationController;
    private Coroutine productionCoroutine;
    private bool listenersWired;

    public void Initialize(Transform canvasRoot, CafeOperationController controller)
    {
        operationController = controller;
        WireButtonListeners();
        HideAllProductionUi();
    }

    private void Awake()
    {
        WireButtonListeners();
        HideAllProductionUi();
    }

    private void OnDestroy()
    {
        UnwireButtonListeners();
    }

    public void Show()
    {
        if (!ValidateRequiredBindings())
        {
            return;
        }

        SetRootActive(productionPopupRoot, true);
        SetRootActive(recipePanelRoot, false);
        SetRootActive(progressRoot, false);
        SetRootActive(completeCheckRoot, false);
        SetProgress(0f);
        SetCoffeeMachineInteractable(true);
        SetStatus("コーヒーマシンを選んでください。");
    }

    public void Hide()
    {
        SetRootActive(productionPopupRoot, false);
        SetRootActive(recipePanelRoot, false);

        if (productionCoroutine == null)
        {
            SetRootActive(progressRoot, false);
            SetRootActive(completeCheckRoot, false);
            SetProgress(0f);
            SetCoffeeMachineInteractable(true);
        }
    }

    public void OnCoffeeMachineClicked()
    {
        if (productionCoroutine != null)
        {
            return;
        }

        SetRootActive(productionPopupRoot, true);
        SetRootActive(recipePanelRoot, true);
        SetRootActive(progressRoot, false);
        SetRootActive(completeCheckRoot, false);
        SetProgress(0f);
        RefreshRecipeLabels();
        SetCoffeeMachineInteractable(true);
        SetStatus("レシピを選んでください。");
    }

    public void OnInariCoffeeRecipeClicked()
    {
        SelectRecipeAndStart(InariCoffeeMenuId);
    }

    public void OnKitsunebiLatteRecipeClicked()
    {
        SelectRecipeAndStart(KitsunebiLatteMenuId);
    }

    private void SelectRecipeAndStart(string menuId)
    {
        if (productionCoroutine != null)
        {
            return;
        }

        SetRootActive(recipePanelRoot, false);
        CafeMenuItem menuItem = FindMenuItem(menuId, out int menuIndex);

        if (menuItem == null)
        {
            SetStatus($"レシピが見つかりません: {menuId}");
            SetCoffeeMachineInteractable(true);
            return;
        }

        CafeMenuItem startedMenuItem = null;
        string resultMessage = "Production controller is missing.";
        bool started = operationController != null
            && operationController.TryStartProduction(menuIndex, out startedMenuItem, out resultMessage);

        SetStatus(resultMessage);

        if (!started || startedMenuItem == null)
        {
            SetRootActive(progressRoot, false);
            SetRootActive(completeCheckRoot, false);
            SetProgress(0f);
            SetCoffeeMachineInteractable(true);
            return;
        }

        productionCoroutine = StartCoroutine(RunCoffeeProduction(startedMenuItem));
    }

    private IEnumerator RunCoffeeProduction(CafeMenuItem menuItem)
    {
        SetCoffeeMachineInteractable(false);
        SetRootActive(progressRoot, true);
        SetRootActive(completeCheckRoot, false);
        SetProgress(0f);

        float duration = operationController != null ? operationController.ProductionSeconds : 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            SetProgress(progress);
            SetStatus($"制作中: {menuItem.DisplayName} {Mathf.RoundToInt(progress * 100f)}%");
            yield return null;
        }

        SetProgress(1f);
        SetRootActive(completeCheckRoot, true);

        if (operationController != null)
        {
            operationController.CompleteProduction(menuItem, out string resultMessage);
            SetStatus(resultMessage);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, completeHoldSeconds));

        SetRootActive(progressRoot, false);
        SetRootActive(completeCheckRoot, false);
        SetProgress(0f);
        SetCoffeeMachineInteractable(true);
        productionCoroutine = null;
    }

    private CafeMenuItem FindMenuItem(string menuId, out int menuIndex)
    {
        menuIndex = -1;

        if (operationController == null)
        {
            return null;
        }

        for (int i = 0; i < operationController.MenuItems.Count; i++)
        {
            CafeMenuItem menuItem = operationController.MenuItems[i];

            if (menuItem != null && menuItem.MenuId == menuId)
            {
                menuIndex = i;
                return menuItem;
            }
        }

        return null;
    }

    private void RefreshRecipeLabels()
    {
        if (inariCoffeeRecipeText != null)
        {
            inariCoffeeRecipeText.text = "InariCoffee";
        }

        if (kitsunebiLatteRecipeText != null)
        {
            kitsunebiLatteRecipeText.text = "KitsunebiLatte";
        }
    }

    private void SetProgress(float progress)
    {
        if (progressFillImage == null)
        {
            return;
        }

        progressFillImage.type = Image.Type.Filled;
        progressFillImage.fillMethod = Image.FillMethod.Horizontal;
        progressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFillImage.fillAmount = Mathf.Clamp01(progress);
    }

    private void SetCoffeeMachineInteractable(bool isInteractable)
    {
        if (coffeeMachineButton != null)
        {
            coffeeMachineButton.interactable = isInteractable;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void HideAllProductionUi()
    {
        SetRootActive(productionPopupRoot, false);
        SetRootActive(recipePanelRoot, false);
        SetRootActive(progressRoot, false);
        SetRootActive(completeCheckRoot, false);
        SetProgress(0f);
        SetCoffeeMachineInteractable(true);
    }

    private void SetRootActive(GameObject root, bool isActive)
    {
        if (root != null)
        {
            root.SetActive(isActive);
        }
    }

    private bool ValidateRequiredBindings()
    {
        if (productionPopupRoot != null
            && recipePanelRoot != null
            && progressRoot != null
            && completeCheckRoot != null
            && coffeeMachineButton != null
            && inariCoffeeRecipeButton != null
            && kitsunebiLatteRecipeButton != null
            && progressFillImage != null)
        {
            return true;
        }

        Debug.LogWarning(
            "Cafe production UI is not fully bound. Please assign ProductionPopupRoot, RecipePanelRoot, ProgressRoot, CompleteCheckRoot, coffee machine button, recipe buttons, and progress fill image in the Inspector.");
        return false;
    }

    private void WireButtonListeners()
    {
        if (listenersWired)
        {
            return;
        }

        AddListener(coffeeMachineButton, OnCoffeeMachineClicked);
        AddListener(inariCoffeeRecipeButton, OnInariCoffeeRecipeClicked);
        AddListener(kitsunebiLatteRecipeButton, OnKitsunebiLatteRecipeClicked);
        AddListener(closeButton, Hide);
        listenersWired = true;
    }

    private void UnwireButtonListeners()
    {
        RemoveListener(coffeeMachineButton, OnCoffeeMachineClicked);
        RemoveListener(inariCoffeeRecipeButton, OnInariCoffeeRecipeClicked);
        RemoveListener(kitsunebiLatteRecipeButton, OnKitsunebiLatteRecipeClicked);
        RemoveListener(closeButton, Hide);
        listenersWired = false;
    }

    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
    }
}
