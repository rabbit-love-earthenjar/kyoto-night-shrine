# Stage 1-2 - Summer Festival Backstreet

## Names
- Scene: `Stage_1_2`
- Display name: `夏祭りの裏道`

## Goal
Create the first proper post-tutorial ACT stage while reusing the stable Stage 1-1 player, combat, retry, pause, reward, and stage-clear flow.

This first pass is a playable blockout. It introduces new small enemies without adding a boss, complex AI, boss materials, combo systems, or cafe systems.

## Structure
1. EntranceArea
   - Safe start copied from the stable Stage 1-1 setup.
   - No new enemy pressure near spawn.

2. PaperDollTutorialArea
   - Introduces `PaperDollEnemy`.
   - Uses near-ground patrol/chase behavior and 1 HP.
   - Rewards Faith Points only.

3. JumpGhostReviewArea
   - Reuses existing Ghost spawns to review Stage 1-1 combat.
   - Keeps ghosts away from pit edges.

4. RewardRouteArea
   - Keeps the optional upper route idea.
   - Heart and Faith Point pickups remain stage-only rewards.
   - One Paper Doll guards the optional route in this pass.

5. GhostLanternTutorialArea
   - Introduces `GhostLanternEnemy`.
   - Uses low-floating patrol/chase behavior, 2 HP, and slightly slower movement.
   - Rewards Faith Points only.

6. MixedChallengeArea
   - Mixes Paper Dolls, existing Ghosts, and Ghost Lanterns.
   - No boss and no unavoidable enemy stacking.

7. EndForeshadowArea
   - Reuses the EndGate flow from Stage 1-1.
   - Intended future visual direction: damaged lanterns, red spirit-fire hints, and summer festival anomaly foreshadowing.

## Enemy Plan
- Existing Ghost: 4 spawned from renamed review/mixed spawn points.
- Paper Doll: 5 direct scene enemies.
- Ghost Lantern: 2 direct scene enemies.
- Umbrella enemy is prepared as visual art only and is not used in Stage 1-2 yet.

## Rules
- Small enemies grant Faith Points only.
- Hearts remain immediate recovery pickups and are not stored.
- No boss materials, shards, or yokai materials drop in this pass.
- StarSeal objects copied from Stage 1-1 are disabled in the Stage 1-2 blockout.
- Stage 1-2 hides the StarSeal UI using the small `GameManager.showStarSealUi` switch.

## Acceptance Checklist
- Player can start from StartPoint and reach EndGate.
- Falling still triggers Retry.
- Esc pause menu still offers Resume and Return to Map.
- EndGate still triggers Stage Clear and Continue returns to `HubMap_Day`.
- Paper Doll and Ghost Lantern can be hit by J attack.
- Paper Doll and Ghost Lantern use GhostHealth hit flash, knockback, vanish, and Faith Point rewards.
- No boss, umbrella enemy, cafe system, or new material economy is added.
