# Game Design

## Concept
A 2D pixel-art ACT + simulation game set in modern Kyoto.

Players manage a small spiritual shrine/shop during the day and purify supernatural entities at night.

The demo should stay focused, playable, and atmospheric before it tries to become large.

## Tone
Quiet modern Kyoto with shrine alleys, rain, lanterns, vending machines, and a soft supernatural mood.

The player fantasy is practical spiritual work: helping visitors, preparing simple items, then going out at night to calm or purify spirits.

## Core Loop
Day:

- Manage shrine/shop.
- Talk with visitors.
- Craft spiritual items.

Night:

- Explore small Kyoto maps.
- Fight or purify spirits.
- Collect materials.

Progression:

- Improve shrine reputation.
- Unlock dialogue and tools.

## Stage 1 Route Prototype

The temporary Stage 1 route test uses a compact two-level path. The player starts at the upper-left torii, travels right along solid grass-block terrain, drops down at the far-right one-way descent, then returns from right to left along the lower route toward a temporary lower-left clear gate.

The route sketch uses black outlines for solid terrain and black vertical marks for spike hazards. Brown crates are breakable, the brown dashed platform is safe on its first crossing and drops immediately on the second entry, gray clouds disappear after being stood on and later recover, green markers are ground enemies, blue markers are flying enemies, and yellow stars use the existing StarSeal/talisman collectible system.

The ordinary route keeps the existing Kagura-bell melee combat. The giant Red Oni encounter and its temporary bean attack will be designed as a separate boss stage with background-scale boss presentation, rather than being attached directly to `Stage_1_1`.

## MVP Content
- One shrine/shop scene.
- One nighttime action map.
- One controllable player.
- Basic movement.
- Camera follow.
- Simple tilemap.
- One simple attack or purification action.
- Three item types.
- Three customer/request types.
- One enemy/spirit type.
- One result screen.

## Starter Item Types
- Paper Charm: basic request item and common purification tool.
- Incense Bundle: support item tied to calm/restoration requests.
- Salt Packet: simple protection or cleansing material.

## Starter Request Types
- Worried Resident: wants protection from strange noises or bad luck.
- Shop Owner: needs a charm to steady business after a supernatural disturbance.
- Shrine Visitor: requests cleansing after encountering a spirit at night.

## Starter Spirit Type
- Restless Wisp: a simple spirit that drifts toward the player and can be purified with the basic action.

## Design Boundaries
- Keep maps small.
- Use placeholder art where needed.
- Build the first playable loop before adding polish.
- Avoid complex branching story.
- Avoid large inventories, many enemy variants, advanced crafting, or deep RPG progression in the MVP.
