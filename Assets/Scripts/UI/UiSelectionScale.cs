using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UiSelectionScale : MonoBehaviour,
    ISelectHandler,
    IDeselectHandler,
    IPointerEnterHandler,
    IPointerDownHandler
{
    [SerializeField, Min(1f)] private float selectedScale = 1.08f;
    [SerializeField, Min(1f)] private float responseSpeed = 12f;
    [SerializeField, Range(0f, 1f)] private float normalAlpha = 0.78f;
    [SerializeField, Range(0f, 1f)] private float selectedAlpha = 0.98f;

    private Vector3 normalScale;
    private Vector3 targetScale;
    private Transform scaleTarget;
    private Graphic targetGraphic;
    private float targetAlpha;
    private RectTransform fixedLabel;
    private Vector2 fixedLabelPosition;
    private Vector3 fixedLabelScale;

    public bool IsConfigured { get; private set; }

    private void Awake()
    {
        scaleTarget = transform;
        normalScale = scaleTarget.localScale;
        targetScale = normalScale;
        targetGraphic = GetComponent<Graphic>();
        targetAlpha = targetGraphic != null ? targetGraphic.color.a : 1f;
    }

    private void Update()
    {
        if (scaleTarget == null)
        {
            scaleTarget = transform;
        }

        scaleTarget.localScale = Vector3.Lerp(
            scaleTarget.localScale,
            targetScale,
            1f - Mathf.Exp(-responseSpeed * Time.unscaledDeltaTime));

        if (targetGraphic != null)
        {
            Color color = targetGraphic.color;
            color.a = Mathf.Lerp(
                color.a,
                targetAlpha,
                1f - Mathf.Exp(-responseSpeed * Time.unscaledDeltaTime));
            targetGraphic.color = color;
        }
        if (fixedLabel != null)
        {
            fixedLabel.anchoredPosition = fixedLabelPosition;
            if (scaleTarget != null && fixedLabel.IsChildOf(scaleTarget))
            {
                Vector3 currentScale = scaleTarget.localScale;
                fixedLabel.localScale = new Vector3(
                    fixedLabelScale.x * SafeScaleRatio(normalScale.x, currentScale.x),
                    fixedLabelScale.y * SafeScaleRatio(normalScale.y, currentScale.y),
                    fixedLabelScale.z * SafeScaleRatio(normalScale.z, currentScale.z));
            }
            else
            {
                fixedLabel.localScale = fixedLabelScale;
            }
        }
    }

    private void OnDisable()
    {
        if (scaleTarget != null)
        {
            scaleTarget.localScale = normalScale;
        }
        targetScale = normalScale;
        if (targetGraphic != null)
        {
            Color color = targetGraphic.color;
            color.a = normalAlpha;
            targetGraphic.color = color;
        }
        if (fixedLabel != null)
        {
            fixedLabel.anchoredPosition = fixedLabelPosition;
            fixedLabel.localScale = fixedLabelScale;
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetSelected(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetSelected(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EventSystem.current?.SetSelectedGameObject(gameObject);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        EventSystem.current?.SetSelectedGameObject(gameObject);
    }

    public void Configure(
        float scale,
        Graphic graphic = null,
        float unselectedAlpha = 0.78f,
        float highlightedAlpha = 0.98f,
        Transform visualScaleTarget = null,
        RectTransform labelToKeepFixed = null)
    {
        selectedScale = Mathf.Max(1f, scale);
        scaleTarget = visualScaleTarget != null ? visualScaleTarget : transform;
        normalScale = scaleTarget.localScale;
        targetScale = normalScale;
        targetGraphic = graphic != null ? graphic : GetComponent<Graphic>();
        normalAlpha = Mathf.Clamp01(unselectedAlpha);
        selectedAlpha = Mathf.Clamp01(highlightedAlpha);
        fixedLabel = labelToKeepFixed;
        if (fixedLabel != null)
        {
            fixedLabelPosition = fixedLabel.anchoredPosition;
            fixedLabelScale = fixedLabel.localScale;
        }
        IsConfigured = true;
    }

    public void SetSelected(bool selected)
    {
        targetScale = selected ? normalScale * selectedScale : normalScale;
        targetAlpha = selected ? selectedAlpha : normalAlpha;
    }

    private static float SafeScaleRatio(float normal, float current)
    {
        return Mathf.Abs(current) > 0.0001f ? normal / current : 1f;
    }
}
