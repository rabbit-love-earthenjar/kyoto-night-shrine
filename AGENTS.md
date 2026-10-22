# AGENTS.md

## Project
This is a small 2D pixel-art ACT + simulation game set in Kyoto.
The player is a modern spiritual worker inspired by a Kyoto deity / Taoist exorcist protagonist.
The game combines daytime shop/shrine management with nighttime action-based purification.

## Core Direction
- Engine: Unity 2D
- Style: 2D pixel art, top-down or side-view, small maps first
- Mood: Kyoto night, shrine alleys, rain, lanterns, vending machines, quiet supernatural atmosphere
- Scope: small MVP first, not a large RPG
- Main loop:
  1. Daytime: manage shop/shrine, craft charms, talk to visitors
  2. Nighttime: explore one small Kyoto map, fight/purify spirits
  3. Rewards: collect materials and faith points
  4. Upgrade: unlock items, dialogue, and stronger tools

## MVP Scope
Only build the first playable demo:
- One shop/shrine scene
- One nighttime action map
- One controllable player
- Basic movement
- One simple attack or purification action
- Three item types
- Three customer/request types
- One enemy/spirit type
- One result screen
- Basic save/load if simple enough

Do not add large systems unless asked.

## Development Rules
- Prioritize playable prototype over visual polish.
- Use placeholder assets when needed.
- Keep scripts small and readable.
- Avoid over-engineering.
- Use clear Unity component names.
- Write comments for gameplay logic.
- Keep all values easy to tune in Inspector or ScriptableObject.
- Do not introduce paid assets or external services.

## Folder Structure
Use this structure when possible:

Assets/
  Art/
  Audio/
  Prefabs/
  Scenes/
  Scripts/
    Core/
    Player/
    Combat/
    Shop/
    Dialogue/
    UI/
  ScriptableObjects/
  Tilemaps/

Docs/
  GameDesign.md
  SystemDesign.md
  TaskList.md
  Changelog.md

## Documentation Requirements
When implementing features, update docs if relevant:
- GameDesign.md: concept, core loop, player experience
- SystemDesign.md: systems, data structure, UI flow
- TaskList.md: current tasks and next steps
- Changelog.md: completed changes
- DevelopmentStatus.md: append a dated development entry after every independent task or important development stage; retain all previous entries.

### Persistent Development Record
- Use `Docs/DevelopmentStatus.md` as the next conversation's development handoff. Read its latest entries before continuing implementation.
- Every new entry must include the date and task name; objective, completed and unfinished work; added and modified files; important architecture or behavior changes; tests executed and their results; unverified features and known bugs; current working and in-progress features; prioritized next tasks; and concrete next-conversation tasks, files to read, and technical limits.
- Never overwrite or delete historical development entries. If older information is superseded, append the correction with its date and evidence.
- Distinguish code inspection, compilation, automated Unity Play Mode checks, and manual playtesting. Record planned or untested behavior as unverified. Identify historical test evidence by its original date; do not present it as a fresh test.
- At the end of each task, proactively update the record and briefly state what was added in the response.

### Workspace and Art Ownership
- Preserve the existing project architecture and the user's working changes. Do not delete, reset, or commit Git changes without explicit user authorization.
- The user organizes and imports art assets. Focus implementation on code and references to existing resources.
- Do not change Import Settings for textures, Sprites, Prefabs, or other assets without explicit user authorization.
- Do not automatically slice, rename, or reimport art assets. Do not run import or scene-building tools that alter those settings as an incidental implementation step.

## Coding Style
- C# for Unity.
- Prefer descriptive names.
- Avoid giant manager scripts.
- Separate player control, combat, shop, dialogue, and UI logic.
- Use serialized fields instead of hard-coded values when possible.
- Keep MVP simple.

## Definition of Done
A task is done only when:
- The scene runs in Unity without errors.
- The feature can be tested manually.
- The code is readable.
- Related documentation is updated.
- No unrelated systems are changed.

## Do Not Do
- Do not expand the story into a huge RPG.
- Do not create dozens of enemies, items, or maps.
- Do not rewrite the whole project without reason.
- Do not focus on final art before the core loop works.
- Do not invent complex lore unless asked.

## 持續開發紀錄與接續規則

- 每次開始工作，先讀取 `Docs/DevelopmentStatus.md` 的維護說明與最新紀錄，再核對相關程式、場景、文件和 Git 工作區狀態。此文件尚不存在時建立它。
- 每完成一個獨立任務或重要開發階段，必須在 `Docs/DevelopmentStatus.md` 末尾新增紀錄；中途受阻或未完成時也記錄實際進度、原因與接續事項。
- 日期使用 Asia/Tokyo。每筆紀錄都必須包含以下五部分，即使沒有變動，也要明寫「無」或「未執行」：
  1. 更新日期與任務名稱；本次目標、已完成內容、尚未完成內容。
  2. 程式碼與資源變更；新增檔案、修改檔案、重要架構與功能變動。
  3. 測試與驗證；已執行測試、結果、尚未驗證功能、已知 Bug。
  4. 目前專案狀態；已實作且可運作功能及其證據、開發中功能、附優先順序的待辦事項。
  5. 下一個 Codex 對話的接續資訊；下一個具體任務、必讀檔案、技術限制。
- 不得覆蓋、刪除或改寫過去的開發紀錄；發現舊紀錄錯誤時，新增有日期與證據的更正說明。最新一筆紀錄代表當時狀態，舊紀錄仍保留。
- 將本次執行、歷史日誌、靜態檢查與人工驗收分開記錄；附上相關檔案或日誌位置。未測試功能不得標為已驗證，自動化整合通過不得當作人工通關或趣味性驗收。
- 僅修改本次任務相關內容並遵守現有架構。開始前已存在的 Git 變更屬於既有工作；除非使用者明確授權，不得刪除、重置、暫存或提交 Git 變更，也不得自行清理未追蹤檔案。
- 美術素材由使用者手動整理與導入。Codex 主要負責程式碼與既有資源引用；不得擅自更改 Unity 圖片、Sprite、Prefab 或其他素材的 Import Settings，不得自動裁切、重命名或重新匯入美術素材。
- 每次任務結束，在回覆中簡短說明 `Docs/DevelopmentStatus.md` 新增了哪些內容；其他設計文件仍依原有 Documentation Requirements 在相關時更新。
