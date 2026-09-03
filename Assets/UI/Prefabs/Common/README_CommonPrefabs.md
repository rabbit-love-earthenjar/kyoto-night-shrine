# Common UI Prefabs

- `UI_Button_NightShrine`: shared TMP button with Normal, Selected, and Disabled presentation.
- `UI_ConfirmDialog_NightShrine`: reusable title, body, confirm, and cancel dialog.
- `UI_PauseMenu_NightShrine`: Inspector-bound ESC menu. Add it to a scene only after checking that the scene does not already own another pause controller.
- `UI_TextPanel_NightShrine`: generic body-text panel for dialogue, notices, and recipe descriptions.

Replace placeholder panel colors or sprites through prefab overrides or the Inspector. Keep fixed rect bounds when swapping button states so selection never shifts the label.

