# Changelog

## 2026-05-29
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
