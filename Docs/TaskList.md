# Task List

## Current Goal
Create the Unity 2D MVP foundation and then build the first playable prototype step by step.

## MVP Tasks

### Environment
- [x] GitHub repository
- [x] AGENTS.md
- [x] Codex connection
- [x] Unity project creation

### Prototype
- [x] Player movement
- [x] Camera follow
- [ ] Tilemap
- [x] Shrine scene
- [x] Basic platform blockout
- [x] Side air walls
- [x] Temporarily disabled air walls for fall/retry testing
- [x] Long horizontal platformer test level
- [x] NightApproach tutorial platformer level
- [x] Extended NightApproach into a longer side-scrolling route
- [x] Added NightApproach background image
- [x] Basic fall and retry flow
- [x] Basic combat tutorial with player attack and Ghost enemies
- [x] Connected first miko and ghost art to the NightApproach prototype
- [x] Adjusted prototype platform/background visual scale
- [x] ShrinePrototype tutorial level layout blockout
- [x] ShrinePrototype first ghost attack tutorial
- [x] Integrated temporary visual assets into ShrinePrototype
- [x] Added basic GhostHealth feedback and GhostSpawner
- [x] Added minimal PlayerHealth and HP UI
- [x] Replaced temporary HP text with three-heart UI
- [x] Added FallZone retry fallback and EndGate success popup
- [x] Revised rewards so small Ghost enemies grant Faith Points and optional route pickups use Hearts
- [x] Created Stage01_NightApproach tutorial level layout under Stage01_Level
- [x] Made Stage 01 reward route Faith Point pickups collectible and synced the layout back into ShrinePrototype
- [x] Added the night shrine background image to Stage01_NightApproach and ShrinePrototype
- [x] Renamed ShrinePrototype to Stage_0_0 and Stage01_NightApproach to Stage_1_1
- [x] Added looping night shrine BGM and core prototype SFX hooks
- [x] Added Tutorial_00_BasicMove as a short movement tutorial scene
- [x] Added Level_01_NightShrinePath as the first short beginner-friendly platformer level
- [x] Added StarSeal pickup support and temporary StarSeal UI
- [x] Added Stage_1_1 SealGhost enemies that drop StarSeal rewards through combat
- [x] Added minimal ResourceInventory for stored Faith Points and future material counts
- [x] Routed Faith Point UI through ResourceInventory as the single stored source of truth
- [x] Created HubMap_Day Phase 1 visual scene skeleton
- [x] Added basic HubMap_Day player movement and cleaned icon cutouts
- [x] Added simple four-direction HubPlayer sprite switching and walk frames
- [x] Adjusted HubMap_Day building icon placement and scale for clearer map readability
- [x] Added HubMap_Day Phase 2 lightweight building interaction panels
- [x] Moved the HubMap_Day warehouse icon to the lower-left clearing
- [x] Added Stage Clear Continue flow into HubMap_Day
- [x] Added minimal HubMap_Day ruined shrine repair action
- [x] Added temporary cafe interior scene after shrine repair
- [x] Strengthened HubMap_Day shrine and warehouse visual presence with larger sprites and depth shadows
- [x] Furnished CafeInterior_Temporary with a counter, fox altar, table sets, sofas, and light decorations
- [x] Added CafeInterior_Temporary looping BGM and replaced the ACT attack SFX with the new bell sound
- [x] Added simple collision-aware RPG movement for HubMap_Day and CafeInterior_Temporary
- [x] Added lightweight CafeInterior_Temporary furniture collision for the counter, altar, tables, and sofas
- [x] Added minimal breakable block, spike hazard, one-way platform, and trigger-spawner support for Level 01
- [ ] Day/Night cycle

## Completed
- Created base Unity folder structure.
- Added project documentation files.
- Defined MVP scope, systems, content limits, and first implementation order.
- Added Unity starter metadata, placeholder scenes, and build scene list.
- Added tracking files for empty starter asset folders.
- Added the core concept and loop to the README and game design docs.
- Created the first playable ShrinePrototype scene.
- Added a simple ground platform, player square, main camera, and global 2D light.
- Added PlayerController.cs with left/right movement, jump, Rigidbody2D movement, and ground detection.
- Added the same visible Player and Ground prototype objects to the current SampleScene for immediate Play Mode testing.
- Added two jump test platforms and left/right air wall colliders to SampleScene and ShrinePrototype.
- Rebuilt ShrinePrototype as a longer horizontal platformer test level under LevelPrototype.
- Added StartArea, SmallJump, GapJump, HighPlatform, RewardPlatform, LongGroundPath, and EndGate objects.
- Added CameraFollow.cs and attached it to the ShrinePrototype main camera.
- Added a StartPoint, FallZone, minimal GameManager, and Retry UI for basic fall recovery.
- Added the same fall/retry testing setup to SampleScene and temporarily disabled prototype air walls.
- Created NightApproach as the first tutorial platformer level with StartArea, SmallStep, GapJump, StairPlatforms, RewardPlatform, charm pickup, EndGate, and FallZone.
- Extended NightApproach further to the right and added the imported night shrine background image.
- Added PlayerAttack, AttackHitbox, GhostEnemy, a GhostEnemy prefab, and three Ghost enemies to NightApproach.
- Added miko standing/running/jumping/attack sprite switching for the Player.
- Added a transparent easy_ghost sprite variant for Ghost enemies.
- Raised the NightApproach background and softened platform block colors so the test level better matches the shrine road image.
- Reworked ShrinePrototype into a platform-only tutorial layout with stone-path ground, easy jumps, three rising platforms, a spiritual reward platform, StartPoint, FallZone, and EndGate.
- Added a first GhostTrainingArea to ShrinePrototype with placeholder Ghost enemies and a J-key attack hitbox that destroys ghosts on hit.
- Integrated the night shrine background, fox-eared shrine maiden player sprite, and small ghost sprite into ShrinePrototype while keeping gameplay colliders and scripts unchanged.
- Added basic GhostHealth damage handling, hit flash, knockback feedback, and a GhostSpawner that creates ghosts from placed spawn points in ShrinePrototype.
- Added minimal PlayerHealth with 3 HP, invincibility after contact damage, simple hit flash, HP text UI, and Retry trigger on death.
- Added a minimal reward prototype with Faith Points from small Ghost enemies and Heart healing pickups in ShrinePrototype.
- Created Stage01_NightApproach as the first structured tutorial level layout with StartArea, JumpTutorialArea, FirstCombatArea, RewardRouteArea, MixedChallengeArea, EndArea, FallZone, Heart reward, ghost spawn points, and EndGate.
- Updated PickupItem so reward objects can be either Hearts or Faith Points, then connected the Stage 01 FaithPointPickup_Reward objects.
- Created Tutorial_00_BasicMove with a safe start, one simple jump, Faith Point guidance, one StarSeal clear goal, RetryZone, and EndGate.
- Created Level_01_NightShrinePath with Start, Jump, First Enemy, Breakable Block, Hazard, Vertical Platform, Hidden Reward, Triggered Enemy, and End sections.
- Added small support components for breakable reward blocks, simple spike damage, one-way platforms, and trigger-based Ghost spawning.
- Added a lightweight ResourceInventory so stage Faith Point rewards can be stored for future cafe systems while Hearts remain immediate recovery pickups.
- Updated GameManager so Faith Point rewards still use the existing public entry point but store and read the value from ResourceInventory.
- Created HubMap_Day as the first daytime hub map skeleton with a grass field background, cleaned ruined shrine icon, cleaned warehouse icon, movable RPG player placeholder, UI group, and interaction point placeholders.
- Moved the ruined shrine icon to the central clearing and enlarged the hub building icons for better visual scale.
- Added lightweight HubMap_Day click panels: the warehouse shows stored Faith Points and BasicYokaiMaterial, while the ruined shrine shows a placeholder repair requirement.
- Moved the warehouse icon into the lower-left clearing so the hub has a clearer shrine/warehouse/future-building layout.
- Added a Continue button to the Stage Clear popup so cleared ACT stages can load HubMap_Day.
- Added the first minimal ruined shrine repair action: spending 10 Faith Points marks the shrine as repaired for the current hub session.
- Added CafeInterior_Temporary with the temporary cafe background, RPG player movement, and a return-to-Hub button.
- Connected the repaired shrine action in HubMap_Day so it can enter CafeInterior_Temporary after repair.
- Enlarged the HubMap_Day shrine and warehouse icons and added subtle offset depth shadows so the shrine remains the main daytime-map focus.
- Added CafeInterior_Temporary furniture and decor: cafe counter, fox altar, two table sets, two sofa sets, menu board, side cabinet, and plant while preserving a central walking route.
- Added `Midnight Matcha Shift.mp3` as CafeInterior_Temporary BGM and changed the ACT attack SFX to `bell.wav`.
- Added lightweight collision-aware RPG movement so HubMap_Day and CafeInterior_Temporary players can walk around solid props.
- Added simple furniture colliders to the CafeInterior_Temporary counter, altar, tables, and sofas while keeping the center aisle open.

## Next Tasks
- Open Tutorial_00_BasicMove and test movement, the single jump, FaithPoint pickups, StarSeal pickup, RetryZone, and EndGate.
- Open Level_01_NightShrinePath and test full traversal, FaithPoint pickups, three StarSeals, Heart pickup, patrol Ghosts, ambush Ghost trigger, breakable blocks, spike damage, one-way platforms, Retry, and EndGate.
- Audit miko standing/running/jumping/attack art so all transparent gameplay sprites share a consistent default facing direction and no portrait-style white-background images are used in gameplay.
- Open Stage_1_1 or Stage_0_0 and test full traversal, fall Retry, ghost spawning, J-key attack, Heart pickup, Faith Point pickups, Faith Point rewards from ghosts, SealGhost StarSeal drops, and EndGate Stage Clear.
- After Stage Clear in Stage_1_1 or Stage_0_0, click Continue and confirm HubMap_Day loads with stored Faith Points available in the warehouse panel.
- Confirm Faith Points from pickups, breakable blocks, and small Ghost enemies update both the UI and ResourceInventory storage.
- Open HubMap_Day and check the daytime map background, central ruined shrine icon, lower-left warehouse icon, and HubPlayer are visible; use WASD or arrow keys to move the HubPlayer in all four directions.
- In HubMap_Day, click the warehouse icon and confirm the resource panel shows Faith Points and BasicYokaiMaterial only; click the ruined shrine icon and confirm the placeholder repair panel appears.
- In HubMap_Day, collect or grant at least 10 Faith Points, click the ruined shrine, repair it, and confirm the warehouse panel shows Faith Points reduced by 10.
- After repairing the shrine in HubMap_Day, click the shrine again, enter CafeInterior_Temporary, move the RPG player inside the cafe, and use the top-right button to return to HubMap_Day.
- In CafeInterior_Temporary, inspect the furniture scale and spacing and confirm the player can walk from the entrance through the open center aisle.
- In CafeInterior_Temporary, confirm CafePlayer slides along the counter, tables, sofas, and altar without blocking the open center aisle.
- In Stage_1_1 and Stage_0_0, confirm BGM loops softly and SFX play for jump, land, attack, hurt, pickup, ghost vanish, retry, and stage clear.
- Open Stage_0_0 and retest HP UI, ghost contact damage, invincibility timing, death Retry, fall retry, ghost spawning, J-key attack effect direction, EndGate visibility/reachability, and Stage Clear popup.
- Open NightApproach and test movement, jump teaching, miko sprite switching, Ghost attacks with J, EndGate, and fall/retry.
- Add a simple tilemap for the first shrine/night test area.
- Add a basic day/night cycle only after the scene and movement work.

## Backlog
- Add one basic purification action.
- Add one Restless Wisp enemy prefab.
- Add simple request/item data.
- Add basic UI for shop, night, and result flow.
- Add minimal save/load after the loop is playable.
- Add placeholder pixel-art tiles and props.
- Add simple audio cues for shop, rain, and purification.
- Tune movement, purification range, and rewards.
- Reserve shards, yokai materials, blue energy, and combo systems for later enemy tiers and boss content.
- Connect ResourceInventory to the cafe scene when cafe management begins.
- Add persistence for repaired hub buildings after the prototype hub loop is stable.
- Rename `cofee_front.png` to a corrected cafe background name during a later art naming cleanup.

## Not In MVP
- Large RPG progression.
- Multiple maps.
- Many enemies.
- Deep crafting.
- Branching story systems.
- Final art polish.
