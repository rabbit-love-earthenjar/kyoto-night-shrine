# Level 01 - Night Shrine Path

## Goal
Create the first short action-platformer level with beginner-friendly pacing, using the project's night shrine visual identity and existing prototype systems.

Target clear time:
- First-time player: about 2 minutes

## Learning Goals
- Follow Faith Points as route guidance.
- Cross easy gaps.
- Fight simple Ghost enemies.
- Learn fall Retry through safe pits.
- Break simple reward blocks.
- Avoid a forgiving spike section.
- Use one-way platforms to climb.
- Discover a hidden reward platform.
- Trigger one ambush Ghost through a simple trigger zone.
- Collect StarSeals and reach the EndGate.

## Scene
- `Assets/Scenes/Level_01_NightShrinePath.unity`

## Section Breakdown
- `StartArea`: safe flat stone path with `PlayerStart` and Faith Points in a line.
- `JumpArea`: two easy gaps, a low wooden platform, a slightly higher wooden platform, and Faith Points in jump arcs.
- `FirstEnemyArea`: flat combat space with `AttackTutorialTrigger_01`, `AttackTutorialMarker_J`, `GhostSpawner_Patrol`, and two patrol-style Ghost spawn points.
- `BreakableBlockArea`: three `BreakableBlock` objects that grant Faith Points when attacked.
- `HazardArea`: two small `SpikeHazard` objects on a forgiving flat route.
- `VerticalPlatformArea`: three one-way wooden platforms with Faith Points guiding the climb.
- `HiddenRewardArea`: a cloud hint plus `HiddenPlatform_01`, `HeartPickup_01_HiddenRoute`, and `StarSeal_02_HiddenReward`.
- `TriggeredEnemyArea`: `AmbushTrigger_01` starts `GhostSpawner_Ambush`, which spawns one Ghost.
- `EndArea`: final safe stone stretch with `StarSeal_03_NearEndGate`, final Faith Point guidance, and `EndGate_01`.

## Enemy Behavior
- `Ghost_Patrol_01` and `Ghost_Patrol_02`: spawned on start by `GhostSpawner_Patrol`; they use the existing simple hover movement.
- `Ghost_Ambush_01`: spawned only after the player enters `AmbushTrigger_01`.
- The scene keeps active enemies low and does not add a new health, boss, or AI system.

## Collectibles
- Faith Points guide movement and are collectible through the existing `PickupItem` FaithPoint mode.
- StarSeals use the existing `PickupItem` extension for key-style stage collectibles:
  - `StarSeal_01_AfterFirstEnemy`
  - `StarSeal_02_HiddenReward`
  - `StarSeal_03_NearEndGate`
- `HeartPickup_01_HiddenRoute` restores 1 HP up to max HP.
- `OmamoriPlaceholder_01_BehindBlocks` is visual only for now.

## Acceptance Checklist
- [ ] Player starts from `PlayerStart`.
- [ ] Player can reach `EndGate_01` without mandatory damage.
- [ ] Faith Points can be collected and update the UI.
- [ ] Three StarSeals can be collected and update the temporary StarSeal UI.
- [ ] Heart pickup restores 1 HP without exceeding max HP.
- [ ] Falling below pits triggers Retry.
- [ ] Patrol Ghosts spawn in the first enemy area.
- [ ] `AttackTutorialTrigger_01` and `AttackTutorialMarker_J` are placed before the first patrol Ghosts.
- [ ] Ambush Ghost spawns after entering `AmbushTrigger_01`.
- [ ] Breakable blocks reward Faith Points when attacked.
- [ ] Spikes damage the player without instant HP loss.
- [ ] One-way platforms allow upward climbing and landing from above.
- [ ] EndGate triggers Stage Clear.
- [ ] Console has no red errors.
