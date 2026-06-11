# Changelog

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
