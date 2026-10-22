# Game Design

## Concept
The platforming design lab uses copies of `Stage_1_2` and `Stage_1_Route_Prototype`. V1 adds finite purification encounters, recovery beats, an extended opening route, and varied cloud traversal within the existing fox-shrine world. `LevelDesign_DesignLab.md` distinguishes implemented scene changes from unmeasured duration, pressure, and enjoyment. Unity playtest acceptance is blocked by licensing (exit 198); offline compilation and serialized-scene checks passed.

The reference study prioritizes Hollow Knight for connected exploration and enemy roles, Dave the Diver for outing rewards feeding management, and Dead Cells for controlled composition of authored challenge modules. These are design references; the shrine's existing world and daytime-management/nighttime-purification order remain the basis. See `LevelDesign_ReferenceStudy.md` for primary sources and the current implementation boundaries.

A 2D pixel-art ACT + simulation game set in modern Kyoto.

Players manage a small spiritual shrine/shop during the day and purify supernatural entities at night.

The demo should stay focused, playable, and atmospheric before it tries to become large.

## Tone
Quiet modern Kyoto with shrine alleys, rain, lanterns, vending machines, and a soft supernatural mood.

The player fantasy is practical spiritual work: helping visitors, preparing simple items, then going out at night to calm or purify spirits.

## Core Loop
Day:

- Manage shrine/shop.
- Talk with visitors.
- Craft spiritual items.

Night:

- Explore small Kyoto maps.
- Fight or purify spirits.
- Collect materials.

Progression:

- Improve shrine reputation.
- Unlock dialogue and tools.

## Stage 1 Route Prototype

The temporary Stage 1 route test uses a compact two-level path. The player starts at the upper-left spirit-lit torii, explores right along solid stone-platform terrain, then backtracks. The brown second-crossing platform collapses on the return trip and is the route's only upper-to-lower transition. The player can investigate the lower-right hidden cave before returning from right to left toward the simple lower-left clear torii.

The route sketch uses black outlines for solid terrain and black vertical marks for spike hazards. Brown crates are breakable, the brown dashed platform is safe on its first crossing and drops immediately on the second entry, gray clouds disappear after being stood on and later recover, green markers are pursuing ground enemies, blue markers are diving flying enemies, and yellow stars use the existing StarSeal/talisman collectible system. The lower-right StarSeal is hidden in a reversible cave detour and appears only after its two blocking crates are destroyed and the player moves close enough to discover it.

The ordinary route keeps the existing Kagura-bell melee combat. The giant Red Oni encounter and its temporary bean attack will be designed as a separate boss stage with background-scale boss presentation, rather than being attached directly to `Stage_1_1`.

## MVP Content
- One shrine/shop scene.
- One nighttime action map.
- One controllable player.
- Basic movement.
- Camera follow.
- Simple tilemap.
- One simple attack or purification action.
- Three item types.
- Three customer/request types.
- One enemy/spirit type.
- One result screen.

## Starter Item Types
- Paper Charm: basic request item and common purification tool.
- Incense Bundle: support item tied to calm/restoration requests.
- Salt Packet: simple protection or cleansing material.

## Starter Request Types
- Worried Resident: wants protection from strange noises or bad luck.
- Shop Owner: needs a charm to steady business after a supernatural disturbance.
- Shrine Visitor: requests cleansing after encountering a spirit at night.

## Starter Spirit Type
- Restless Wisp: a simple spirit that drifts toward the player and can be purified with the basic action.

## Design Boundaries
- Keep maps small.
- Use placeholder art where needed.
- Build the first playable loop before adding polish.
- Avoid complex branching story.
- Avoid large inventories, many enemy variants, advanced crafting, or deep RPG progression in the MVP.

## Dialogue and guidance authoring boundary (2026-10-09)

The next pass adds linear dialogue and presentation to existing DesignLab segments. The author will supply all dialogue and guidance wording. Five empty slots cover the opening, the first seal approach and completion, the route's upper approach, and the cloud approach. Technical preparation supplies safe grounded triggers, readable dialogue controls, a short panel fade, and an optional target highlight. Empty slots remain silent; this pass adds no story branches, lore, enemies or maps. Narrative pacing and the final player's experience await authored content and playtesting.


## 2026-10-20 — 打爆南瓜 H5 番外

独立浏览器排球小游戏：右侧小狐狸对战左侧 easy_ghost 小幽灵 AI；每局先得3分，连续赢3局解锁南瓜炸弹倒计时、糖果爆炸与原样文案 happy hallow day。三局难度递进，输局连胜归零。手机仅左右两键，自动起跳与挥击；电脑ADWS移动、空格攻击。支持中日英与万圣节日期倒计时。此番外不改变Unity主循环、剧情或存档。


### 2026-10-20｜打爆南瓜手机体验补充

- 手机横向满屏，只用「左」「右」选位和接球。发球前允许选择站位，松开后自动发球；实际命中才挥击。每局结束由玩家确认继续，三连胜展示「南瓜爆破王」称号与南瓜爆炸、糖果雨结算。
