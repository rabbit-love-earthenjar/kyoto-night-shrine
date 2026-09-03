using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class NightShrinePauseMenuController : MonoBehaviour
{
    private const string StartSceneName = "StartScene";

    [Header("Inspector Bindings")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button returnToTitleButton;
    [SerializeField] private Button quitButton;

    private bool isOpen;

    private void Awake()
    {
        BindButtons();
        SetMenuOpen(false);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (isOpen && settingsPanel != null && settingsPanel.activeSelf)
        {
            ShowMainPanel();
            return;
        }

        SetMenuOpen(!isOpen);
    }

    private void OnDisable()
    {
        if (isOpen)
        {
            Time.timeScale = 1f;
            isOpen = false;
        }
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    public void OpenPauseMenu()
    {
        SetMenuOpen(true);
    }

    public void ClosePauseMenu()
    {
        SetMenuOpen(false);
    }

    public void OpenSettings()
    {
        Debug.Log("Open Settings");
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void ReturnToTitle()
    {
        Time.timeScale = 1f;
        isOpen = false;
        SceneManager.LoadScene(StartSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("Quit Game requested. Application.Quit runs only in a player build.");
#else
        Application.Quit();
#endif
    }

    private void ShowMainPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }

        SelectButton(resumeButton);
    }

    private void SetMenuOpen(bool open)
    {
        isOpen = open;
        Time.timeScale = open ? 0f : 1f;

        if (menuRoot != null)
        {
            menuRoot.SetActive(open);
        }

        if (open)
        {
            ShowMainPanel();
        }
    }

    private void BindButtons()
    {
        resumeButton?.onClick.AddListener(ClosePauseMenu);
        settingsButton?.onClick.AddListener(OpenSettings);
        settingsBackButton?.onClick.AddListener(ShowMainPanel);
        returnToTitleButton?.onClick.AddListener(ReturnToTitle);
        quitButton?.onClick.AddListener(QuitGame);
    }

    private void UnbindButtons()
    {
        resumeButton?.onClick.RemoveListener(ClosePauseMenu);
        settingsButton?.onClick.RemoveListener(OpenSettings);
        settingsBackButton?.onClick.RemoveListener(ShowMainPanel);
        returnToTitleButton?.onClick.RemoveListener(ReturnToTitle);
        quitButton?.onClick.RemoveListener(QuitGame);
    }

    private static void SelectButton(Button target)
    {
        if (target != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }
}
