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

## Next Tasks
- Open Tutorial_00_BasicMove and test movement, the single jump, FaithPoint pickups, StarSeal pickup, RetryZone, and EndGate.
- Open Level_01_NightShrinePath and test full traversal, FaithPoint pickups, three StarSeals, Heart pickup, patrol Ghosts, ambush Ghost trigger, breakable blocks, spike damage, one-way platforms, Retry, and EndGate.
- Audit miko standing/running/jumping/attack art so all transparent gameplay sprites share a consistent default facing direction and no portrait-style white-background images are used in gameplay.
- Open Stage_1_1 or Stage_0_0 and test full traversal, fall Retry, ghost spawning, J-key attack, Heart pickup, Faith Point pickups, Faith Point rewards from ghosts, SealGhost StarSeal drops, and EndGate Stage Clear.
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

## Not In MVP
- Large RPG progression.
- Multiple maps.
- Many enemies.
- Deep crafting.
- Branching story systems.
- Final art polish.
