using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "NightShrineUITheme", menuName = "Night Shrine/UI Theme")]
public sealed class NightShrineUITheme : ScriptableObject
{
    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private TMP_FontAsset menuFont;
    [SerializeField] private TMP_FontAsset numberFont;

    [Header("Colors")]
    [SerializeField] private Color textPrimary = new Color32(0xF6, 0xE7, 0xC6, 0xFF);
    [SerializeField] private Color textGold = new Color32(0xF2, 0xC9, 0x6B, 0xFF);
    [SerializeField] private Color textGoldDark = new Color32(0xB8, 0x89, 0x42, 0xFF);
    [SerializeField] private Color panelDarkPurple = new Color32(0x2B, 0x1D, 0x35, 0xFF);
    [SerializeField] private Color panelDarkBrown = new Color32(0x3A, 0x24, 0x1C, 0xFF);
    [SerializeField] private Color warningRed = new Color32(0xC9, 0x5A, 0x4A, 0xFF);
    [SerializeField] private Color spiritBlue = new Color32(0x70, 0xCF, 0xFF, 0xFF);
    [SerializeField] private Color successGreen = new Color32(0x8F, 0xD1, 0x7A, 0xFF);
    [SerializeField] private Color disabledGray = new Color32(0x7B, 0x75, 0x80, 0xFF);
    [SerializeField] private Color outlineDark = new Color32(0x1A, 0x10, 0x20, 0xFF);

    [Header("Typography")]
    [SerializeField, Min(1f)] private float defaultFontSize = 24f;
    [SerializeField, Min(1f)] private float menuFontSize = 30f;
    [SerializeField, Min(1f)] private float bodyFontSize = 24f;
    [SerializeField, Min(1f)] private float numberFontSize = 28f;

    [Header("Button Motion")]
    [SerializeField, Min(0.1f)] private float buttonNormalScale = 1f;
    [SerializeField, Min(0.1f)] private float buttonSelectedScale = 1.06f;

    public Color TextPrimary => textPrimary;
    public Color TextGold => textGold;
    public Color TextGoldDark => textGoldDark;
    public Color PanelDarkPurple => panelDarkPurple;
    public Color PanelDarkBrown => panelDarkBrown;
    public Color WarningRed => warningRed;
    public Color SpiritBlue => spiritBlue;
    public Color SuccessGreen => successGreen;
    public Color DisabledGray => disabledGray;
    public Color OutlineDark => outlineDark;
    public float DefaultFontSize => defaultFontSize;
    public float MenuFontSize => menuFontSize;
    public float BodyFontSize => bodyFontSize;
    public float NumberFontSize => numberFontSize;
    public float ButtonNormalScale => buttonNormalScale;
    public float ButtonSelectedScale => buttonSelectedScale;

    public TMP_FontAsset ResolveBodyFont()
    {
        return bodyFont != null ? bodyFont : TMP_Settings.defaultFontAsset;
    }

    public TMP_FontAsset ResolveMenuFont()
    {
        return menuFont != null ? menuFont : TMP_Settings.defaultFontAsset;
    }

    public TMP_FontAsset ResolveNumberFont()
    {
        return numberFont != null ? numberFont : TMP_Settings.defaultFontAsset;
    }
}
