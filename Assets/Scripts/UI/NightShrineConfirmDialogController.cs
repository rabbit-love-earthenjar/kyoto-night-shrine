using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class NightShrineConfirmDialogController : MonoBehaviour
{
    [SerializeField] private GameObject dialogRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action confirmAction;
    private Action cancelAction;

    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(Confirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(Cancel);
        }
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(Confirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(Cancel);
        }
    }

    public void Show(string title, string message, Action onConfirm = null, Action onCancel = null)
    {
        confirmAction = onConfirm;
        cancelAction = onCancel;

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (messageText != null)
        {
            messageText.text = message;
        }

        GameObject root = dialogRoot != null ? dialogRoot : gameObject;
        root.SetActive(true);

        if (confirmButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
        }
    }

    public void Hide()
    {
        confirmAction = null;
        cancelAction = null;
        GameObject root = dialogRoot != null ? dialogRoot : gameObject;
        root.SetActive(false);
    }

    public void Confirm()
    {
        Action action = confirmAction;
        Hide();
        action?.Invoke();
    }

    public void Cancel()
    {
        Action action = cancelAction;
        Hide();
        action?.Invoke();
    }
}
