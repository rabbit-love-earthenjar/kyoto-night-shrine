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
5. Launch the existing `Stage_1_1` Night ACT stage from the Stage 1-1 entry
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
- Combo-step feedback is lightweight: attack motes vary by step, the third hit has a small camera accent, and `GameAudio` reuses the existing attack clip with subtle per-step volume emphasis.
- If no attack sprite is assigned, the attack creates a small runtime placeholder purification slash so the player always gets visible attack feedback.
- Attack startup creates a few lightweight motes in the attack direction. These are visual feedback only and do not extend the damage window.
- Attack hitboxes call `GhostHealth.TakeDamage` once per Ghost per swing, then disable their collider before the visual fades out.
- Ghost enemies use simple hovering movement and `GhostHealth` for HP, Faith Point rewards, optional StarSeal drops, hit flash, small knockback, and a short vanish/fade death effect.
- Combat feedback uses temporary runtime mote effects for Ghost hits, Ghost vanish moments, and player hurt flashes. These are lightweight placeholder visuals, not a final particle/VFX pipeline.
- A small runtime `CameraShake` component can be auto-attached to the active camera for very subtle hit, vanish, and hurt shakes. It clears its previous offset before camera follow updates, then applies a tiny offset afterward so it does not replace the existing camera-follow behavior.
- Ghost hits may apply a very short hit stop for readability. This should stay tiny and should not become combo or blue-meter logic yet.
- Player damage uses `PlayerHealth` with short invincibility frames, a red hit flash, small knockback, blinking during invincibility, and the existing three-heart UI.
- Normal Ghost contact damage should remain beginner-friendly and relies on both player invincibility and a short per-Ghost contact cooldown to prevent repeated frame-by-frame HP loss.
- Ghost visuals use the transparent `easy_ghost` variant while keeping the original source image unchanged.
- `GhostEnemy` now supports a lightweight state-machine mode with Idle, Patrol, Chase, Attack, Hit, and Dead states. It uses serialized `detectRange`, `attackRange`, `attackCooldown`, `attackPauseDuration`, and `hitStunDuration` values so enemies can detect the player, chase, pause for contact attacks, react to hits, and die cleanly without rewriting player movement.
- The base `GhostEnemy` prefab and the existing Stage_1_1 SealGhost enemies enable this state-machine tuning. Contact damage remains beginner-friendly through player invincibility plus per-enemy attack cooldown, so HP should not drain every frame.
- Stage 1-2 introduces Paper Doll and Ghost Lantern as small enemies by reusing `GhostEnemy`, `GhostHealth` damage/reward/feedback, and `SpriteFrameAnimator` 4-frame visual loops.
- Paper Doll is currently a 1 HP lightweight near-ground patrol enemy. Ghost Lantern is currently a 2 HP sturdier low-floating patrol/chase enemy. Both use the same detect/chase/attack/hit/death state-machine fields as Stage 1-1, grant Faith Points only, and do not drop StarSeals, shards, yokai materials, or boss materials.

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
- Current ACT rewards should add Faith Points through the existing `GameManager.AddFaithPoints` entry point, which forwards to `ResourceInventory` and refreshes the UI.
- `ResourceInventory.FaithPoints` is the single stored source of truth; `GameManager` does not keep an independent Faith Points counter.
- Hearts remain temporary stage pickups: they heal the player immediately and are not stored.
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
- The cafe now has placeholder interactions for the fox altar and front counter. The fox altar shows its Lv.1 shrine-status panel, while the counter shows four initial guest slots without guest AI.
- The four reception placeholders correspond to the physical cafe-chair anchors named `GuestSeat_01` through `GuestSeat_04`. These anchors are reserved for later guest spawning, messages, affection, and reception state.
- Cafe operation Phase 1 adds a small `CafeOperationController` data and serving layer without NPC pathfinding or a full management system.
- The initial cafe guests are `参拝客`, `旅人`, `小さな妖怪`, and `不思議な常連`, mapped in order to `GuestSeat_01` through `GuestSeat_04`.
- Current guest visual mapping: `参拝客` uses `guest_gramma`, `旅人` uses `guest_traveler`, `小さな妖怪` uses `guest_nekomata`, and `不思議な常連` uses `guest_priest`.
- The first menu contains `稲荷コーヒー`, `狐火ラテ`, and `夜桜ケーキ`. Serving one item grants its small Faith Point reward through `ResourceInventory`, increases the selected guest's temporary affection by 1, and updates that guest's latest temporary message.
- Cafe operation Phase 2 adds a selectable front-counter panel with four guest buttons, three menu buttons, a `Serve` action, current Faith Points, and a small latest-message board.
- Phase 2 also adds a lightweight visual arrival sequence: the four guests appear at the cafe's lower doorway, follow a fixed two-segment route to the counter, and settle at their matching `GuestSeat_01` through `GuestSeat_04` anchors. This is a scripted presentation only, not NPC pathfinding.
- Runtime guest sprites use cleaned transparent derivatives generated from the preserved `guest_*` source art. The counter panel uses front-idle portraits while the cafe floor uses back-facing walk and idle sprites.
- The first three menu buttons use cleaned transparent runtime derivatives from `Assets/Art/cafe_icon/menu_icon`. The additional sakura soft-drink art is retained for later menu expansion but is not exposed in the current three-item MVP.
- Guest affection and messages are in-scene prototype state only. Persistent guest progression, autonomous guest AI, offline income, material costs, and a full cafe-management system remain deferred.
- Cafe operation Phase 3 is the current handoff point: the visible guest-arrival presentation, front-counter Serve interaction, temporary affection, latest messages, menu icons, and `ResourceInventory` Faith Point rewards form one manually testable cafe loop.
- Cafe guests now wait outside until the player opens the front-counter panel and presses `開業`. The one-time `営業中` state starts the existing four-guest doorway arrival sequence and prevents serving before the cafe has opened.
- The minimal order and ingredient loop gives each occupied seat one random requested menu item from the first three dishes. Serving validates the selected dish, consumes its ingredients, grants Faith Points through `ResourceInventory`, increases temporary affection, updates the guest message, and rolls the next request.
- Cafe ingredients are stored as lightweight `ResourceInventory` material counts: `CoffeeBean`, `Milk`, `Sugar`, and `Flour`. The temporary `仕入れ商店` panel purchases one ingredient at a time by spending the existing stored Faith Points. The first hub or cafe visit grants two of each ingredient as trial-opening stock.
- `ResourceInventory` exposes the cafe-facing methods `AddIngredient`, `SpendIngredient`, `GetIngredientCount`, and `HasIngredient` while retaining its generic material storage for later crafting rewards. Faith Points remain stored only in `ResourceInventory`.
- The ingredient shop is a lightweight HubMap interaction point rather than a cafe-interior panel. `HubIngredientShopController` places the cleaned rabbit-and-jar store marker in the HubMap upper-right clearing and opens the temporary `仕入れ商店` panel when clicked.
- The HubMap shop panel uses cleaned transparent runtime derivatives for its rabbit merchant portrait and four ingredient icons. There is no separate shop scene, stock limit, price fluctuation, item quality, or full inventory grid.
- CafeInterior_Temporary only consumes the shared ingredients during serving. It no longer contains a purchase button or ingredient shop panel.
- This is still not a full cafe-management system. There is no inventory grid, supplier simulation, offline income, guest pathfinding, or boss menu content.
- `HubMap_Day` creates a visually distinct moonlit-pool `NightPatrolIcon_夜の巡回へ` entry point in the lower-right clearing. It now opens `NightStageSelectPanel` instead of directly loading an ACT scene.
- `NightStageSelectPanel` is a lightweight UI only, not a new world-map system. It uses `BG_level.png` as the temporary panel background, `level_finfish_icon.png` for available star nodes, and `level_icon.png` for locked or placeholder nodes. Node 1 loads `Stage_1_1`, node 2 loads the existing `Stage_1_2`, and nodes 3 and 4 remain locked placeholders for Stage 1-3 `灯籠流し` and Boss `赤鬼`.
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
- Breakable blocks are simple attack targets that grant small rewards immediately when destroyed.
- Spike hazards deal 1 damage through PlayerHealth and rely on existing invincibility to prevent instant HP loss.
- One-way platforms use a minimal collision helper so the player can jump through from below and land from above.
- Triggered Ghost spawners are used for small tutorial ambushes without adding a new enemy AI system.

### Prototype Audio
- `GameAudio` is the lightweight scene audio component for the action prototype.
- Stage scenes can assign one looping BGM clip and one-shot SFX clips through serialized fields.
- BGM should stay low, around 0.15 to 0.25 volume.
- SFX should use `PlayOneShot` and remain null-safe so missing clips do not break gameplay.
- Current hooks cover jump, landing, attack, player hurt, pickups, Ghost vanish, Retry fall, Stage Clear, and spike hazards.
- Current combo audio feedback is temporary plumbing only: it reuses the assigned attack clip with slight volume variation and does not define final SFX direction.

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
- If included, save only minimal progress: faith points, materials, and basic unlocked state.
- Use Unity-friendly local persistence such as PlayerPrefs or a small JSON file.

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
