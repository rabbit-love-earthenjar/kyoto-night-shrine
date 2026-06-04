# Stage 01 Background Sections

## Scene
- Scene: `Assets/Scenes/Stage_1_1.unity`
- Parent object: `Stage01_Backgrounds`

## Purpose
Stage 1-1 uses three horizontal background sections to support the level's left-to-right progression. This is not a parallax setup and not a visual-depth layer setup.

## Hierarchy
```text
Stage01_Backgrounds
- BG_EarlySection
  - BG_EarlySection_Background
- BG_MiddleSection
  - BG_MiddleSection_Background
- BG_LateSection
  - BG_LateSection_Background
```

Some disabled background tiles may remain in the hierarchy from earlier setup work. They are not part of the active presentation.

## Current Art Assignments
- Early section: `Assets/Art/Backgrounds/stage_1_1_front.png`
- Middle section: `Assets/Art/Backgrounds/stage_1_1_middle.png`
- Late section: `Assets/Art/Backgrounds/stage_1_1_end.png`

## Section Intent
- Early section: beginning of the level, covering StartArea and JumpTutorialArea.
- Middle section: FirstCombatArea and RewardRouteArea, with a stronger shrine path and supernatural mood.
- Late section: MixedChallengeArea and EndArea, closer to the shrine gate and stage clear mood.

## Placement
- Early section is centered near `x = 20`.
- Middle section is centered near `x = 68`.
- Late section is centered near `x = 119` and uses a smaller scale so the final shrine image does not overpower the player and EndGate.
- Each active section uses sorting order `-40`, behind platforms, player, enemies, pickups, Retry, and EndGate.

## Rules
- Do not move gameplay colliders, enemies, pickups, Retry, EndGate, or player spawn when adjusting backgrounds.
- Do not create new scenes or new folders for this setup.
- Future swaps should keep the same three-section meaning unless the level layout changes.
