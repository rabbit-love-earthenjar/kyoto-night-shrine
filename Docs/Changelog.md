# Changelog

## 2026-08-14
- Added three lightweight animated ranged runners to the isolated `Stage_1_Route_Prototype` V26. They patrol wide road sections, approach or retreat to maintain distance, show a short attack tint, and fire simple spirit shots without changing the existing player controller or formal `Stage_1_1`.
- Extended `GhostHealth` hit/death integration to pause, stun, and knock back ranged runners while preserving the existing FaithPoint-only small-enemy reward behavior.
- Persisted ranged patrol bounds in the generated scene and added validation that rejects zero-width routes. Unity editor validation passed, and automated Play Mode diagnostics confirmed a 1.90-unit reposition span plus two fired shots while the existing Red Oni visual patrol remained active.
- Extended the isolated `Stage_1_Route_Prototype` to V24 with two optional high reward branches above the existing readable main route. Each branch rejoins the main route and preserves the lower danger/return route.
- Added six reachable FaithPoint guide pickups and two flying guards to the high branches without changing `Stage_1_1` or the isolated Red Oni Boss scene.
- Expanded route validation to cover all new platform links, pickup components, enemy behavior, camera limits, Retry coverage, and EndGate continuity. Unity batch validation and an automated Play Mode presentation/patrol capture completed without C# errors.

## 2026-08-13
- Replaced Phase 2's flat platform approach with an Inspector-tunable parabolic leap, short launch hold, white-gray takeoff/impact smoke, camera shake, and HP-driven tempo escalation so platform smashes read as Boss attacks rather than position changes.
- Strengthened Phase 2 rhythm with two-hit smash phrases, a short pressure-scaled beat gap, and a heavier final impact while preserving the existing trajectory and platform lifecycle.

## 2026-08-12
- Extended the isolated Red Oni encounter into Phase 2 without changing `Stage_1_1`: Boss HP is now 60, Phase 2 begins at 40 HP, and the temporary stage result opens at 20 HP.
- Added a short `PHASE 2` transition, a distinct orange-red HP fill, and a persistent phase/HP label so the longer health bar and phase change are visually readable.
- Changed Phase 1 pacing so its established high/middle/low attacks accelerate continuously as Red Oni HP moves from 60 toward 40, instead of changing speed abruptly at the phase boundary.
- Replaced the temporary Phase 2 speed-up with a platform-smash loop: the Red Oni targets a nearby wooden ledge, pulses it as a warning, temporarily removes its collision and visual presence, then restores it before selecting another target.
- Separated the Red Oni phase animation flow after finding the Phase 1 and Phase 2 calls were reversed: Phase 1 once again uses its high/middle/low attacks, while Phase 2 now uses dedicated left/center/right smash states derived from `second_hit1/2/3` and returns to a separate Phase 2 idle.
- Phase 2 now moves the Red Oni visual to the selected platform's top attack point during the warning, plays the direction-matched smash there, breaks the platform on impact, and returns to the center rest point while the platform recovers.
- Updated Boss editor validation and automated Play Mode checks to verify one real Faith Bean hit, the 40 HP transition without premature Stage Clear, a complete warn/break/restore platform cycle, and the 20 HP temporary completion threshold.

## 2026-08-08
- Made each Faith Bean hit visibly readable on the Red Oni HP display: the fill now shrinks through its `RectTransform` instead of relying on a subtle filled-image update, flashes gold on impact, and briefly shows `-1` beside the current HP.
- Added Boss-scene-only Faith Bean aiming and shooting: mouse aim with left-click fire, plus `K` as a keyboard fallback. The existing movement, jump, and `J` melee attack remain unchanged.
- Added a lightweight Red Oni Phase 1 health model and top-center Boss HP bar. The prototype starts at 30 HP and completes Phase 1 at 20 HP after one third of the bar is removed.
- Added a moving projectile hit trigger that follows the Red Oni visual between high, middle, and low attack positions, along with a brief hit flash and small impact effect.
- Connected Retry completion to Boss HP and attack-state reset without rebuilding `Stage_1_Boss_RedOni` or changing `Stage_1_1`.
- Extended the automated Boss Play Mode check to fire a real Faith Bean, verify exactly one HP of damage, and retain the existing attack, one-way-platform, stable-height, and fall-recovery checks.
- Synchronized the Red Oni's visual position with its selected attack lane. During the warning, the background Boss now eases to a tunable high, middle, or low visual offset, holds that position through impact, and returns to idle afterward instead of swinging at one height while damaging another.
- Enlarged the Phase 1 Red Oni presentation by 12%, added a grounded anticipation pause to the middle attack, and added a soft blue-white cloud burst beneath the high attack so its vertical movement reads as a jump without moving gameplay collision.
- Corrected the Boss body grounding: middle and low attacks now share one background-ground foot line instead of sinking the whole Red Oni for a low swing. Only the high attack lifts the visual, and every attack returns to the grounded stance.
- Replaced the tinted high-attack cloud with neutral white-gray smoke and added matching low-attack smoke. High smoke travels upward and outward while low smoke travels downward and outward, keeping the two attack heights readable without moving gameplay collision.

## 2026-08-07
- Reworked the Red Oni attacks again after visual review: each height now uses a complete twelve-frame sliced sequence at 12 FPS instead of a four-frame sample. Fixed `320x280` cells, a shared bottom pivot, and a fixed sprite rectangle prevent weapon reach from changing the Boss scale between frames. Alternating rows play in left-to-right then right-to-left order to preserve the generated swing and return motion.
- Added three cleaned attack sheets for high, middle, and low swings. Neighboring-pose fragments and the opaque white source background are excluded before Unity slices the sheets.
- Replaced the temporary Red Oni walking fallback with real high, middle, and low club-swing frames derived from the regenerated source art. The Phase 1 Animator again shows real club attacks without neighboring-pose duplication.
- Rebuilt and validated the Boss animation assets in Unity. Automated Play Mode completed two attacks and retained lane warnings, upward one-way platform traversal, solid top landings, one-point fall recovery damage, and a stable `6.65`-unit Boss height.

## 2026-08-06
- Added one-point HP loss for ordinary falls in the Red Oni arena, while respecting the player's existing invincibility frames so a boss knockback cannot charge both hit damage and fall damage.
- Added an isolated Red Oni visual child that normalizes each animation frame to a consistent `6.65`-unit height without modifying source PNGs or `.meta` files. Attacks now enter their Animator states directly, preventing cross-fade overlap while making the Boss about 11% larger than the previous presentation.
- Switched all six Boss platforms to Unity `PlatformEffector2D` one-way surfaces: the player can jump upward through each wooden board, while its top surface remains solid and prevents downward fall-through.
- Resized the six wooden Boss platforms into a consistent oval-ring layout around the Red Oni. Elevated ledges remain stable one-way surfaces that allow upward passage but catch the player from above; a Boss-only recovery zone now returns falls to either lower safe platform before the normal Retry boundary.
- Replaced the Boss arena stone ledges with the existing transparent shrine-wood bridge art and widened the two lowest ledges into a continuous safety deck, so missed height changes fall back into the fight instead of immediately reaching the FallZone.
- Rebuilt the Boss arena's six ledges as a continuous one-way route: staggered left-side ascent, short upper crossing, and staggered right-side descent. Reduced each vertical step from 3.1 to 2.1 units so it fits the player's existing jump physics, and aligned the three Red Oni warning lanes with the actual standing heights.
- Created the isolated `Stage_1_Boss_RedOni` Phase 1 arena without changing `Stage_1_1`, using one existing shrine-night background and six fixed stone platforms across three heights.
- Added `RedOniPhaseOneController` with beginner-readable high/middle/low attack selection, a pulsing lane warning, delayed damage timing, cooldowns, and reuse of the three Red Oni attack animations.
- Reused the existing Player, GameManager Retry, FallZone, audio, and combat pause systems in the boss scene while keeping aimed Faith Beans, boss HP, bar-based player HP, and safe-drop recovery for later passes.
- Added Unity editor build/validation and Play Mode capture utilities for the boss prototype. Automated Play Mode observes two completed attacks, an active warning band, one-way platform traversal, stable Boss visual height, and one-point fall recovery damage.
- Added the first Red Oni Phase 1 animation package under `Assets/Art/boss`: idle plus high, middle, and low club-swing clips, a three-trigger Animator Controller, and a visual-only prefab.
- Matched the Red Oni idle frame to the attack-sheet scale by reusing the first middle-swing frame, preventing visible size jumps when an attack starts.
- Kept the original boss PNG pixels unchanged while applying dedicated per-sheet slicing only to the four boss source textures.
- Added a visual-only left/right patrol to the Red Oni foreshadow, reusing its eight-frame animation and flipping it at safe bounds on the lower goal platform.
- Corrected the Red Oni patrol visual facing: the source frames face left by default, so the sprite now flips only while moving right.
- Updated the route Play Mode diagnostic to validate the patrol component's real facing direction instead of assuming `SpriteRenderer.flipX` always means left.
- Lowered the eleven half-size temporary cloud platforms so the bridge sits closer to the lower-route platform height.
- Aligned each cloud collider with the visible cloud top so the player no longer sinks into the sprite while landing.
- Reduced the Red Oni foreshadow to a 3.5-unit square presentation and grounded its feet on the lower goal platform.
- Refined the Red Oni to 3.1 units tall, about 1.72 times player height and roughly three times the player's visual area.
- Reduced the Red Oni foreshadow to 2.2 units tall, about 1.22 times player height and roughly 1.5 times the player's visual area.
- Added editor validation for cloud height and Red Oni ground clearance.

## 2026-08-05
- Rebuilt the temporary cloud crossing as eleven half-size steps, raised them above the route, and constrained every cloud to the open gap so none overlap either stone platform.
- Rebuilt the goal bridge as nine smaller cloud stepping stones with aspect-correct visuals, and reduced stone platform visual thickness so the player/platform scale reads more naturally.
- Rebuilt the Red Oni preview as V14 with eight independently imported transparent frames and an isolated unlit material, preventing the route preview from contaminating the shared Sprite-Lit batch.
- Rebuilt the isolated route as V12 with six overlapping temporary clouds between the lower landing and goal, extending the stand time to 1.6 seconds so the crossing is forgiving in real play rather than merely passing a theoretical jump-distance check.
- Added an eight-frame transparent Red Oni background animation immediately before the lower goal as visual foreshadowing only; it has no collider, damage, or boss logic.

## 2026-08-04
- Rebuilt the isolated route as V11 using the existing clean `cloud_stage_icon_transparent.png` sprite, removing the baked checkerboard rectangle without editing any source art or `.meta` files.
- Added a fifth temporary cloud before the lower-left goal so the final cloud-to-gate jump has a forgiving overlap instead of requiring the player's maximum theoretical jump distance.
- Rebuilt the isolated route as V10 so `Upper_SecondCrossing` is the only upper-to-lower transition. The obsolete far-right descent ledges were removed and replaced with a visible stone return wall that directs the player back across the collapsing span.
- Lowered the optional lower-right cave floor to the same walkable height as `Lower_RightSolid`, repositioned its crates and hidden StarSeal, and widened the collapse landing so the reward detour can be entered and exited without a softlock.
- Increased route pressure from 14 to 18 enemies with spaced additions on the upper return path, lower-right combat stretch, cave entrance, and post-cloud landing. Existing bounded ground pursuit and flying dive behavior are reused.
- Extended the existing background composition without new art: early art remains at the start, the middle section now repeats through the cave approach, and the late section is reserved for the far-right shrine area. Unity compilation and V10 editor validation passed.
- Rebuilt the isolated route as V9: entrance and goal torii surfaces now align directly with the real stone platforms, while the torii visuals render behind terrain so their floating bases no longer read as detached islands.
- Replaced source-size-relative actor scaling with fixed world-height targets for the player visual, Paper Dolls, flying Ghosts, and StarSeals; ground enemies are now aligned by their visible feet to each platform surface.
- Extended the existing `GhostEnemy` state machine with bounded ground pursuit and active flying dive behavior. Ground enemies chase within safe platform edges, while flying enemies descend toward the player, lunge diagonally, then recover to their hover height.
- Increased the isolated route from 11 to 14 enemies using spaced tutorial encounters rather than large waves. Unity compilation and V9 scene validation passed, including behavior configuration and visible ground alignment checks.
- Rebuilt the isolated route as V7 after playability review: replaced the baked-checkerboard entrance/goal art with the existing genuinely transparent gate sprite, normalized Paper Doll/Ghost/StarSeal visual height against the player, and kept source art untouched.
- Added solid left/right/top world boundaries, expanded FallZone coverage, enabled bounded vertical camera following for the two-level route, and widened the right descent landing overlap.
- Replaced the route-blocking crate pyramid with six low, separated breakable practice crates so the player can attack or jump past them without becoming stuck.
- Added editor checks for route-link gap/step reachability, descent overlap, actor/item scale ratios, camera coverage, and world boundaries; Unity V7 validation passed.
- Corrected the isolated `Stage_1_Route_Prototype` platform reference to use the existing clean `stone_stage_icon_transparent.png` asset instead of the opaque `stone_stage.png` source; gameplay colliders remain separate from visuals.
- Reworked the lower-right StarSeal into a hidden cave reward: the cave is an optional right-side detour after the descent, two corrupted crates guard its entrance, and the StarSeal appears only after both crates are broken and the player approaches the cave.
- Replaced the overly long upper finish platform with four forgiving stone islands and one additional reused Paper Doll encounter, adding jump/combat rhythm without enlarging scene bounds or adding a new enemy system.
- Added editor validation for the clean stone sprite, cave shell, hidden initial reward state, and configured reveal controller without changing `Stage_1_1`.
- Rebuilt the isolated route as V6 and passed Unity Editor validation after aligning the cave detour floor with the descent landing, preventing the optional reward route from trapping the player.

## 2026-08-03
- Upgraded `Stage_1_Route_Prototype` to V4 presentation using the requested existing art: `stone_stage.png` for solid route surfaces, `cloud_stage.png` for temporary clouds, the lit spirit torii at the upper-left entrance, and the simple torii at the lower-left goal.
- Replaced ordinary route crates with the corrupted crate cutout while preserving `BreakableBlock` rewards, and rebuilt spike visuals as repeated horizontally compressed broken-talisman clusters over one reliable hazard trigger.
- Separated route art from gameplay colliders so transparent sprite pivots and visual scaling cannot move the solid terrain, cloud, crate, or hazard collision shapes.
- Generated and saved the V4 hierarchy through the licensed Unity Editor, then passed the editor validation for requested sprites, solid colliders, two hazard zones, four recovering clouds, eight breakable crates, FallZone, lower-left EndGate, calculated jump margin, and the non-reversible right descent with no matching red-error log entries.
- Corrected `Stage_1_Route_Prototype` V3 to follow the reviewed sketch: upper-left start, upper route moving right, far-right down-only descent, lower route returning left, and lower-left temporary clear goal.
- Rebuilt the sketch's black outlines as thick solid terrain with the existing transparent grass tile repeated across the surface; black vertical marks remain spike hazards rather than platforms.
- Kept the crate pyramid, staggered ground/flying enemies, two fixed StarSeals, temporary cloud crossing, and existing Stage Clear/Retry integrations.
- Changed the brown second-crossing platform so the first entry is safe and the second separate entry drops immediately without warning.
- Preserved `Stage_1_1` and kept the Red Oni/bean encounter outside this isolated route prototype.
- Attempted Unity validation, but the local batch editor could not reconnect to Unity Licensing and consequently reported unavailable built-in UI, Physics2D, and Audio modules; no new error specific to `StageOnePlayablePassSetup` was reported before the failed import ended.
- Rebuilt `Stage_1_Route_Prototype` layout V2 after platformer reference review: the player started at the lower-left, crossed temporary clouds over spikes, advanced across short jump/combat islands, climbed at the far right, and returned across an upper route to the upper-left goal. This V2 interpretation was superseded by V3 above.
- Replaced the prototype's oversized continuous platforms with compact stone and wood platforms, three upper spike gaps, a crate pyramid, staggered ground/flying enemies, and beginner-friendly vertical steps. This V2 layout was superseded by V3 above.
- Kept the Red Oni and bean attack outside this route; they remain reserved for a separate boss stage.
- Added `Stage_1_Route_Prototype.unity` as a temporary, isolated route test scene while restoring `Stage_1_1` to its previous scene setup. Its original V2 direction was superseded by the corrected V3 route above.
- Reused existing stone/wood/cloud visuals, Paper Doll and Ghost prefabs, breakable behavior, hazard damage, StarSeal pickups, player health, Retry, background/music, and Stage Clear systems.
- Added Inspector-tunable temporary cloud platforms that warn, disappear, recover, and reset on Retry.
- Added an Inspector-tunable second-crossing platform that is safe once and collapses on the second separate entry, then resets on Retry; V3 removes the warning delay so the second entry drops immediately.
- Kept the Red Oni and bean-attack prototype code disconnected from the route scene after deciding that the giant Red Oni encounter belongs in a separate boss stage.
- Added a non-invasive `GameManager.RetryCompleted` notification so new Stage 1 hazards and the boss can reset without changing existing Retry behavior.
- Recorded that automated Play Mode validation is blocked by pre-existing batch-import module errors involving UGUI, Physics2D, and Audio; these errors were present before the Stage 1 edits.

## 2026-07-14
- Sliced the two newly added generated Backgrounds sprite sheets into 59 transparent cutout PNGs under `Assets/Art/Backgrounds/Cutouts`, preserving the original source sheets.
- Added `LeftRoute_Prototype.unity` as a standalone, non-connected visual draft scene for testing the left-side route mood with the newly sliced shrine/terrain cutouts.

## 2026-07-12
- Added runtime cleanup for farm planting/harvest action frames so tiny detached non-blue artifacts are removed from the popup animation without editing source PNGs or `.meta` files.

## 2026-07-11
- Nudged the left and right farm plot columns inward so the 3x3 crop layout reads closer to the visible soil-bed centers after the latest visual review.

## 2026-07-10
- Enlarged farm crop icons and added per-crop visual offsets for Wheat, CoffeeBean, and Sugarcane so the planted crops read larger and closer to each soil plot center without changing farm gameplay or source art.

## 2026-07-07
- Adjusted farm crop display anchoring so crop sprites use a bottom/root pivot near the soil-bed center, reducing apparent offset from different crop icon proportions while keeping the 3x3 farm logic unchanged.

## 2026-07-06
- Forced the farm controller and panel to use the full 3x3 plot grid, with manually aligned plot anchors for all nine soil beds so the bottom row can be planted and harvested.
- Centered farm crop icons and growth bars inside each of the nine soil plots based on the latest visual review, keeping the farm gameplay logic unchanged.

## 2026-07-03
- Tuned farm crop placement after visual review: crop icons now sit closer to the center of each soil plot, progress bars align lower on the plot, and extra growing text is hidden for a cleaner field view.
- Revised the farm panel into the intended V0.6 flow: click an empty plot, choose a seed from a small popup, plant, wait, then click the ready crop to harvest.
- Expanded the farm panel from 6 to 9 plot click areas to match the background grid, kept the background fully opaque, reduced extra plot text, enlarged crop visuals, and moved the HubMap farm icon toward the upper-left field area.
- Added the first playable farm-loop polish pass: farm panels now refresh growth in real time, each plot shows a thin growth bar and clear plant/grow/harvest prompts, and the planting action frame path uses all 8 available frames.
- Cleaned the newly added farm PNG assets by removing edge-connected white/gray backgrounds from crop icons, planting frames, harvest frames, and the HubMap farm marker without adding a broad import automation script.
- Tightened the farm panel presentation so plot hit areas stay transparent and action-frame previews also use runtime edge cleanup, keeping the planting area free of colored placeholder blocks.

## 2026-07-02
- Added a HubMap farm entry and lightweight six-plot farm panel through `HubFarmPanelController`, allowing Wheat, CoffeeBean, and Sugarcane planting/harvesting without creating a new scene or inventory system.
- The HubMap farm marker now uses the newly added farm icon with runtime-only edge-white cleanup, avoiding any source PNG or `.meta` edits.
- Farm panel plots now show runtime-loaded seed/growing crop icons from `Assets/Art/farm_icon`; ready crops reuse the growing icon until final mature art is added.
- Planting and harvesting now show a centered mini-stage popup with slow 8-frame UI-only action feedback, while farm plot buttons no longer render colored block fills.
- Added Farm V0 core data through `FarmController`: fixed farm plots, simple Seed/Growing/Ready states, real-time growth seconds, lightweight PlayerPrefs plot persistence, and harvest output into the existing `ResourceInventory` ingredients (`Flour`, `CoffeeBean`, `Sugar`).
- Documented the farm as a small daytime support loop for cafe ingredients, with watering, seasons, fertilizer, automation, and full farming economy deferred.

## 2026-06-25
- Recentered the cafe production popup coffee-machine recipe/progress anchors back to the machine, and added special walk-width stabilization for the gramma and traveler visitor sprites to reduce visible size popping during movement.
- Improved cafe visitor walk-size normalization again by measuring the visible alpha bounds of guest sprites in the Unity Editor, reducing left/right versus front/back proportion mismatches caused by uneven transparent canvas space.
- Disabled default walk-frame width normalization so narrow side-view sprites no longer scale the whole guest taller/wider during movement.
- Fine-tuned the cafe production popup coffee-machine alignment and nudged the visible counter coffee-machine display slightly right.

## 2026-06-24
- Reworked cafe visitor walk-size normalization to use Unity sprite display bounds instead of raw pixel rects, reducing remaining size jumps from mismatched import scale or sprite bounds.
- Shifted the cafe production popup coffee-machine control and its recipe/progress UI alignment slightly right, and replaced the completion check fallback with the new `Assets/Art/finish.png` icon when available.
- Normalized cafe visitor walk visuals against a common per-visitor sprite height so front/back/side walking frames are less likely to visibly change size during movement.
- Corrected the left lounge sofa fixed-slot orientation by placing the front-facing sofa on the upper side of the table and the back-facing sofa on the lower side.
- Updated the Editor/Development-only cafe furniture debug unlock preview so fixed furniture appears sequentially with the existing drop animation instead of spawning all at once.

## 2026-06-23
- Adjusted the fixed-slot cafe furniture layout to follow a clearer reference-room structure: left lounge table grouping, right-side sofa seating, cleaner counter decoration placement, and less clutter around the entrance and central walking space.
- Added an Editor/Development-only cafe furniture debug unlock button to the fox altar furniture panel so the full Lv.4 layout can be previewed quickly without changing normal player progression.
- Replaced the Lv.3 fixed sofa display with the cleaner sofa-table-set cutout and moved the furniture panel buttons above the catalog layer so the debug unlock control is visible.
- Registered more existing cafe furniture cutouts for the unlock/debug preview flow and improved the fixed furniture unlock animation so new pieces drop in with a small bounce instead of only fading in.

## 2026-06-22
- Restored the cafe production popup background and machine-select presentation when opening the front counter, while keeping only the unwanted generated border/corner decorations hidden.
- Fixed cafe production collection so completed-item bubbles are clickable: finished items are stored through the existing `ResourceInventory` path when the player clicks the bubble, then the bubble clears and the machine can craft again.

## 2026-06-21
- Refined the cafe production popup into a lightweight machine overlay: the full-screen frame/background is hidden, production bars only show while a machine is working, and completed items appear as small request-style bubbles with the menu icon and green check.
- Added a card-based fixed-slot furniture unlock UI to the fox altar flow. Each furniture entry now shows its icon, unlock state, fixed slot, cost/altar-level requirement, and its own unlock button while still spending FaithPoints through `ResourceInventory`.
- Added `Docs/CafeGuestVisualAudit.md` to track the current cafe guest direction-frame/cutout review without changing art import settings, PNG files, or `.meta` files.

## 2026-06-19
- Updated cafe production so the coffee machine and baker machine can run independently at the same time.
- The production popup now stays open during crafting; each machine keeps its own progress bar, completed item icon, and green completion check.
- Relaxed the cafe operation production lock so concurrent machine jobs still store finished items through the existing ResourceInventory path.

## 2026-06-18
- Adjusted the cafe production progress fill alignment so the animated fill sits inside the progress frame more cleanly for both coffee and baker machines.
- Connected the new cafe production popup timing to the existing fox altar production speed multiplier while preserving per-recipe base craft times.
- Improved cafe visitor walking animation timing by switching the scripted movement cycle from a simple two-frame toggle to an optional four-beat idle/walk_01/idle/walk_02 loop with the same safe sprite fallbacks.
- Added a runtime visual safety pass for cafe visitors: walk frames now gently normalize against the same-direction idle frame width, and naturally wide visitors like tanuki/nekomata get default scale tuning unless an Inspector mapping overrides them.

## 2026-06-17
- Fixed cafe production progress feedback so the popup repairs missing ProgressRoot/ProgressFill/CompleteCheck bindings at runtime, shows visible 4-segment progress during coffee and baker production, and restores the green completion check pop.
- Stabilized cafe visitor walk visuals by disabling the default squash/stretch pose scaling and normalizing walk-frame height slightly, reducing visible left/right walk scale jumps while keeping sprite fallbacks safe.
- Added a safer cafe visitor visual resolver: seated guests now resolve by visitorId/visualId, can use explicit Inspector mappings for special visitors, fall back safely when sprites are missing, and log missing visitor sprites without crashing.
- Added a lightweight fixed-slot cafe furniture unlock panel from the fox altar UI. Default furniture unlock flags use the existing furniture PlayerPrefs keys, optional furniture spends FaithPoints from ResourceInventory, and unlocked visuals refresh immediately in the cafe.
- Connected the cafe delivery step to visitor request bubbles: after production completes the popup can close, players can click a visitor's overhead request bubble to serve the matching finished item, missing finished items show a safe feedback message, and request bubbles get a small hover/availability cue.

## 2026-06-16
- Fixed the cafe production progress bar so it visibly fills during crafting: the fill now uses the original progress-bar inner art, aligns to the empty frame, supports both coffee machine and baker machine production, and keeps the green complete check for the finish state.

## 2026-06-15
- Sliced the cafe furniture source sheet into 42 transparent `furniture_*.png` cutouts under `Assets/Art/cafe_icon/cafe_icons_cutouts/`, including individual chairs, sofas, shelves, lanterns, rugs, tables, counters, and grouped furniture sets for later cafe placement and unlock previews.

## 2026-06-13
- Enlarged the cafe production device presentation: coffee/baker machine visuals are larger on the front counter, their UI production buttons are easier to read, and visitor request menu icons are larger inside the overhead bubbles.
- Split the Lv.2 sofa display from the vertical `cafe_icon_20.png` sheet into a single sofa cutout so the fixed furniture no longer appears as a tiny stacked sheet.
- Added a lightweight world-space visitor message text layer to the door-side `MenuBoard`, showing short previews of recent visitor messages while keeping the full message list in the counter UI.
- Cleaned the cafe request bubble, coffee machine, baker machine, and progress bar art into transparent runtime cutouts; the cafe now places coffee/baker machine visuals on the front counter and uses the cleaned cutouts in the production UI.
- Updated the Lv.2 furniture placeholder display to use the available `cafe_icon_20.png` art instead of leaving floor text, and normalized visitor runtime frames onto consistent per-visitor canvases to reduce side-walk size jitter.
- Fixed cafe presentation bugs: missing-art furniture no longer renders floor text placeholders, visitor request bubbles now prefer menu icons, cafe production is triggered from coffee/baker machine icon buttons with working-frame animation and a progress bar, and current visitor runtime sprites were regenerated at a consistent scale with cleaner side-walk frames.
- Added cafe presentation Phase 5: fox altar furniture unlocks now appear as fixed, visual-only cafe furniture with a small drop/fade animation, and sofa unlocks prepare `GuestSeat_05` / `GuestSeat_06` anchors for later seat expansion while keeping the current four-seat service loop stable.
- Added cafe presentation Phase 4: fox altar level now feeds lightweight cafe production bonuses, showing production speed/output in the fox altar and counter UI while keeping machine placement deferred.
- Added cafe presentation Phase 3: the cafe counter now has a lightweight `制作` flow that consumes ingredients, shows short production progress, stores finished menu items in `ResourceInventory`, and makes `Serve` consume finished items before granting rewards.
- Added cafe presentation Phase 2 storage groundwork: `ResourceInventory` now persists finished menu item counts for `InariCoffee`, `KitsunebiLatte`, and `YozakuraCake`, and the cafe counter UI displays the current finished-item stock without changing the instant Serve flow.
- Added cafe presentation Phase 1: repaired shrine interactions now enter `CafeInterior_Temporary` directly instead of reopening the repair/status popup, and seated visitors show small request bubbles with their current desired menu.

## 2026-06-12
- Added Phase 2 fox altar furniture preview support: the fox altar panel now includes a small unlocked-furniture preview strip that can show known furniture sprites in the Unity Editor and safely falls back to text cards when art is not assigned.
- Connected fox altar furniture unlock data to the existing HeartFox upgrade flow: Lv.1 now registers the fox icon and fox altar base as starting furniture, Lv.3 unlocks four directional double-sofa IDs, and Lv.4 unlocks the shrine lamp plus a small torii ID while keeping furniture placement deferred.
- Added Cafe Day Result V1: exiting `CafeInterior_Temporary` now shows `今日のカフェ記録` with current-session visitors served, Faith Points gained, HeartFox gained, affection increases, and furniture unlocked before returning to `HubMap_Day`.
- Added Cafe Visual Loop V2 support on the existing cafe flow: counter guest selection now highlights the matching visible seated guest, guest arrival/leaving posts gentle status feedback, and fox altar upgrades show the level transition alongside the existing HeartFox cost and furniture unlock list.
- Stabilized Cafe Serving Loop V1.5: liked-menu checks now use stable menu IDs while still displaying Japanese menu names, missing ingredients start with `材料が足りません。`, served/leaving visitor slots are disabled in the counter UI, and the empty-visitor state now checks occupied seats instead of only the list count.
- Fixed cafe visitor seat refill so the visitor who just left a seat is removed from the immediate refill candidate pool when possible, preventing the same visitor from repeatedly entering and leaving the same seat.
- Audited the cafe visitor walk-animation frame set for the current random visitor pool, cleaned white matte backgrounds from the standard front/back/left/right idle and walk frames, and confirmed no walk_01/walk_02 duplicates remain.
- Added standard `guest_gramma_back_idle` and `guest_gramma_back_walk_01` sprite files from the legacy gramma back-frame aliases so the visitor sprite fallback loader can resolve the full 12-frame naming convention.
- Rebuilt the cafe RPG player's left-facing transparent sprites from the original source images to restore hair pixels that were over-cleaned, while keeping the foot-shadow residue removed.
- Rebuilt the tanuki yokai visitor cutouts from the source frames, cleaned the remaining background residue, and regenerated the runtime visitor frames at a more consistent cafe-visitor size.
- Cleaned the RPG cafe player transparent sprites by removing edge-connected gray/white matte residue and the visible foot-shadow floor residue while preserving the character body, hair, outfit, and tail.
- Cleaned current cafe furniture cutouts used by the counter/menu area, while intentionally skipping the fox shrine and offering-table/altar-side assets after visual review.
- Cleaned the cafe guest-seat stool sprite transparency by removing gray/white background residue from `Assets/cafe_icon_14.png` and the matching `Assets/Art/cafe_icon/cafe_icons_cutouts/cafe_icon_14.png` source cutout.
- Removed the three direct Kenney decor test sprites from `CafeInterior_Temporary` after visual review; the assets remain archived for graybox or internal layout use, but they should not be used directly in the cafe's main visual layer without restyling.
- Added a small low-risk Kenney art integration test to `CafeInterior_Temporary`: `KenneyCrateDecor_01`, `KenneyPlantDecor_01`, and `KenneyShelfDecor_01` are visual-only sprites under `FurnitureAndDecor` with no colliders, scripts, or gameplay changes.
- Added Unity import metadata for the three selected Kenney tile sprites used by the cafe decor test.
- Organized newly downloaded Kenney art resources into Unity-facing folders: character spritesheets now live under `Assets/Art/Spritesheets/Kenney/roguelike_characters`, while RPG tilemap sources, exported tile sheets, sliced tiles, and Tiled examples live under `Assets/Tilemaps/Kenney/roguelike_rpg_pack`.
- Removed the empty `kenney_roguelike-indoors` source folder after confirming it contained no files.
- Updated `Docs/ArtAssetGuide.md` with external asset organization notes and the included Kenney CC0 license handling rule.

## 2026-06-11
- Fixed cafe visitor seat refill: after a served visitor leaves through the doorway, the same seat can receive a new weighted random visitor after a short delay.
- Added editor-time cafe visitor sprite completion from existing `Assets/Art/cafe_icon/guest_*` files, so older scene visual sets can fill missing directional walk frames before falling back.
- Added cafe-counter-aware sorting to `HubPlayerController`, letting `CafePlayer` render behind the front counter when walking in the counter-back area and in front again on the open floor.
- Added lightweight PlayerPrefs persistence for `ResourceInventory` Faith Points, HeartFox/material counts, cafe starter ingredient initialization, and cafe visitor affection values by `visitorId`.
- Polished cafe serving feedback so completed service shows `来訪者は少し安心したようです。`, liked menus show `気に入ってくれたようです。`, and HeartFox rewards keep the existing `こころ狐を受け取りました。` feedback.
- Documented the current prototype save/load boundary: core resources, HeartFox, fox altar level, furniture unlock IDs, shrine repair state, and visitor affection persist, while active seats, orders, recent messages, guest positions, and future furniture layouts remain placeholders.
- Extended the fox altar placeholder upgrade path to Lv.4: Lv.1 -> Lv.2 costs 3 HeartFox, Lv.2 -> Lv.3 costs 5 HeartFox, and Lv.3 -> Lv.4 costs 8 HeartFox.
- Updated fox altar feedback so failed upgrades show `こころ狐が足りません。` while successful upgrades continue to show the warmer shrine message and data-only furniture unlock notice.
- Adjusted placeholder furniture unlock data so `furniture_small_flower_table` unlocks at Lv.2, `furniture_soft_sofa` unlocks at Lv.3, and `furniture_shrine_lamp` unlocks at Lv.4.
- Added lightweight cafe visitor data support with visitor IDs, `Living` / `Spirit` / `Yokai` / `Special` types, favorite menu lists, message lists, random weights, and HeartFox eligibility.
- Updated cafe visitor refresh to use weighted random selection without replacement, while keeping the `black_priest` Special visitor out of the early random pool until a future unlock.
- Updated the cafe counter UI so each visitor row shows type, affection, current order, and favorite menu summary.
- Added gentle cafe visitor service messages to the serve result and recent-message board, keeping the tone closer to `来訪者` gratitude than generic shop reviews.
- Added temporary HeartFox reward feedback: liked-menu service now shows `こころ狐を受け取りました。`, briefly displays a HeartFox icon if available, and falls back to a simple `狐` placeholder with one warning if the icon is missing.
- Improved fox altar upgrade feedback so successful upgrades show `狐の祠が少しあたたかくなりました。` and unlock placeholder furniture IDs for future placement systems.
- Fixed the cafe visitor system so CafeInterior_Temporary refreshes a random current visitor list and the counter UI reads that list instead of hardcoded four default guests.
- Added `HeartFox` / `こころ狐` to ResourceInventory as a gratitude resource earned by serving a visitor's liked menu, without creating another Faith Points system.
- Added placeholder fox altar upgrades that consume 3 then 5 HeartFox and prepare future furniture unlock text without implementing furniture placement.
- Extended cafe visitor walking visuals to support front/back/left/right idle and two-frame walk sprites, with safe one-time warnings and sprite fallbacks for missing frames.
- Added a lightweight step-pose scale pulse to cafe guest movement so guests with limited walk frames no longer appear to glide.
- Fixed cafe guest departure facing so guests use front-facing sprites when walking back down toward the doorway instead of appearing to walk backward.
- Added a minimal cafe guest order-completion loop: serving a correct order now moves the guest from waiting to message state, then departure state, then clears the seat.
- Added cafe guest departure presentation so served guests walk back toward the doorway and disappear after leaving their message.
- Updated the cafe message board to keep recent served-guest messages even after the guest leaves.

## 2026-06-10
- Updated the cafe guest design documentation: `夜神社カフェ` is framed as a warm boundary refuge for `来訪者`, with gentle message tone rules, future Living/Spirit/Yokai/Special visitor categories, initial visitor direction, and a note that `黒衣の司祭` should appear later as a special visitor rather than a normal early guest.
- Changed cafe opening guest selection to randomly choose 4 guests from the full current guest pool.
- Connected the kappa yokai and middle-aged office worker guests to the cafe operation flow, including cleaned runtime portraits and back-facing arrival sprites.
- Generated transparent runtime guest sprites for `kappa_yokai` and `middle_aged_office_worker`, with front/back/left/right idle and two-frame walk variants for each direction.
- Connected the kimono girl and child kimono guest art to the cafe operation flow, including cleaned front portraits and back-facing arrival sprites.
- Generated transparent runtime guest sprites for `girl_kimono` and `child_girl_kimono`, with front/back/left/right idle and two-frame walk variants for each direction.
- Connected the student girl and tanuki yokai guests to the cafe operation flow with refreshed operation UI icons and guest-ID based arrival visuals.
- Updated cafe guest arrival visuals so spawned guest sprites are resolved by guest ID instead of fixed seat index, and refreshed cafe UI guest icons when the active roster changes.
- Generated transparent runtime guest sprites for `student_girl_uniform` and `tanuki_yokai`, with front/back/left/right idle and two-frame walk variants for each direction.
- Matched the new runtime guest sprites to the existing cafe guest scale range so they are ready for later cafe guest-pool wiring without changing current cafe logic.

## 2026-06-09
- Thickened basic enemy HP for better combat rhythm: normal Ghost prefabs now use 3 HP, Paper Dolls use 2 HP, and Ghost Lanterns use 3 HP, with Stage_1_2 placed enemies updated to match.
- Moved the Combat Feedback V1 practice objects from Stage_1_1 to Stage_0_0 so the attack-practice props live in the tutorial stage.
- Generated cleaned cutout variants for the four corrupted yokai-residue prop images and updated the practice setup to prefer `prop_*_cutout.png` sprites.
- Strengthened breakable-object impact feedback with a longer shake, scale punch, stronger hit motes, and stronger break motes.
- Extended Combat Feedback V1 Step A: breakable practice objects include a small shake reaction and optional runtime pickup drops, with Stage_0_0 spawning two practice-only targets, one FaithPoint reward target, and one Heart reward target near the start area.
- Updated the Stage_0_0 combat practice objects to prefer the new corrupted yokai-residue prop art in `Assets/Art/Tools_icon`, with runtime prototype-square fallback if Unity has not imported the sprites yet.
- Added Combat Feedback V1 support: breakable objects now flash on hit, spawn hit/break motes, show reward feedback, and can be configured at runtime for practice-only or reward behavior.
- Added a runtime Stage_0_0 start-area practice group with two practice-only breakables and two small reward breakables, without creating a new scene or changing PlayerController.
- Strengthened monster hit reaction: `GhostEnemy` now visibly slides backward from attack knockback, the base Ghost prefab now has 2 HP, and Stage_1_1 SealGhost knockback/stun tuning is more readable for combo hits.

## 2026-06-08
- Added a lightweight chase-pressure pass to `GhostEnemy`: enemies now remember the player briefly, show a warmer chase tint, speed up at close range, and make a short horizontal lunge during their attack warning.
- Retuned Ghost-style enemy pursuit parameters across the base Ghost, Paper Doll, and Ghost Lantern prefabs, plus placed Stage_1_1, Stage_1_2, and NightApproach enemies, so night combat feels more like the player is being chased.
- Restored Stage_1_2 Ghost Lantern enemies to `0.18` scale and enlarged Paper Doll enemies proportionally to `0.225` in both prefabs and placed scene instances.
- Remapped the HubMap_Day night stage select entries so node 1 loads `Stage_0_0`, node 2 loads `Stage_1_1`, node 3 loads `Stage_1_2`, and node 4 stays locked as the Boss placeholder.
- Completed the Stage_1_1 basic combat feedback phase: Ghosts now use a clearer hit flash, stronger knockback, short hit stun, simple fade/float vanish, clearer StarSeal drop feedback, and the existing kagura-bell-style attack SFX path.
- Retuned Ghost Lantern scale again so its apparent height matches the enlarged Paper Doll more closely in Stage_1_2.
- Moved the locked Stage 1-3 and Boss nodes closer to the marked center-right water positions on the night stage select screen, then retuned Stage_1_2 enemy proportions so Paper Dolls read larger and Ghost Lanterns read smaller.
- Made the HubMap_Day night stage select BGM more reliable by moving it to its own child AudioSource, restarting it each time the panel opens, and keeping it independent from hover SFX; also moved node 4 left and added hover scale plus halo feedback to stage nodes.

## 2026-06-07
- Switched the temporary ACT attack SFX from `sariin.mp3` to `鈴を鳴らす.mp3` for the next attack-feel test pass.
- Restored night stage select audio takeover so opening `NightStageSelectPanel` pauses HubMap BGM and plays the menu BGM, then retuned Stage_1_2 Paper Doll visuals and colliders to match the small Ghost scale.
- Switched ACT attack SFX to `sariin.mp3` with a wider runtime playback window.
- Cleaned the night stage select node button backgrounds and transparent icon pixels to remove visible square blocks, and made Stage_1_1 / Stage_1_2 attack SFX clearer by raising SFX volume and allowing a longer attack playback window.
- Changed the night stage select hover cue to fixed-pitch `small-japanese-cast-iron-wind-chime.wav` playback capped at the first 4 seconds, and restored ACT attack SFX to `bell.wav`.
- Cleaned the HubMap_Day night stage select node icons into transparent versions and switched node hover feedback to the softer `bell-japanese-small.wav` clip at a lower UI volume.
- Finished the Stage_1_2 new-enemy tuning pass with compact final proportions: Paper Dolls now use `0.075` scale with enlarged colliders and Ghost Lanterns use `0.25`, with shorter patrol/chase leashes and smaller contact attack ranges for safer beginner combat.
- Retuned Stage_1_2 Paper Doll and Ghost Lantern readability: Paper Dolls use reduced hover sway and shorter patrol/chase leashes, while Ghost Lanterns are compact low-hover enemies with calmer bobbing and safer pursuit ranges.
- Continued Phase 4 combat cleanup by applying the new enemy attack telegraph timing directly to Stage_1_2's placed Paper Doll, Ghost Lantern, and disabled SealGhost scene instances.
- Added Phase 3 combat readability tuning: state-machine enemies now show a short warning tint before contact damage resolves, letting the player step out of range instead of taking instant touch damage.
- Retuned Stage_1_1 SealGhost attack windows and updated Ghost/Paper Doll/Ghost Lantern prefab defaults so future enemies inherit clearer attack telegraphs and safer cooldowns.
- Made the HubMap_Day night stage select panel open as a full-screen overlay, pause the HubMap BGM, and play `Lotus Lantern Menu.mp3` until Back or stage launch.
- Shortened the runtime playback window for the temporary player attack bell SFX so the source clip stays untouched while attacks sound more immediate.
- Tuned the existing PlayerAttack 3-hit combo feel with a more forgiving input buffer/reset window, slightly larger per-step hitboxes, quicker early recovery, and a clearer third-hit visual reach while preserving movement and Stage_1_1 route setup.
- Retuned the existing Stage_1_1 combat encounters without adding new systems: normal Ghost spawn markers are staggered more safely, SealGhost_01 blocks the first flat combat route, SealGhost_02 now guards the optional reward cloud route, and SealGhost_03 sits before the EndGate as a light final route obstacle.
- Added a lightweight `LevelMenuAudioController` for the HubMap_Day night stage select screen, with soft wind-chime hover SFX and a short lantern-ignite SFX before loading available stages.
- Replaced the ACT player attack clip in Stage_0_0, Stage_1_1, and Stage_1_2 with `bell-japanese-small.wav`, and added Unity meta files for the new audio clips.
- Reworked the HubMap_Day night stage selection UI into a map-node layout closer to the target mockup, with a top-left back button and star-style stage nodes over the night background.
- Enabled the Stage 1-2 node in the night stage selection panel so it loads the existing `Stage_1_2` scene, while Stage 1-3 and Boss remain locked placeholders.

## 2026-06-06
- Connected the new `BG_level.png`, `level_finfish_icon.png`, and `level_icon.png` art to the HubMap_Day night stage selection UI, with the background behind the panel and status icons on each stage row.
- Added a lightweight HubMap_Day night stage selection panel opened from the existing night patrol icon. Stage 1-1 now launches from the panel, while Stage 1-2, Stage 1-3, and Boss entries are visible as locked/coming-soon placeholders.
- Cleaned the HubMap_Day controller's default panel text and guarded hub movement/world clicks while the night stage selection UI is open.
- Applied the same minimum combat-core tuning to Stage_1_2: Paper Doll and Ghost Lantern prefabs and placed scene enemies now include detect range, attack range, attack cooldown, attack pause, and hit-stun parameters.
- Updated Stage_1_2's copied SealGhost scene objects to use the same Ghost state-machine fields for consistency if they are re-enabled later.
- Completed the minimum combat core needed before Stage 1-2 without creating or expanding a new stage: `GhostEnemy` now has Idle, Patrol, Chase, Attack, Hit, and Dead state flow with serialized detect range, attack range, attack cooldown, attack pause, and hit-stun timing.
- Enabled the lightweight Ghost state-machine tuning on the base `GhostEnemy` prefab and the three existing Stage_1_1 SealGhost combat enemies so the current playable stage can test detect, chase, attack, hit reaction, death, Faith Point rewards, and StarSeal drops.
- Kept the existing single-button `J` attack as the player normal combo path: the current 3-hit chain remains movement-safe, uses per-step hitbox timing, and gives the third hit stronger damage/feedback.
- Added lightweight combo-step feedback differences: attack motes now vary by combo step, the third hit adds a small camera accent, and attack SFX reuses the same clip with a subtle per-step volume accent.
- Added a lightweight 3-hit `J` attack combo to `PlayerAttack` with per-step hitbox size, offset, active time, cooldown, visual duration, damage, and effect travel tuning.
- Made combo input slightly more forgiving with a separate combo input buffer and a short combo reset window, while keeping the existing single-button attack and `AttackHitbox` hit-safety behavior.
- Added an optional lightweight enemy state machine to `GhostEnemy` with idle, patrol, chase, hit-stun, and dead states while keeping existing Stage 1-1 Ghost behavior disabled by default.
- Enabled the new state-machine movement only for Stage 1-2 Paper Doll and Ghost Lantern enemies: Paper Dolls use near-ground patrol, while Ghost Lanterns use low-hover patrol/chase.
- Restored `Stage_1_2` from the stable `Stage_1_1` base after a scene-text correction pass, then re-applied the Stage 1-2 enemy introductions safely.
- Retuned Stage 1-2 Paper Doll and Ghost Lantern proportions: Paper Dolls now read as smaller near-ground enemies with only a tiny supernatural sway, while Ghost Lanterns hover lower and smaller.
- Kept copied Stage 1-1 StarSeal objects and SealGhosts disabled in Stage 1-2 so the new stage remains focused on normal small-enemy introductions.

## 2026-06-05
- Created the first Stage_1_2 playable blockout scene from the stable Stage_1_1 setup and registered it in Build Settings.
- Added PaperDollEnemy and GhostLanternEnemy prefabs using existing Ghost movement, GhostHealth feedback, Faith Point rewards, and the new 4-frame monster animation sheets.
- Updated Stage_1_2 into a Summer Festival Backstreet first pass with disabled copied StarSeals, renamed sections, Paper Doll encounters, Ghost review spawns, Ghost Lantern encounters, and mixed combat placement.
- Added a small GameManager StarSeal UI visibility switch so Stage_1_2 can avoid showing Stage_1_1's three-StarSeal objective while preserving the existing default.
- Added cleaned transparent animation sprite sheets for the new paper, kasa, and lantern monster art while preserving the original source images.
- Added a small reusable `SpriteFrameAnimator` and three visual-only monster prefabs for previewing their 4-frame idle loops without changing existing enemy gameplay.

## 2026-06-04
- Reduced the Stage_1_1 late-section background scale, disabled the old tutorial sign placeholder blocks, and made normal Ghost death hide the sprite immediately while keeping vanish feedback and rewards.
- Corrected the Stage_1_1 background setup to use three horizontal level-progression sections: early, middle, and late/end, with no parallax or visual-depth layering.
- Organized `Stage01_Backgrounds` into `BG_EarlySection`, `BG_MiddleSection`, and `BG_LateSection` using the new `stage_1_1_*` background art.
- Added sprite import metadata for `stage_1_1_middle.png`, `stage_1_1_front.png`, and `stage_1_1_end.png` so Stage_1_1 can reference them by stable GUIDs.
- Documented the Stage 01 horizontal background section hierarchy, current art assignments, placement, and safety rules.
- Completed the basic combat-feel pass with a runtime fallback purification slash, attack-start motes, slight forward slash motion, and a short per-Ghost contact-damage cooldown.
- Added a lightweight runtime CameraShake helper and connected subtle shake amounts to Ghost hit, Ghost vanish, and player hurt feedback without changing CameraFollow or scene setup.
- Added lightweight runtime combat mote feedback for Ghost hits, Ghost vanish moments, and player hurt reactions without adding a full VFX system or scene prefab dependency.
- Improved the basic Stage_1_1 ACT combat feel without rewriting player movement: J attacks now have a tiny input buffer, a short active hitbox, and a slightly longer fading slash/charm visual.
- Updated AttackHitbox so damage detection ends before the placeholder attack visual disappears, keeping feedback readable without extending the damage window.
- Added clearer Ghost feedback through hit flash, small knockback, a very short hit stop, movement pause on death, and a brief vanish/fade before destruction while preserving Faith Point rewards and StarSeal drops.
- Improved player damage readability with immediate red hit flash, short invincibility frames, small knockback, and blinking during invincibility while keeping the existing heart UI.
- Documented the current combat feedback rules and added a focused Stage_1_1 combat retest item.

## 2026-06-03
- Nudged the rabbit merchant portrait toward the marked lower-left counter area and increased its size slightly.
- Tuned the rabbit merchant intro animation to start lower-left and play more slowly so the pop-in is easier to notice.
- Added a lightweight rabbit merchant pop-in animation for the HubMap ingredient shop and moved the portrait to a larger left-lower counter position.
- Regenerated the `store.png` import metadata as a single full-image Sprite and updated the HubMap shop background reference back to the standard single-sprite file ID.
- Fixed the ingredient shop overlay so opening it pauses the HubMap BGM, closing it resumes the HubMap BGM, and the shop interior background can be loaded directly from `Assets/Art/Backgrounds/store.png` if the serialized sprite reference is not enough.
- Connected the HubMap ingredient shop panel to the `store.png` interior sprite with the correct sprite reference, added `Rabbit Store.mp3` as temporary shop BGM, and enlarged the rabbit merchant portrait without its extra backing block.
- Refined the HubMap ingredient shop presentation into a full-screen shelf-style trading UI using the existing `store.png` shop interior background, while preserving the existing purchase and inventory logic.
- Fixed the cafe guest visual mapping so `GuestSeat_04` / `不思議な常連` uses the `guest_priest` icon and cafe-floor sprites instead of the old `guest_gramma` art.
- Updated the fallback front-counter summary so `GuestSeat_04` is no longer labeled as empty/undecided.
- Updated the guest visual mapping notes in `Docs/SystemDesign.md`.

## 2026-06-02
- Added the first minimal cafe order loop: each guest receives a random request from the three early menu items when the cafe opens and after each successful serving.
- Added lightweight cafe ingredient storage through the existing `ResourceInventory` material-count structure: `CoffeeBean`, `Milk`, `Sugar`, and `Flour`.
- Added recipe checks and ingredient consumption for `稲荷コーヒー`, `狐火ラテ`, and `夜桜ケーキ`. Wrong dishes and missing ingredients now block serving with a clear message.
- Added a small `仕入れ` panel that purchases one ingredient at a time by spending the existing stored Faith Points without creating a second currency counter.
- Added two units of each ingredient as one-time trial-opening stock so the cafe loop can be tested directly.
- Added explicit cafe ingredient APIs to `ResourceInventory`: `AddIngredient`, `SpendIngredient`, `GetIngredientCount`, and `HasIngredient`.
- Aligned the early recipes and temporary shop pricing: `稲荷コーヒー` uses one CoffeeBean, `狐火ラテ` uses CoffeeBean and Milk, `夜桜ケーキ` uses Flour and Sugar, and Flour costs 2 Faith Points.
- Renamed the temporary cafe restocking panel to `仕入れ商店` and made each ingredient purchase action explicit.
- Generated cleaned transparent runtime derivatives for the new store art while preserving the original generated images.
- Added cleaned rabbit merchant, ingredient, and full-store visuals for the temporary `仕入れ商店` flow.
- Added `HubIngredientShopController` and moved the temporary `仕入れ商店` into HubMap_Day as an upper-right rabbit-and-jar merchant interaction point.
- Removed the `仕入れ` button and ingredient-shop popup from CafeInterior_Temporary. The cafe now only consumes the shared ingredients during serving.

## 2026-06-01
- Added Cafe Operation Phase 1 through a small `CafeOperationController`: four guest states tied to `GuestSeat_01` through `GuestSeat_04`, three menu items, temporary affection values, and latest-message placeholders.
- Added a minimal serving action that grants Faith Points through the existing `ResourceInventory` single source of truth and increments the selected guest's temporary affection by 1.
- Updated the existing CafeInterior_Temporary front-counter summary to read its four guest slots from the new cafe-operation data layer while keeping the fox altar separate.
- Prepared optional guest-icon references for the new `guest_*` art folders. The selectable cafe-operation UI and icon presentation remain scoped to Phase 2.
- Added Cafe Operation Phase 2: the counter now opens a selectable guest/menu panel with Serve feedback, current Faith Points, and a minimal latest-message board.
- Added a lightweight fixed-route guest arrival presentation without NPC pathfinding: four guest visuals enter through the lower doorway and move to their matching physical counter seats.
- Generated cleaned transparent runtime derivatives for the four guest portrait, back-idle, and back-walk sprite sets while preserving the original `guest_*` source art.
- Tightened the guest runtime sprite crops and normalized per-guest visual scale so their proportions and counter placement read more naturally.
- Raised the cafe guest base scale to match the RPG protagonist more closely and nudged seated guests further toward the counter so the foreground counter layer creates a clearer seated effect.
- Pulled the seated guest offset back slightly so the foreground counter layer no longer covers guest heads while still preserving the intended chair-to-counter depth.
- Raised seated guest rendering above the counter layer so the current full-character guest art remains clearly visible while seated.
- Generated cleaned transparent runtime derivatives for the new cafe menu art and connected icons for `稲荷コーヒー`, `狐火ラテ`, and `夜桜ケーキ` to the Serve UI. The additional sakura soft drink remains reserved for later expansion.
- Completed the Cafe Operation Phase 3 acceptance handoff: verified the current manual cafe loop and documented the next narrow pass for requested dishes and simple stock purchasing without expanding into full management simulation.
- Set cafe operation buttons to an explicit dark default palette with restrained hover and pressed states so the white menu labels remain readable at all times.
- Added a front-counter `開業` button. Cafe guests now wait outside until service begins, then enter through the lower doorway and take their four existing seats in sequence.
- Disabled guest selection, menu selection, and serving until the cafe is open; the button changes to `営業中` after activation.

## 2026-05-31
- Connected the first simple day/night loop: CafeInterior_Temporary can return to HubMap_Day, HubMap_Day can enter Stage_1_1 through a distinct night-patrol icon, and Stage_1_1 can pause and return to the daytime map.
- Added CafeInterior_Temporary placeholder panels for the fox altar and front counter without introducing cafe-management logic.
- Renamed the four physical counter chairs to `GuestSeat_01` through `GuestSeat_04` so the reception placeholder UI maps to future guest-spawn anchors.
- Added a minimal Stage_1_1 Esc pause menu with Resume and Return to Map while preserving Retry and Stage Clear behavior.
- Restored the CafeInterior_Temporary reception counter to the intended `cafe_icon_02.png` presentation scale and removed the extra PlantDecor object from the walking space.
- Replaced the CafeInterior_Temporary return button and exit label with a natural lower-center doorway transition: walking out now returns to HubMap_Day.
- Replaced the HubMap_Day night-patrol torii placeholder with a cleaned transparent runtime variant of `night_entrance.png` and tuned its map scale for the new moonlit-pool icon.
- Made cafe construction a one-time unlock using a minimal PlayerPrefs flag, so the player is not charged again after returning to HubMap_Day or restarting the prototype.
- Repositioned the moonlit-pool night entrance into the HubMap_Day lower-right clearing and separated its sprite scale from a larger reliable click area.

## 2026-05-30
- Enlarged the HubMap_Day shrine and warehouse icons and added subtle offset shadow layers so the repaired shrine reads as the main daytime hub destination.
- Furnished CafeInterior_Temporary with a cafe counter, fox altar, two table sets, two sofa sets, menu board, side cabinet, and plant while preserving an open center aisle.
- Added `Midnight Matcha Shift.mp3` as the looping CafeInterior_Temporary BGM and replaced the main ACT stage attack SFX with `bell.wav`.
- Added lightweight collision-aware RPG movement and simple CafeInterior_Temporary furniture colliders so the player can navigate around major props while keeping the center aisle open.

## 2026-05-29
- Switched the Stage HP HUD back to the earliest simple text-heart version while keeping the lowered HUD placement.
- Restored the Stage HP HUD to the earlier heart icon style and lowered the top-left HUD rows for better spacing.
- Simplified the Stage HUD to icon-led counters and raised the Player heart HUD so the three-heart display is easier to see.
- Expanded the CafeInterior_Temporary player movement bounds and hardened RPG movement input so the cafe player can walk freely with WASD or arrow keys.
- Reconnected Retry-state audio so GameManager now drives AudioManagerRetryController on player death/fall and retry while keeping BGM continuous.
- Hardened PlayerHealth heart UI creation so the three-heart HP display can recreate itself and render above the normal HUD.
- Refreshed `cafe_finish_transparent.png` from the updated `cafe_finish.png` source art while preserving the existing HubMap sprite reference.
- Swapped the CafeInterior_Temporary background sprite to `Assets/Art/Backgrounds/cafe.png`.
- Added a transparent cropped `fox_god_transparent.png` cafe icon asset while keeping the original `fox_god.png` as the source image.
- Created transparent cafe icon assets from `cafe_icons.png`, including a cleaned full sheet and 27 cropped cutout sprites for future cafe layout work.
- Hardened the Stage HP UI so PlayerHealth can render hearts with the existing `stage_heart.png` sprite instead of relying only on text glyphs.
- Updated Stage_0_0 and Stage_1_1 to use `Night_ Loop.mp3` as their looping stage BGM.
- Added AudioManagerRetryController for continuous Retry-state BGM handling with low-pass/volume transitions and optional AudioMixer snapshots.
- Added CafeInterior_Temporary as a lightweight cafe interior scene using `cofee_front.png`, the RPG player movement sprites, and a return-to-Hub button.
- Connected the repaired HubMap shrine action so it can enter CafeInterior_Temporary after repair.
- Wired the repaired shrine/cafe icon into HubMap_Day so successful shrine repair swaps from the ruined icon to `cafe_finish_transparent.png`.

## 2026-05-28
- Fixed invalid 31-character GUIDs in HubMap_Day script and audio references so Unity can parse the scene cleanly.
- Added Phase 4 minimal HubMap shrine repair: the ruined shrine can spend 10 stored Faith Points and switch to a repaired state for the current hub session.
- Added Phase 3 Stage Clear flow: the Stage Clear popup now has a Continue button that loads HubMap_Day, and HubMap_Day is included in Build Settings.
- Wired `Shrine Path .mp3` as the looping BGM for HubMap_Day using the existing lightweight GameAudio component.
- Added HubMap_Day Phase 2 lightweight click interactions: the warehouse panel reads Faith Points and BasicYokaiMaterial from ResourceInventory, while the ruined shrine panel shows the future repair requirement.
- Moved the HubMap_Day warehouse icon and interaction point into the lower-left clearing to reserve the central space for the ruined shrine.
- Enlarged HubMap_Day building icons and moved the ruined shrine icon into the central clearing, with interaction placeholders kept aligned.
- Added transparent four-direction RPG player sprite variants and wired HubPlayer movement to switch idle/walk sprites by direction.
- Added cleaned transparent HubMap icon variants and a minimal HubPlayerController so the HubMap_Day player can move with WASD or arrow keys.
- Created HubMap_Day as Phase 1 of the daytime RPG hub flow, using the new map background, warehouse icon, ruined building icon, and RPG player front sprite.
- Added a Unity meta file for the first RPG player front sprite so HubMap_Day can reference it reliably.

## 2026-05-27
- Added the mamori_part StarSeal icon to the Stage_1_1 StarSeal UI while keeping StarSeal rewards independent from ResourceInventory.
- Added a lightweight ResourceInventory for stored Faith Points and future BasicYokaiMaterial counts, with GameManager syncing Faith Point rewards into the inventory and UI.
- Tightened Faith Point ownership so ResourceInventory is the single stored source of truth while GameManager only forwards rewards and refreshes UI.
- Added Stage_1_1 SealGhost rewards: three special Ghost enemies now drop one StarSeal each, and StarSeal UI displays progress as a 0/3 counter.
- Lowered Stage_1_1 SealGhost StarSeal drop offset so dropped rewards appear closer to the ground.
- Polished Stage_1_1 with a slightly longer final approach, more FaithPoint route guidance, and three reachable StarSeal pickups.
- Updated Stage_1_1 to use `BGM_Stage_1_1_temporty.wav` and replaced its StarSeal visuals with `mamori_part.png`.
- Fixed Stage_1_1 StarSeal scene serialization so their Transform components and child references load correctly in Unity.
- Cleaned baked checkerboard backgrounds from `mamori_part.png` and `stage_heart.png`, reduced StarSeal display size, and assigned `stage_heart.png` to the reward platform Heart pickup.

## 2026-05-26
- Cleaned the dark edge background from `stage_icon.png` and reset in-scene FaithPoint sprite tint so the blue flame icon no longer appears green or boxed.
- Updated in-level FaithPoint pickups to use `Assets/Art/Tools_icon/stage_icon.png`, reduced their visible scale, and kept their trigger area easy to collect.
- Hardened FaithPoint and StarSeal pickups so they resolve the active GameManager before disappearing, preventing collectibles from vanishing without updating the counter.
- Imported the prototype audio clips with Unity meta files, added a minimal GameAudio component, and wired BGM/SFX into Stage_0_0 and Stage_1_1.
- Added sound hooks for jump, landing, attack, player hurt, FaithPoint/StarSeal/Heart pickup, Ghost vanish, Retry fall, Stage Clear, and spike hazards.
- Renamed ShrinePrototype to Stage_0_0 and Stage01_NightApproach to Stage_1_1, preserving their Unity meta GUIDs and Build Settings entries.
- Added the existing night shrine background image to Stage01_NightApproach and ShrinePrototype as a non-colliding background layer behind the playable level.
- Created Tutorial_00_BasicMove as a short 30-second movement tutorial with a safe start, one jump, FaithPoint guidance, one StarSeal clear goal, RetryZone, and torii-style EndGate.
- Created Level_01_NightShrinePath as the first compact beginner level with Start, Jump, First Enemy, Breakable Block, Hazard, Vertical Platform, Hidden Reward, Triggered Enemy, and End sections.
- Added StarSeal pickup support to PickupItem and a temporary StarSeal counter to GameManager.
- Added minimal support scripts for Level 01 setpieces: BreakableBlock, HazardDamage, TriggerGhostSpawner, and SimpleOneWayPlatform.
- Added both new scenes to EditorBuildSettings and documented their layouts in Docs/LevelDesign_Tutorial_00.md and Docs/LevelDesign_Level_01_NightShrinePath.md.
- Extended PickupItem with a FaithPoint mode and connected the three Stage 01 reward-route FaithPoint pickups so they update the Faith Points UI.
- Synced the Stage01_NightApproach tutorial layout back into ShrinePrototype so the original main prototype scene uses the same Stage 01 route.
- Created Stage01_NightApproach as the first structured tutorial level layout, reusing existing movement, attack, health, retry, pickup, GhostSpawner, and EndGate systems.
- Organized the level under Stage01_Level with Geometry, SpawnPoints, Pickups, Hazards, Goal, and Notes groups.
- Added Stage 01 sections: StartArea, JumpTutorialArea, FirstCombatArea, RewardRouteArea, MixedChallengeArea, and EndArea.
- Added five ghost spawn points, a level-wide FallZone, one functional Heart pickup, Faith Point pickups, movement/attack sign placeholders, and a torii-style EndGate.
- Added Docs/LevelDesign_Stage01.md and included Stage01_NightApproach in EditorBuildSettings.

## 2026-05-25
- Integrated the new stage_icon platform art into ShrinePrototype: stone path visuals, wooden jump platforms, spiritual cloud reward platforms, and a torii-style EndGate visual while keeping existing colliders unchanged.
- Generated transparent runtime icon variants from the new Tools_icon art and wired the Faith Points UI and Heart pickups to use them.
- Revised the current reward system so small Ghost enemies grant Faith Points, Faith Points show in UI, and current pickups are Heart recovery items only.
- Moved Heart pickups onto the optional reward route and removed active shard pickup behavior from the prototype.
- Created transparent platform sprite variants from the new stage/cloud art and assigned them to ShrinePrototype platform visuals while keeping simple box colliders unchanged.
- Prepared sprite asset folders for platform, background, and item textures, and added an ArtAssetGuide with naming and size recommendations.
- Added a compact optional upper reward route to ShrinePrototype with wooden steps, a pale blue spiritual cloud platform, and reward pickups.
- Darkened normal stone-path placeholder platforms so the reward route material colors read more clearly.
- Cleaned leftover white background pixels from the miko run and jump transparent sprites, especially the run pose leg gap.
- Added a minimal PickupItem system for the ShrinePrototype level.
- Added temporary pickup UI and placed reward pickups in ShrinePrototype.
- Added PlayerHealth.Heal so Heart pickups restore 1 HP up to max HP.
- Added a GameManager fall-height fallback so falling below the level triggers Retry even if the FallZone trigger is missed.
- Added EndGateTrigger and a minimal Stage Clear popup when the Player reaches the torii-like EndGate.
- Replaced the temporary Player HP text with a minimal top-left three-heart UI that empties hearts as the Player takes damage and refreshes on Retry.
- Hardened the Retry UI creation path so 0 HP and FallZone retry can safely recreate/show the Retry panel before disabling Player control.

## 2026-05-24
- Assigned the MikoPurifySlash sprite to ShrinePrototype PlayerAttack and restored its facing setting so the attack effect follows the player's attack direction.
- Rebuilt ShrinePrototype EndGate as a simple torii-like placeholder made from red block pieces instead of a single red marker block.
- Reconnected PlayerVisualController in ShrinePrototype so the child PlayerVisual sprite flips with movement and attack direction after the jump-flip visual refactor.
- Added an optional PlayerJumpFlip visual effect in ShrinePrototype using a PlayerVisual child so jump flips rotate only the sprite while keeping the Player root, Rigidbody2D, and collider stable.
- Added minimal PlayerHealth to ShrinePrototype with 3 HP, contact damage handling, brief invincibility, hit flash feedback, small player knockback, and death triggering the existing Retry flow.
- Added temporary runtime HP text UI and updated GameManager retry reset so Player HP returns to full after retry.
- Updated GhostEnemy contact behavior so touching the Player deals 1 damage while PlayerHealth prevents frame-by-frame damage.
- Added GhostHealth with serialized maxHP, TakeDamage, Die, hit flash, and simple knockback feedback for Ghost enemies.
- Updated PlayerAttack and AttackHitbox so J attacks create a short-lived directional hitbox that damages GhostHealth.
- Added GhostSpawner and wired ShrinePrototype GhostTrainingArea to spawn the GhostEnemy prefab from three placed spawn points.
- Integrated temporary visual assets into ShrinePrototype: added the night shrine background behind the level, replaced the Player square with the fox-eared shrine maiden sprite, and replaced Ghost squares with the small ghost sprite.
- Set ShrinePrototype render order so the background is behind platforms, while the Player and Ghost enemies draw in front without changing movement, attack, collision, or retry behavior.
- Added the first ShrinePrototype ghost attack tutorial: Player can press J to spawn a short-lived placeholder hitbox, and Ghost enemies disappear when hit.
- Added a placeholder GhostEnemy prefab setup and three placeholder Ghost enemies under GhostTrainingArea before the EndGate.
- Updated ShrinePrototype into a platform-only tutorial layout with stone-path ground blocks, an easy SmallStep, a safe GapJump, three rising StairPlatforms, a spiritual RewardPlatform, StartPoint, FallZone, and EndGate.
- Kept the ShrinePrototype layout pass free of enemies and combat so it can validate traversal, falling, retry, and camera follow first.
- Created the NightApproach tutorial platformer scene using placeholder square blocks.
- Added StartArea, SmallStep, GapJump, three StairPlatforms, RewardPlatform, PlaceholderCharmPickup, EndGate blocks, and a level-wide FallZone under LevelPrototype.
- Kept PlayerController, CameraFollow, GameManager, and Retry UI support working in the new scene.
- Imported Night_shrine_1_background.png into Assets/Art/Backgrounds and added it as the NightApproach background.
- Extended NightApproach into a longer horizontal side-scrolling route with a longer final approach road and farther EndGate.
- Added the first basic combat tutorial: PlayerAttack, short-lived attack hitboxes on J, GhostEnemy floating behavior, a GhostEnemy prefab, and three Ghost enemies in NightApproach.
- Added first miko visual states for the Player: stand, run, jump, and attack pose switching.
- Created a transparent easy_ghost sprite variant and assigned it to Ghost enemies and the GhostEnemy prefab.
- Adjusted the NightApproach background placement and softened prototype platform colors so the stage reads more like one shrine road scene.
- Fixed the miko facing direction so movement and attack visuals match the player's input direction.
- Nudged the NightApproach background upward to better line up the painted stone road with the prototype platforms.
- Generated transparent miko sprite variants and updated the Player to use them, removing the baked white/checkerboard background.
- Generated a first MikoPurifySlash attack effect sprite and assigned it to the PlayerAttack hitbox visual.
- Fixed the MikoPurifySlash facing logic so the effect points in the player's attack direction.
- Added NightApproach to EditorBuildSettings.
- Updated TaskList.md with the new tutorial level progress.

## 2026-05-23
- Created the ShrinePrototype scene for the first playable prototype.
- Added one ground platform, one player square object, a main camera, and a global 2D light.
- Added visible Player and Ground prototype objects to SampleScene for immediate Play Mode testing.
- Added two jump test platforms and left/right air wall colliders to SampleScene and ShrinePrototype.
- Rebuilt ShrinePrototype into a longer horizontal platformer test level organized under LevelPrototype.
- Added labeled test sections: StartArea, SmallJump, GapJump, HighPlatform, RewardPlatform, LongGroundPath, and EndGate.
- Added CameraFollow.cs and attached it to the ShrinePrototype main camera.
- Added a basic fall-and-retry flow with StartPoint, FallZone, runtime Retry UI, and a minimal GameManager.
- Updated PlayerController.cs so player input can be paused and motion reset during retry.
- Added fall/retry testing objects to SampleScene so the currently open scene can be validated.
- Temporarily disabled prototype side air walls so falling can trigger the Retry UI.
- Replaced deprecated FindFirstObjectByType calls with FindAnyObjectByType.
- Added PlayerController.cs with serialized moveSpeed and jumpForce values.
- Implemented simple left/right movement, jumping, Rigidbody2D movement, and ground detection.
- Simplified PlayerController input handling and enabled both Unity input backends for safer prototype testing.
- Added a placeholder square sprite for prototype objects.
- Added ShrinePrototype to EditorBuildSettings.
- Updated TaskList.md for the first playable prototype progress.

## 2026-05-22
- Initialized Unity-style folder structure for the MVP.
- Added GameDesign.md with core concept, loop, MVP content, and boundaries.
- Added SystemDesign.md with scene flow, system responsibilities, and implementation order.
- Added TaskList.md with completed setup work, next tasks, backlog, and non-MVP items.
- Restored AGENTS.md project guidance in the workspace.
- Added Unity starter project metadata and a Unity .gitignore.
- Added placeholder ShopShrine, NightMap, and Result scenes.
- Added scene entries to EditorBuildSettings.
- Added folder tracking files for empty starter asset folders.
- Updated README with the current concept and core loop.
- Updated GameDesign.md with the latest concept, day/night loop, progression notes, MVP content, and design boundaries.
- Updated TaskList.md with the MVP environment and prototype checklist.
