# Night Shrine UI Fonts

The reusable UI layer uses TextMeshPro for all system text.

## Current temporary fallback

- `NightShrineUITheme` falls back to `TMP_Settings.defaultFontAsset` when a role-specific font is not assigned.
- This fallback is temporary and may not contain Japanese or Chinese glyphs.
- Runtime-generated legacy UI currently uses `NightShrineTextStyle`: it prefers installed Japanese/Chinese UI fonts, applies Bold, and adds a compact dark outline so menus visually approach the warm rounded cover lettering without bundling an unlicensed font.
- The fixed StartScene menu keeps its baked artwork lettering. It is a visual reference, not a reusable font asset.
- Codex must not download fonts for this project.

## Formal font requirements

- The developer must manually add a font file with a clear, legal license.
- The final font assets must support Japanese, Simplified/Traditional Chinese, English, and digits.
- Do not commit font files from unknown or unverifiable sources.
- Create TMP Font Assets from the approved source font, then assign the Body, Menu, and Number slots on `NightShrineUITheme.asset`.
- The preferred final direction is a cute rounded display face with pixel-friendly counters, heavy enough strokes for a dark outline, and stable Japanese/Chinese baseline metrics. Replace the theme font slots rather than hard-coding fonts per scene.

## Text roles

### Body

Use for NPC dialogue, system explanations, item descriptions, and recipe text.

- Color: `#F6E7C6`
- Light dark outline or shadow
- Default size: 24

### Menu

Use for buttons, menu options, dialog titles, and compact headings.

- Normal color: `#F6E7C6`
- Accent/selected color: `#F2C96B`
- Default size: 30

### Number

Use for HUD values, Faith, currency, and inventory counts.

- Prioritize legibility and stable character width
- Default size: 28
