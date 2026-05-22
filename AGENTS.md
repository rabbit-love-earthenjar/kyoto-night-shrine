# AGENTS.md

## Project
This is a small 2D pixel-art ACT + simulation game set in Kyoto.
The player is a modern spiritual worker inspired by a Kyoto deity / Taoist exorcist protagonist.
The game combines daytime shop/shrine management with nighttime action-based purification.

## Core Direction
- Engine: Unity 2D
- Style: 2D pixel art, top-down or side-view, small maps first
- Mood: Kyoto night, shrine alleys, rain, lanterns, vending machines, quiet supernatural atmosphere
- Scope: small MVP first, not a large RPG
- Main loop:
  1. Daytime: manage shop/shrine, craft charms, talk to visitors
  2. Nighttime: explore one small Kyoto map, fight/purify spirits
  3. Rewards: collect materials and faith points
  4. Upgrade: unlock items, dialogue, and stronger tools

## MVP Scope
Only build the first playable demo:
- One shop/shrine scene
- One nighttime action map
- One controllable player
- Basic movement
- One simple attack or purification action
- Three item types
- Three customer/request types
- One enemy/spirit type
- One result screen
- Basic save/load if simple enough

Do not add large systems unless asked.

## Development Rules
- Prioritize playable prototype over visual polish.
- Use placeholder assets when needed.
- Keep scripts small and readable.
- Avoid over-engineering.
- Use clear Unity component names.
- Write comments for gameplay logic.
- Keep all values easy to tune in Inspector or ScriptableObject.
- Do not introduce paid assets or external services.

## Folder Structure
Use this structure when possible:

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
  GameDesign.md
  SystemDesign.md
  TaskList.md
  Changelog.md

## Documentation Requirements
When implementing features, update docs if relevant:
- GameDesign.md: concept, core loop, player experience
- SystemDesign.md: systems, data structure, UI flow
- TaskList.md: current tasks and next steps
- Changelog.md: completed changes

## Coding Style
- C# for Unity.
- Prefer descriptive names.
- Avoid giant manager scripts.
- Separate player control, combat, shop, dialogue, and UI logic.
- Use serialized fields instead of hard-coded values when possible.
- Keep MVP simple.

## Definition of Done
A task is done only when:
- The scene runs in Unity without errors.
- The feature can be tested manually.
- The code is readable.
- Related documentation is updated.
- No unrelated systems are changed.

## Do Not Do
- Do not expand the story into a huge RPG.
- Do not create dozens of enemies, items, or maps.
- Do not rewrite the whole project without reason.
- Do not focus on final art before the core loop works.
- Do not invent complex lore unless asked.
