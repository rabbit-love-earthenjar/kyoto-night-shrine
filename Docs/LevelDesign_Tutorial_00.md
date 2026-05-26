# Tutorial 00 - Basic Move

## Goal
Create a very short, safe movement tutorial for the current 2D platformer prototype.

Target clear time:
- First-time player: about 30 seconds

## Learning Goals
- Move right from a safe start.
- Make one forgiving jump.
- Follow Faith Points placed along the intended route.
- Collect one StarSeal-style clear goal.
- Reach a small torii-style EndGate.

## Scene
- `Assets/Scenes/Tutorial_00_BasicMove.unity`

## Structure
- `Tutorial_00_BasicMove_Level`
  - `Backgrounds`: night shrine background panels.
  - `Geometry`: short stone path, one low wooden step, and one safe landing path.
  - `Pickups`: FaithPoint arc and `StarSeal_01_ClearGoal`.
  - `Hazards`: `RetryZone_01` below the level.
  - `Goal`: `EndGate_00`.
  - `Notes`: `PlayerStart`.

## Pickups
- Faith Points guide the player from the start area through the jump arc.
- `StarSeal_01_ClearGoal` uses the StarSeal pickup mode and updates the temporary StarSeal UI.

## Acceptance Checklist
- [ ] Player starts at `PlayerStart`.
- [ ] Player can move right and clear the single simple jump.
- [ ] Faith Points can be collected.
- [ ] StarSeal can be collected.
- [ ] Falling below the level triggers Retry.
- [ ] EndGate triggers Stage Clear.
- [ ] No enemies or spikes are present.
- [ ] Console has no red errors.
