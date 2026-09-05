using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class NightShrineButtonState : MonoBehaviour,
    ISelectHandler,
    IDeselectHandler,
    IPointerEnterHandler,
    IPointerDownHandler
{
    [SerializeField] private NightShrineUITheme theme;
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button button;
    [SerializeField, Min(1f)] private float responseSpeed = 12f;

    private Vector3 targetScale = Vector3.one;
    private bool isSelected;
    private bool wasInteractable;
    private Vector2 fixedLabelPosition;
    private Vector3 fixedLabelScale = Vector3.one;

    private void Awake()
    {
        visualRoot = visualRoot != null ? visualRoot : transform as RectTransform;
        background = background != null ? background : GetComponent<Image>();
        label = label != null ? label : GetComponentInChildren<TMP_Text>(true);
        button = button != null ? button : GetComponent<Button>();
        if (label != null)
        {
            fixedLabelPosition = label.rectTransform.anchoredPosition;
            fixedLabelScale = label.rectTransform.localScale;
        }
        wasInteractable = IsInteractable();
        ApplyState(true);
    }

    private void Update()
    {
        bool interactable = IsInteractable();
        if (interactable != wasInteractable)
        {
            wasInteractable = interactable;
            ApplyState(false);
        }

        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localScale = Vector3.Lerp(
            visualRoot.localScale,
            targetScale,
            1f - Mathf.Exp(-responseSpeed * Time.unscaledDeltaTime));
        KeepLabelFixed();
    }

    private void OnDisable()
    {
        isSelected = false;
        targetScale = Vector3.one * NormalScale;
        if (visualRoot != null)
        {
            visualRoot.localScale = targetScale;
        }
        if (label != null)
        {
            label.rectTransform.anchoredPosition = fixedLabelPosition;
            label.rectTransform.localScale = fixedLabelScale;
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        ApplyState(false);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        ApplyState(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsInteractable() && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsInteractable() && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }

        wasInteractable = interactable;
        ApplyState(false);
    }

    private bool IsInteractable()
    {
        return button == null || button.interactable;
    }

    private float NormalScale => theme != null ? theme.ButtonNormalScale : 1f;

    private float SelectedScale => theme != null ? theme.ButtonSelectedScale : 1.06f;

    private void ApplyState(bool immediate)
    {
        bool interactable = IsInteractable();
        float scale = interactable && isSelected ? SelectedScale : NormalScale;
        targetScale = Vector3.one * scale;

        if (immediate && visualRoot != null)
        {
            visualRoot.localScale = targetScale;
            KeepLabelFixed();
        }

        if (label != null)
        {
            TMP_FontAsset resolvedFont = theme != null ? theme.ResolveMenuFont() : TMP_Settings.defaultFontAsset;
            if (resolvedFont != null)
            {
                label.font = resolvedFont;
            }

            label.fontSize = theme != null ? theme.MenuFontSize : 30f;
            label.color = !interactable
                ? (theme != null ? theme.DisabledGray : Color.gray)
                : isSelected
                    ? (theme != null ? theme.TextGold : new Color32(0xF2, 0xC9, 0x6B, 0xFF))
                    : (theme != null ? theme.TextPrimary : Color.white);
            Color outlineColor = theme != null ? theme.OutlineDark : Color.black;
            outlineColor.a = 0.32f;
            label.outlineColor = outlineColor;
            label.outlineWidth = 0.055f;
            label.fontStyle |= FontStyles.Bold;
        }

        if (background != null)
        {
            Color color;
            if (!interactable)
            {
                color = theme != null ? theme.DisabledGray : Color.gray;
                color.a = 0.45f;
            }
            else if (isSelected)
            {
                color = theme != null ? theme.TextGoldDark : new Color32(0xB8, 0x89, 0x42, 0xFF);
                color.a = 0.92f;
            }
            else
            {
                color = theme != null ? theme.PanelDarkBrown : new Color32(0x3A, 0x24, 0x1C, 0xFF);
                color.a = 0.78f;
            }

            background.color = color;
        }
    }

    private void KeepLabelFixed()
    {
        if (label == null)
        {
            return;
        }

        RectTransform labelRect = label.rectTransform;
        labelRect.anchoredPosition = fixedLabelPosition;

        if (visualRoot != null && labelRect.IsChildOf(visualRoot))
        {
            Vector3 currentScale = visualRoot.localScale;
            labelRect.localScale = new Vector3(
                fixedLabelScale.x * SafeScaleRatio(NormalScale, currentScale.x),
                fixedLabelScale.y * SafeScaleRatio(NormalScale, currentScale.y),
                fixedLabelScale.z * SafeScaleRatio(NormalScale, currentScale.z));
        }
        else
        {
            labelRect.localScale = fixedLabelScale;
        }
    }

    private static float SafeScaleRatio(float normalScale, float currentScale)
    {
        return Mathf.Abs(currentScale) > 0.0001f ? normalScale / currentScale : 1f;
    }
}
