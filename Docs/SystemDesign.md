# System Design

## Project Structure
The project follows a small Unity 2D layout:

```text
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
```

## Scene Flow
The MVP should use a simple scene flow:

1. Shop/Shrine Scene
2. Night Map Scene
3. Result Screen

Current prototype flow is being built in small pieces:

1. `CafeInterior_Temporary`
2. Return to `HubMap_Day`
3. Select `NightPatrolIcon_夜の巡回へ`
4. Open `NightStageSelectPanel`
5. Launch a Night ACT stage from `NightStageSelectPanel`: node 1 -> `Stage_0_0`, node 2 -> `Stage_1_1`, node 3 -> `Stage_1_2`
6. Pause Return to Map or Stage Clear Continue
7. Return to `HubMap_Day`
8. Resource/repair/cafe preparation later

Starter placeholder scene files:

- `Assets/Scenes/ShopShrine.unity`
- `Assets/Scenes/NightMap.unity`
- `Assets/Scenes/Result.unity`

Scene transitions can begin as direct button or trigger-driven changes. A more advanced calendar, clock, or scheduler should wait until the prototype proves the loop.

## Core Systems

### Player
- Handles 2D movement.
- Owns the basic purification action.
- Uses inspector-tuned values for speed, action range, and cooldown.
- Uses a small visual controller for prototype sprite states instead of a full animation graph.

### Prototype Retry
- A small GameManager pauses player control when the player enters the FallZone.
- The temporary Retry UI resets the player to StartPoint and then restores movement.
- This should stay lightweight until checkpoints or lives are actually needed.

### Combat/Purification
- One player action affects one enemy/spirit type.
- The first version can use simple collision or trigger checks.
- Damage, purification strength, or cooldown should stay easy to tune.
- The first combat tutorial uses `J` to spawn a short-lived attack hitbox in front of the player.
- Player attack remains a single-button basic purification action on `J`. It now supports a lightweight 3-hit combo with a forgiving input buffer, a short combo reset window, and per-step tuning for hitbox size, offset, active time, cooldown, visual duration, damage, and effect travel.
- The third combo hit is slightly stronger and larger for readability, but this is still basic ACT feel tuning rather than a skill tree or blue-meter system.
- Combat Feedback V1 keeps the existing combo and does not add dash or rewrite player movement. The focus is immediate attack response, hit flashes, knockback, short hit pause, and readable break/destruction feedback.
- Current combo tuning keeps the first two hits quick and easy to chain, while the third hit has a little more reach, visual scale, and recovery so it reads as a finishing beat instead of a new skill.
- Combo-step feedback is lightweight: attack motes vary by step, the third hit has a small camera accent, and `GameAudio` reuses the existing attack clip with subtle per-step volume emphasis.
- If no attack sprite is assigned, the attack creates a small runtime placeholder purification slash so the player always gets visible attack feedback.
- Attack startup creates a few lightweight motes in the attack direction. These are visual feedback only and do not extend the damage window.
- Attack hitboxes call `GhostHealth.TakeDamage` once per Ghost per swing, then disable their collider before the visual fades out.
- Ghost enemies use simple hovering movement and `GhostHealth` for HP, Faith Point rewards, optional StarSeal drops, hit flash, small knockback, and a short vanish/fade death effect.
- The current Stage_1_1 combat feedback pass keeps the existing enemy system but makes route-blocking enemies easier to read: hits flash white, apply small knockback, briefly pause movement or enter hit stun, and deaths now float/fade out while vanish motes and SFX play.
- StarSeal drops from SealGhost enemies remain simple pickup objects, but they now render in front of gameplay elements and spawn a small pickup-drop mote burst so the reward is easier to notice.
- Combat feedback uses temporary runtime mote effects for Ghost hits, Ghost vanish moments, and player hurt flashes. These are lightweight placeholder visuals, not a final particle/VFX pipeline.
- A small runtime `CameraShake` component can be auto-attached to the active camera for very subtle hit, vanish, and hurt shakes. It clears its previous offset before camera follow updates, then applies a tiny offset afterward so it does not replace the existing camera-follow behavior.
- Ghost hits may apply a very short hit stop for readability. This should stay tiny and should not become combo or blue-meter logic yet.
- Ghost knockback now moves the enemy body a visible short distance away from the attack source, so each hit has a clearer physical result before the enemy resumes patrol/chase.
- Player damage uses `PlayerHealth` with short invincibility frames, a red hit flash, small knockback, blinking during invincibility, and the existing three-heart UI.
- Normal Ghost contact damage should remain beginner-friendly and relies on both player invincibility and a short per-Ghost contact cooldown to prevent repeated frame-by-frame HP loss.
- State-machine enemies now use a tiny attack telegraph: when the player enters attack range, the enemy briefly tints to its warning color and pauses before damage resolves. If the player leaves the commit range during that warning, the attack misses.
- Enemy pursuit has a lightweight pressure pass: state-machine enemies briefly remember the player's last seen position, keep chasing a little beyond the first detection moment, tint warmer while chasing, speed up when close, and make a short horizontal lunge during the attack warning. This is still MVP tuning, not a pathfinding or advanced AI system.
- Ghost visuals use the transparent `easy_ghost` variant while keeping the original source image unchanged.
- `GhostEnemy` now supports a lightweight state-machine mode with Idle, Patrol, Chase, Attack, Hit, and Dead states. It uses serialized `detectRange`, `attackRange`, `attackCooldown`, `attackPauseDuration`, and `hitStunDuration` values so enemies can detect the player, chase, pause for contact attacks, react to hits, and die cleanly without rewriting player movement.
- The base `GhostEnemy` prefab and the existing Stage_1_1 SealGhost enemies enable this state-machine tuning. Contact damage remains beginner-friendly through player invincibility plus per-enemy attack cooldown, so HP should not drain every frame.
- Stage_1_1 combat pacing should treat enemies as light route obstacles: normal Ghosts interrupt movement in safe spaces, while SealGhosts guard key route/reward moments and drop StarSeals through combat instead of placing StarSeals as normal floating collectibles.
- Stage 1-2 introduces Paper Doll and Ghost Lantern as small enemies by reusing `GhostEnemy`, `GhostHealth` damage/reward/feedback, and `SpriteFrameAnimator` 4-frame visual loops.
- Paper Doll is currently a 2 HP lightweight near-ground patrol enemy. It uses a compact `0.225` scene scale with simple colliders so it reads clearly while staying below player scale, plus very small hover sway, shorter contact range, and a short patrol/chase leash so it blocks routes without drifting into gaps. Ghost Lantern is currently a 3 HP sturdier low-floating patrol/chase enemy with a `0.18` low-hover scale so its apparent height reads close to the Paper Doll despite the larger source art, plus calmer bobbing, smaller contact range, and a slightly wider but still beginner-safe detect range. Both use the same detect/chase/attack/hit/death state-machine fields as Stage 1-1, grant Faith Points only, and do not drop StarSeals, shards, yokai materials, or boss materials.
- Stage 1-2 placed enemies also use the attack telegraph timing directly in the scene: Paper Dolls use a shorter warning, while Ghost Lanterns and copied disabled SealGhosts keep a slightly longer warning and cooldown.

### Reward Hierarchy
- Faith Points are the basic currency and the first reward type used by small enemies.
- Small Ghost enemies should grant Faith Points directly on defeat.
- StarSeals are key-style stage collectibles for tutorial and level goals. They are count-only in the prototype and should not become currency or crafting material.
- `GameManager` can hide the StarSeal HUD per scene for stages that do not use StarSeal objectives, such as the current Stage 1-2 blockout.
- Hearts are temporary recovery pickups placed on optional reward platforms or hidden routes.
- Hearts restore 1 HP up to the player's max HP and disappear when collected.
- Shards and yokai materials are reserved for stronger enemies, boss spirits, or later progression systems.
- Blue energy, ultimate-style actions, advanced combo upgrades, and skill-tree systems are planned for later and are not part of the current prototype.

### Resource Inventory
- `ResourceInventory` is a lightweight resource store, not a full RPG inventory.
- It stores Faith Points as the basic currency and can store future material counts by string id, starting with `BasicYokaiMaterial`.
- It also stores `HeartFox` / `こころ狐`, a gratitude resource earned from cafe visitors when they are served a liked menu.
- Current ACT rewards should add Faith Points through the existing `GameManager.AddFaithPoints` entry point, which forwards to `ResourceInventory` and refreshes the UI.
- `ResourceInventory.FaithPoints` is the single stored source of truth; `GameManager` does not keep an independent Faith Points counter.
- Hearts remain temporary stage pickups: they heal the player immediately and are not stored.
- `HeartFox` is not money and is not a second Faith Points system. It is a small sign of gratitude used by shrine/cafe progression.
- Small Ghost enemies grant Faith Points only. Yokai materials, charm fragments, shards, and boss rewards are reserved for stronger enemies or later stages.
- Future cafe systems can read `ResourceInventory.Instance` or call its methods directly when spending Faith Points or checking material counts.

### Day HubMap
- `HubMap_Day` is the temporary daytime hub after the first night ACT stage.
- Phase 1 is a small playable scene skeleton: grass map background, cleaned ruined shrine icon, cleaned warehouse icon, a movable RPG player placeholder with simple four-direction sprite switching, and organized placeholder groups.
- Phase 2 adds lightweight click interaction panels for the ruined shrine and warehouse.
- The warehouse panel reads Faith Points and `BasicYokaiMaterial` from `ResourceInventory`; Hearts are not stored and should not appear there.
- Phase 4 adds the first minimal shrine repair action: the ruined shrine can spend 10 Faith Points from `ResourceInventory` and switch to a repaired cafe state.
- Cafe construction is a one-time unlock stored with a small `PlayerPrefs` flag. Returning to the hub or restarting the prototype does not charge the player again.
- This is still not a full construction system. There is no upgrade tree, build queue, repair animation, or general-purpose building-save system yet.
- Stage Clear now includes a Continue button that loads `HubMap_Day`, keeping the first Night ACT to Day Hub flow testable.
- Phase 5 adds `CafeInterior_Temporary`, a lightweight cafe interior scene using the temporary cafe background and the same four-direction RPG player movement.
- After the shrine is repaired, the shrine action button can load `CafeInterior_Temporary`; walking out through the cafe's lower-center entrance returns naturally to `HubMap_Day`.
- The cafe now has placeholder interactions for the fox altar and front counter. The fox altar shows shrine-status and placeholder upgrade information, while the counter shows the current visitor list without guest AI.
- The four reception placeholders correspond to the physical cafe-chair anchors named `GuestSeat_01` through `GuestSeat_04`. These anchors are reserved for later guest spawning, messages, affection, and reception state.
- Cafe operation Phase 1 adds a small `CafeOperationController` data and serving layer without NPC pathfinding or a full management system.
- The cafe still has four physical visitor seats, and `CafeOperationController` now uses lightweight visitor data instead of a fixed four-guest list. Each visitor has `visitorId`, `displayName`, `visitorType`, `favoriteMenus`, temporary `affection`, `messageList`, `weight`, `canGiveHeartFox`, and a separate visual id for existing sprite mappings.
- Visitor types are `Living`, `Spirit`, `Yokai`, and `Special`. The counter UI shows display name, type, affection, current request, and favorite menu summary for each current visitor.
- Current early random visitor pool: `elder_woman_worshipper` (Living, weight 30), `foreign_backpacker` (Living, weight 26), `nekomata_orange_cat` (Yokai, weight 24), `small_ghost` (Spirit, weight 22), `tanuki_yokai` (Yokai, weight 18), `kappa_yokai` (Yokai, weight 16), plus lower-weight gentle living visitors already available in the prototype.
- `black_priest` exists in the data catalog as `Special`, but `specialVisitorsUnlocked` is false by default, so he does not appear in the normal early random pool. He is reserved for a later Red Oni menu unlock.
- Random visitor refresh uses weighted selection without replacement, up to four current visitors for `GuestSeat_01` through `GuestSeat_04`.
- Current visitor visual mapping keeps data ids separate from art ids: `elder_woman_worshipper` uses `worshipper`, `foreign_backpacker` uses `traveler`, `nekomata_orange_cat` uses `small_yokai`, `small_ghost` uses `child_girl_kimono`, `tanuki_yokai` uses `tanuki_yokai`, and `kappa_yokai` uses `kappa_yokai`.
- Cafe visitor sprites follow the `{guestId}_{direction}_{state}` convention for `front`, `back`, `left`, and `right`, with `idle`, `walk_01`, and `walk_02` states. If a sprite is missing, the runtime logs a warning and falls back to an available direction rather than crashing.
- The first menu contains `稲荷コーヒー`, `狐火ラテ`, and `夜桜ケーキ`. Serving the requested item grants its small Faith Point reward through `ResourceInventory` and updates that visitor's latest temporary message.
- Serving a visitor's liked menu increases temporary affection by 1 and adds 1 `HeartFox` / `こころ狐` through `ResourceInventory`.
- Cafe operation Phase 2 adds a selectable front-counter panel with four guest buttons, three menu buttons, a `Serve` action, current Faith Points, and a small latest-message board.
- Phase 2 also adds a lightweight visual arrival sequence: the four guests appear at the cafe's lower doorway, follow a fixed two-segment route to the counter, and settle at their matching `GuestSeat_01` through `GuestSeat_04` anchors. This is a scripted presentation only, not NPC pathfinding.
- Cafe Visual Loop V2 keeps the same scripted seat system and adds two lightweight presentation links: selecting a guest in the counter UI highlights the matching visible seated guest, and guest arrival/leaving updates the counter feedback text with `来訪者がやって来ました。` or `来訪者は静かに席を立ちました。`
- Runtime guest sprites use cleaned transparent derivatives generated from the preserved `guest_*` source art. The counter panel uses front-idle portraits while the cafe floor uses directional walk sprites. If a scene-bound visitor visual is missing a direction or walk frame, the cafe guest controller tries to fill it from the matching `Assets/Art/cafe_icon/guest_*` files in the editor, then logs a warning and uses a safe fallback if the sprite is still missing.
- The first three menu buttons use cleaned transparent runtime derivatives from `Assets/Art/cafe_icon/menu_icon`. The additional sakura soft-drink art is retained for later menu expansion but is not exposed in the current three-item MVP.
- Visitor affection is now lightly persisted per `visitorId` with PlayerPrefs for prototype testing. Current visitor seats, active orders, recent messages, autonomous guest AI, offline income, and a full cafe-management system remain deferred.
- The fox altar can currently consume `HeartFox` for a lightweight placeholder upgrade path: Lv.1 -> Lv.2 costs 3 `HeartFox`, Lv.2 -> Lv.3 costs 5 `HeartFox`, and Lv.3 -> Lv.4 costs 8 `HeartFox`. Higher levels are locked as future content. On upgrade it shows `狐の祠が少しあたたかくなりました。`; if the player lacks `HeartFox`, it shows `こころ狐が足りません。`
- Fox altar furniture unlocks are placeholder IDs only and are stored as simple PlayerPrefs flags for future systems. Lv.1 starts with `furniture_fox_icon` and `furniture_fox_altar_base`; Lv.2 unlocks `furniture_small_flower_table`; Lv.3 unlocks `furniture_sofa_double_up`, `furniture_sofa_double_down`, `furniture_sofa_double_left`, and `furniture_sofa_double_right`; Lv.4 unlocks `furniture_shrine_lamp` and `furniture_torii_small`.
- The fox altar panel lists the current unlocked furniture, the next unlock preview, and a small unlocked-furniture preview strip. The preview strip can show known sprites such as the fox icon and directional sofa art in the Unity Editor, then safely falls back to text cards when art is not assigned yet. This is display/preview support only: the prototype does not implement free furniture placement, drag-and-drop, or a furniture editor yet.
- Cafe operation Phase 3 is the current handoff point: the visible guest-arrival presentation, front-counter Serve interaction, temporary affection, latest messages, menu icons, and `ResourceInventory` Faith Point rewards form one manually testable cafe loop.
- Cafe guests now wait outside until the player opens the front-counter panel and presses `開業`. The one-time `営業中` state starts the existing four-guest doorway arrival sequence and prevents serving before the cafe has opened.
- The minimal order and ingredient loop gives each occupied seat one random requested menu item from the first three dishes. Serving validates the selected dish, checks and consumes `CoffeeBean`, `Milk`, `Sugar`, or `Flour` from `ResourceInventory`, grants Faith Points through `ResourceInventory`, increases temporary affection only when the menu is liked, and moves that visitor through `注文待ち -> 留言中 -> 帰り支度中 -> 空席`.
- Liked-menu checks use stable internal menu IDs (`inari_coffee`, `kitsunebi_latte`, `yozakura_cake`) while the UI still displays the Japanese menu names. If ingredients are missing, service is blocked with `材料が足りません。`.
- Served guests leave one short message, wait briefly, walk back toward the cafe doorway, disappear, and clear their seat. If the cafe is open, that seat now refills with a new weighted random visitor after a short delay, excluding the just-departed visitor when another candidate exists. The message board keeps a small recent-message history after guests leave.
- Seat refill is still a simple prototype loop, not full guest pathfinding, waiting lines, table turnover pacing, or table-turnover scheduling.
- Cafe Day Result V1 tracks only the current cafe-session gains: served visitor count, gained Faith Points, gained HeartFox, affection increase count, and furniture unlocked during that session.
- Exiting `CafeInterior_Temporary` now shows `今日のカフェ記録` before returning to `HubMap_Day`. Closing that panel resets only the session counters; total Faith Points, total HeartFox, visitor affection, fox altar level, and furniture unlock flags remain stored separately.
- Cafe ingredients are stored as lightweight `ResourceInventory` material counts: `CoffeeBean`, `Milk`, `Sugar`, and `Flour`. The temporary `仕入れ商店` panel purchases one ingredient at a time by spending the existing stored Faith Points. The first hub or cafe visit grants two of each ingredient as trial-opening stock.
- `ResourceInventory` exposes the cafe-facing methods `AddIngredient`, `SpendIngredient`, `GetIngredientCount`, and `HasIngredient` while retaining its generic material storage for later crafting rewards. Faith Points remain stored only in `ResourceInventory`.
- The ingredient shop is a lightweight HubMap interaction point rather than a cafe-interior panel. `HubIngredientShopController` places the cleaned rabbit-and-jar store marker in the HubMap upper-right clearing and opens the temporary `仕入れ商店` panel when clicked.
- The HubMap shop panel uses cleaned transparent runtime derivatives for its rabbit merchant portrait and four ingredient icons. There is no separate shop scene, stock limit, price fluctuation, item quality, or full inventory grid.
- CafeInterior_Temporary only consumes the shared ingredients during serving. It no longer contains a purchase button or ingredient shop panel.
- This is still not a full cafe-management system. There is no inventory grid, supplier simulation, offline income, guest pathfinding, or boss menu content.
- Emotional direction for the cafe guest system: `夜神社カフェ` is not just a normal cafe management loop. It is a quiet boundary refuge between the living, the dead, yokai, and special visitors; a small warm light where those who have lost their way can sit down briefly.
- Core sentence: "夜神社カフェ is a place that briefly catches those who have lost their way."
- Internal Chinese theme: "让走失的人与灵，能被暂时接住一下。"
- Japanese tone guide: "迷い込んだ来訪者が、少しだけ息をつける場所。"
- Guest terminology should prefer `来訪者` or `今日の来訪者` over only `客` or `お客様`, because cafe visitors are not only customers. They are people, spirits, yokai, or unusual presences drawn by the shrine light.
- Guest messages should stay short, restrained, warm, and atmospheric. Avoid overly direct tragedy, horror phrasing, or heavy explanation. The cafe does not solve every visitor's life; it offers a light, a warm drink, a seat, a short conversation, and sometimes a small message left behind.
- Faith Points, serving rewards, ingredients, orders, and affection remain gameplay systems, but guest messages should carry emotional weight instead of becoming generic shop reviews.
- Current visitor messages are lightweight text only. Serving stores the latest short visitor message in the cafe operation state and the temporary message board can show recent served-visitor messages. This is not a dialogue tree or full conversation system yet.
- When a liked menu grants `HeartFox`, the front-counter UI shows `こころ狐を受け取りました。` and briefly displays the HeartFox icon if `item_heart_fox_icon.png` is assigned or available. If the icon is missing, the UI uses a simple `狐` placeholder and logs one warning.
- Future guest categories can remain simple: `Living`, `Spirit`, `Yokai`, and `Special`. These categories may later affect favorite menus, message tone, affection events, Obon or summer festival appearances, and whether a visitor can be gently sent off or helped to move on.
- Initial visitor direction should stay gentle and grounded: an elder woman worshipper carrying memories of someone important, a foreign backpacker lost in an unfamiliar place, an orange nekomata-like boundary visitor who remembers a former home and warm cafe smells, and a small ghost who reads as soft and lonely rather than frightening.
- The black priest / `黒衣の司祭` should not be treated as a normal initial guest. He should appear later as a `Special` visitor after the Red Oni menu is unlocked, with a more dangerous and mysterious tone.
- Example message tone: "ここは、少しだけ息がしやすいですね。", "言葉は分からなくても、温かさは分かります。", "この匂い、昔の家を思い出すにゃ。", "今日も、あの人に少し近づけた気がします。"
- `HubMap_Day` creates a visually distinct moonlit-pool `NightPatrolIcon_夜の巡回へ` entry point in the lower-right clearing. It now opens `NightStageSelectPanel` instead of directly loading an ACT scene.
- `NightStageSelectPanel` is a lightweight UI only, not a new world-map system. It uses `BG_level.png` as the temporary panel background, `level_finfish_icon.png` for available star nodes, and `level_icon.png` for locked or placeholder nodes. Node 1 loads `Stage_0_0`, node 2 loads `Stage_1_1`, node 3 loads the existing `Stage_1_2`, and node 4 remains a locked Boss placeholder.
- Full cafe management, customer requests, full inventory UI, and persistent building save should be added in later phases only.

### Future Shop Refresh Design
- Basic cafe ingredients should always remain available in the HubMap ingredient shop: `CoffeeBean`, `Milk`, `Sugar`, and `Flour`.
- Later-stage ingredients and boss materials should not be permanently available. Rare items can appear through a future shop-refresh pool.
- A natural refresh timing is when the player returns from a night ACT stage to `HubMap_Day`.
- This creates a fallback loop: farm small enemies for Faith Points, return to the daytime hub, inspect refreshed stock, and spend Faith Points when useful rare materials appear.
- Boss materials may appear with a low probability and a high Faith Point price.
- Shop refresh is a fallback and convenience system. It must not replace boss progression, guaranteed first-clear rewards, or the intended value of defeating stronger spirits.
- The current MVP does not implement shop refresh, random stock, rare-item probabilities, or boss-material purchasing yet.

### Combat Pause
- `Stage_1_1` includes a minimal `CombatPauseController`.
- Pressing `Esc` pauses the ACT stage and shows `Resume` and `Return to Map`.
- `Resume` restores gameplay immediately.
- `Return to Map` restores `Time.timeScale` to `1` and loads `HubMap_Day`.
- Retry and Stage Clear remain owned by the existing `GameManager`; the pause menu does not replace them.

### Platformer Setpieces
- Breakable blocks are simple attack targets that flash, shake, scale-punch briefly, and spawn stronger hit/break motes when damaged or destroyed.
- Stage_0_0 spawns a small runtime `CombatPracticeBreakables_Runtime` group near the start area. These practice targets are themed as corrupted yokai residue rather than normal shrine furniture: ghost lantern, cursed offering pile, broken talisman bundle, and corrupted crate flame. The setup uses cleaned `Assets/Art/Tools_icon/prop_*_cutout.png` sprites first, then falls back to the runtime prototype square if needed.
- The first targets are practice-only, while later targets can drop a small FaithPoint pickup or Heart pickup, giving the player a safe place to test the `J` attack before enemies.
- Spike hazards deal 1 damage through PlayerHealth and rely on existing invincibility to prevent instant HP loss.
- One-way platforms use a minimal collision helper so the player can jump through from below and land from above.
- Triggered Ghost spawners are used for small tutorial ambushes without adding a new enemy AI system.

### Prototype Audio
- `GameAudio` is the lightweight scene audio component for the action prototype.
- Stage scenes can assign one looping BGM clip and one-shot SFX clips through serialized fields.
- BGM should stay low, around 0.15 to 0.25 volume.
- SFX should use `PlayOneShot` and remain null-safe so missing clips do not break gameplay.
- Current hooks cover jump, landing, attack, player hurt, pickups, Ghost vanish, Retry fall, Stage Clear, and spike hazards.
- Current combo audio feedback is temporary plumbing only: it reuses the assigned attack clip with slight volume variation and a short runtime playback limit so attack clips stay readable without trailing across attacks.
- Stage_0_0, Stage_1_1, and Stage_1_2 currently use `鈴を鳴らす.mp3` as the temporary player attack clip.
- `NightStageSelectPanel` is a full-screen HubMap overlay. Opening it pauses the HubMap BGM and plays `Lotus Lantern Menu.mp3`; closing it stops the menu BGM and restores the HubMap BGM, while launching a stage stops the menu BGM before the scene load.
- The night stage select menu BGM uses its own child AudioSource so it does not share playback state with hover/ignite UI SFX. The menu restarts this BGM on each open, then pauses the HubMap BGM.
- Stage select nodes use invisible button hit areas with small hover scale feedback and a runtime halo sprite for readable selection feedback. Nodes 3 and 4 are placed in the center-right water space to follow the current visual mockup and avoid crowding the edge of the background.
- `NightStageSelectPanel` uses cleaned transparent available/locked node icons with invisible button hit areas and a lightweight `LevelMenuAudioController` for null-safe UI SFX: a low-volume fixed-pitch wind-chime cue capped to the first 4 seconds on node hover and a short lantern-ignite cue before loading an available stage.

### Shop/Requests
- Stores a small list of three request types.
- Tracks needed item type and reward values.
- Completes requests through simple UI interactions.

### Items
- Three item types are enough for the MVP.
- Data can start as simple serializable classes or ScriptableObjects.
- Avoid complex inventory rules until the loop is playable.

### Results
- Shows completed requests, spirits purified, materials gained, and faith points gained.
- Provides a clear way to return to the shop/shrine scene.

### Save/Load
- Optional for the first pass.
- Current prototype persistence uses small PlayerPrefs keys rather than a large save framework.
- Persisted now: Faith Points, current lightweight resource/material counts including `HeartFox`, cafe starter stock initialization, fox altar level, placeholder furniture unlock IDs, shrine repair state, and visitor affection values by `visitorId`.
- Still placeholder/not fully saved: current visitor seats, active cafe orders, recent message board state, guest arrival positions, black_priest unlock conditions, and any future furniture placement layout.
- A later production save system can replace these PlayerPrefs keys with a small JSON save once the loop stabilizes.

## UI Flow
- Shop UI: current requests, available items, start night button.
- Night UI: player health/status if needed, material count if useful.
- Result UI: rewards and return button.

## Data Notes
- Keep gameplay values exposed through serialized fields or ScriptableObjects.
- Prefer small, readable components over large manager scripts.
- Keep systems independent enough that shop, player, combat, and UI can be adjusted separately.

## Initial Implementation Order
1. Create scenes and placeholder objects.
2. Add player movement.
3. Add basic purification action.
4. Add one spirit behavior.
5. Add shop requests and item data.
6. Add result screen.
7. Add minimal save/load only if the loop is stable.
