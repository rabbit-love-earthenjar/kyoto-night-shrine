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

## Next Tasks
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

## Not In MVP
- Large RPG progression.
- Multiple maps.
- Many enemies.
- Deep crafting.
- Branching story systems.
- Final art polish.
