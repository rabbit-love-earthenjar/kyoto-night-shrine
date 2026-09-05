using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum NightShrineTextRole
{
    Body,
    Menu,
    Number
}

public static class NightShrineTextStyle
{
    private static readonly Color SoftShadow = new Color32(0x1A, 0x10, 0x20, 0x40);
    private static Font legacyFont;

    public static Font ResolveLegacyFont()
    {
        if (legacyFont != null)
        {
            return legacyFont;
        }

        legacyFont = Font.CreateDynamicFontFromOSFont(
            new[]
            {
                "Meiryo UI",
                "Yu Gothic UI",
                "Microsoft YaHei UI",
                "Microsoft JhengHei UI",
                "Arial"
            },
            24);

        if (legacyFont == null)
        {
            legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return legacyFont;
    }

    public static void Apply(Text text, NightShrineTextRole role = NightShrineTextRole.Body)
    {
        if (text == null)
        {
            return;
        }

        Font resolvedFont = ResolveLegacyFont();
        if (resolvedFont != null)
        {
            text.font = resolvedFont;
        }

        text.fontStyle = role == NightShrineTextRole.Menu ? FontStyle.Bold : FontStyle.Normal;
        text.alignByGeometry = true;

        Outline outline = text.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }

        Shadow shadow = null;
        Shadow[] shadows = text.GetComponents<Shadow>();
        for (int index = 0; index < shadows.Length; index++)
        {
            if (!(shadows[index] is Outline))
            {
                shadow = shadows[index];
                break;
            }
        }

        if (shadow == null)
        {
            shadow = text.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = SoftShadow;
        shadow.effectDistance = new Vector2(0.45f, -0.45f);
        shadow.useGraphicAlpha = true;
    }

    public static void Apply(TMP_Text text, NightShrineTextRole role = NightShrineTextRole.Body)
    {
        if (text == null)
        {
            return;
        }

        text.fontStyle = role == NightShrineTextRole.Menu ? FontStyles.Bold : FontStyles.Normal;
        text.outlineColor = SoftShadow;
        text.outlineWidth = role == NightShrineTextRole.Menu ? 0.045f : 0.025f;
    }
}
