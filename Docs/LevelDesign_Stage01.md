# Stage 01 - Night Approach

Japanese display name: 月下参道

## Goal
Create a beginner-friendly nighttime Kyoto shrine approach tutorial level that teaches movement, jumping, falling/retry, basic ghost combat, optional reward routing, Heart recovery, and EndGate clear.

Target clear time:
- First-time player: 4 to 6 minutes
- Skilled replay: 2 to 3 minutes

## Section Breakdown
- StartArea: safe 12-unit flat stone path with StartPoint and movement/jump sign.
- JumpTutorialArea: two forgiving gaps, two low wooden platforms, one slightly higher wooden platform, and safe stone landings.
- FirstCombatArea: flat stone combat lane with two ghost spawn points and an Attack: J sign.
- RewardRouteArea: safe main stone path plus optional upper spiritual cloud route. The upper route contains one Heart pickup and three Faith Point pickups.
- MixedChallengeArea: beginner-friendly broken stone platforms, two wooden safety steps, small gaps, and three ghost spawn points.
- EndArea: safe final stone stretch ending at the torii-style EndGate.

## Enemy Spawn List
- GhostSpawn_C1: FirstCombatArea, easy front encounter.
- GhostSpawn_C2: FirstCombatArea, second easy encounter.
- GhostSpawn_E1: MixedChallengeArea, low ground encounter.
- GhostSpawn_E2: MixedChallengeArea, higher platform encounter.
- GhostSpawn_E3: MixedChallengeArea, final mixed challenge encounter.

Total Stage 01 ghost count: 5. Small ghosts reward Faith Points through the existing GhostHealth reward flow.

## Pickup List
- HeartPickup_Reward01: upper RewardRouteArea cloud platform, restores 1 HP up to max HP.
- FaithPointPickup_Reward01: upper reward route collectible, grants 1 Faith Point.
- FaithPointPickup_Reward02: upper reward route collectible, grants 1 Faith Point.
- FaithPointPickup_Reward03: upper reward route collectible, grants 1 Faith Point.

Note: Small Ghost enemies still reward Faith Points through GhostHealth. Reward-route Faith Point pickups use PickupItem in FaithPoint mode.

## Reward Route
The main route continues safely at ground level. The optional route rises through a wood entry step and three pale blue spiritual cloud platforms, then returns through wood steps to the main path. The route is slightly harder than the main route but remains forgiving and has no softlock.

## Acceptance Checklist
- [ ] Player starts from StartPoint and can reach EndGate.
- [ ] Player can clear all required jumps.
- [ ] Falling below the level triggers Retry.
- [ ] Retry returns player to StartPoint.
- [ ] Player can fight ghosts in combat areas.
- [ ] Ghosts spawn from the five listed spawn points.
- [ ] Small ghosts reward Faith Points.
- [ ] Reward route Faith Point pickups update the Faith Points UI.
- [ ] Reward route contains HeartPickup_Reward01.
- [ ] Heart pickup restores HP without exceeding max HP.
- [ ] EndGate triggers Stage Clear.
- [ ] Console has no red errors.
- [ ] No unrelated systems were modified.
