using UnityEngine;

[CreateAssetMenu(fileName = "GameUiTheme", menuName = "Night Shrine/Game UI Theme")]
public sealed class GameUiTheme : ScriptableObject
{
    [Header("Cursor States")]
    [SerializeField] private Sprite cursorNormal;
    [SerializeField] private Sprite cursorHover;
    [SerializeField] private Sprite cursorClick;
    [SerializeField] private Sprite cursorForbidden;
    [SerializeField] private Sprite cursorHand;
    [SerializeField] private Sprite cursorDialogue;

    [Header("Menu Presentation")]
    [SerializeField] private Sprite largePanel;
    [SerializeField] private Sprite headerFrame;
    [SerializeField] private Sprite confirmPanel;
    [SerializeField] private Sprite purpleButtonNormal;
    [SerializeField] private Sprite purpleButtonSelected;
    [SerializeField] private Sprite redButtonNormal;
    [SerializeField] private Sprite redButtonSelected;

    public Sprite CursorNormal => cursorNormal;
    public Sprite CursorHover => cursorHover;
    public Sprite CursorClick => cursorClick;
    public Sprite CursorForbidden => cursorForbidden;
    public Sprite CursorHand => cursorHand;
    public Sprite CursorDialogue => cursorDialogue;
    public Sprite LargePanel => largePanel;
    public Sprite HeaderFrame => headerFrame;
    public Sprite ConfirmPanel => confirmPanel;
    public Sprite PurpleButtonNormal => purpleButtonNormal;
    public Sprite PurpleButtonSelected => purpleButtonSelected;
    public Sprite RedButtonNormal => redButtonNormal;
    public Sprite RedButtonSelected => redButtonSelected;

    public static GameUiTheme LoadDefault()
    {
        return Resources.Load<GameUiTheme>("UI/GameUiTheme");
    }
}
