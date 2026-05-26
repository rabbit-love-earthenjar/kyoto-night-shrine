# System Design

## Project Structure
The project follows a small Unity 2D layout:

```text
Assets/
  Art/
  Audio/
  Prefabs/
  Scenes/
  Scripts/
    Core/
    Player/
    Combat/
    Shop/
    Dialogue/
    UI/
  ScriptableObjects/
  Tilemaps/

Docs/
```

## Scene Flow
The MVP should use a simple scene flow:

1. Shop/Shrine Scene
2. Night Map Scene
3. Result Screen

Starter placeholder scene files:

- `Assets/Scenes/ShopShrine.unity`
- `Assets/Scenes/NightMap.unity`
- `Assets/Scenes/Result.unity`

Scene transitions can begin as direct button or trigger-driven changes. A more advanced calendar, clock, or scheduler should wait until the prototype proves the loop.

## Core Systems

### Player
- Handles 2D movement.
- Owns the basic purification action.
- Uses inspector-tuned values for speed, action range, and cooldown.
- Uses a small visual controller for prototype sprite states instead of a full animation graph.

### Prototype Retry
- A small GameManager pauses player control when the player enters the FallZone.
- The temporary Retry UI resets the player to StartPoint and then restores movement.
- This should stay lightweight until checkpoints or lives are actually needed.

### Combat/Purification
- One player action affects one enemy/spirit type.
- The first version can use simple collision or trigger checks.
- Damage, purification strength, or cooldown should stay easy to tune.
- The first combat tutorial uses `J` to spawn a short-lived attack hitbox in front of the player.
- Ghost enemies use simple hovering movement and disappear when hit; no health or advanced AI yet.
- Ghost visuals use the transparent `easy_ghost` variant while keeping the original source image unchanged.

### Reward Hierarchy
- Faith Points are the basic currency and the first reward type used by small enemies.
- Small Ghost enemies should grant Faith Points directly on defeat.
- StarSeals are key-style stage collectibles for tutorial and level goals. They are count-only in the prototype and should not become currency or crafting material.
- Hearts are temporary recovery pickups placed on optional reward platforms or hidden routes.
- Hearts restore 1 HP up to the player's max HP and disappear when collected.
- Shards and yokai materials are reserved for stronger enemies, boss spirits, or later progression systems.
- The blue energy meter, combo system, and ultimate-style actions are planned for later and are not part of the current prototype.

### Platformer Setpieces
- Breakable blocks are simple attack targets that grant small rewards immediately when destroyed.
- Spike hazards deal 1 damage through PlayerHealth and rely on existing invincibility to prevent instant HP loss.
- One-way platforms use a minimal collision helper so the player can jump through from below and land from above.
- Triggered Ghost spawners are used for small tutorial ambushes without adding a new enemy AI system.

### Prototype Audio
- `GameAudio` is the lightweight scene audio component for the action prototype.
- Stage scenes can assign one looping BGM clip and one-shot SFX clips through serialized fields.
- BGM should stay low, around 0.15 to 0.25 volume.
- SFX should use `PlayOneShot` and remain null-safe so missing clips do not break gameplay.
- Current hooks cover jump, landing, attack, player hurt, pickups, Ghost vanish, Retry fall, Stage Clear, and spike hazards.

### Shop/Requests
- Stores a small list of three request types.
- Tracks needed item type and reward values.
- Completes requests through simple UI interactions.

### Items
- Three item types are enough for the MVP.
- Data can start as simple serializable classes or ScriptableObjects.
- Avoid complex inventory rules until the loop is playable.

### Results
- Shows completed requests, spirits purified, materials gained, and faith points gained.
- Provides a clear way to return to the shop/shrine scene.

### Save/Load
- Optional for the first pass.
- If included, save only minimal progress: faith points, materials, and basic unlocked state.
- Use Unity-friendly local persistence such as PlayerPrefs or a small JSON file.

## UI Flow
- Shop UI: current requests, available items, start night button.
- Night UI: player health/status if needed, material count if useful.
- Result UI: rewards and return button.

## Data Notes
- Keep gameplay values exposed through serialized fields or ScriptableObjects.
- Prefer small, readable components over large manager scripts.
- Keep systems independent enough that shop, player, combat, and UI can be adjusted separately.

## Initial Implementation Order
1. Create scenes and placeholder objects.
2. Add player movement.
3. Add basic purification action.
4. Add one spirit behavior.
5. Add shop requests and item data.
6. Add result screen.
7. Add minimal save/load only if the loop is stable.
