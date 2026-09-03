# Scene Review Checklist

Last reviewed: 2026-09-03

This checklist is the human review layer for `Docs/Generated/SceneInventory.csv`. The generated inventory records objective file, dependency, and Build Settings data; this file records each scene's intended role and acceptance status. Update the generated inventory first whenever scenes are added, removed, or reordered.

## Current Story Flow

`StartScene` -> `Stage_1_2` -> `HubMap_Day` with the cafe locked and warehouse available -> explicit shrine repair -> `CafeInterior_Temporary` -> night patrol routes -> `Stage_1_Route_Prototype` -> `Stage_1_Boss_RedOni`

Scene transitions use scene names rather than Build Index values.

## Main Flow Scenes

| Scene | Build | Intended role | Current review status | Next manual check |
|---|---:|---|---|---|
| `StartScene` | 0 | Title, New Game, Continue, settings entry | Connected | New Game enters `Stage_1_2`; Continue resumes the latest safe autosave |
| `Stage_1_2` | 4 | Current opening playable combat stage | Connected; automated combat/flow checks pass | Complete the route with normal input and confirm 3/5 HP enemy pacing |
| `HubMap_Day` | 7 | First daytime Hub and progression junction | Connected | First arrival shows ruined cafe, usable warehouse, and explicit repair flow |
| `CafeInterior_Temporary` | 8 | Current cafe operation scene | Connected, temporary presentation | Verify counter machine trigger, guest bubbles, service loop, and return summary |
| `Stage_1_Route_Prototype` | 10 | Red Oni route leading to the challenge prompt | Connected as an isolated prototype | Traverse the full upper/lower route and accept the Boss challenge |
| `Stage_1_Boss_RedOni` | 9 | Dedicated multi-phase Red Oni Boss battle | Connected | Complete all phases with real aiming, Retry, BGM transitions, and Final Rush |

## Retained Test And Earlier Stage Scenes

| Scene | Build | Intended role | Current review status | Decision needed |
|---|---:|---|---|---|
| `Stage_0_0` | 1 | Combat-practice stage with breakable targets | Available from night stage select | Keep while it remains a useful combat sandbox |
| `NightApproach` | 2 | Early platform-route prototype | Enabled legacy scene | Compare with `Stage_1_1`; remove from Build Settings only after confirming no unique test path remains |
| `Stage_1_1` | 3 | Earlier complete ACT route and Red Oni builder source | Available from night stage select | Keep as a stable comparison scene until the new route is accepted |
| `Tutorial_00_BasicMove` | 5 | Standalone short movement tutorial | Retained safe scene; no longer New Game target | Decide later whether its lessons move into the opening story flow |
| `Level_01_NightShrinePath` | 6 | Compact beginner platform/combat validation level | Enabled test scene | Keep until its useful mechanics are merged or formally archived |

## Unconnected Drafts And Placeholders

| Scene | Build | Intended role | Current review status | Decision needed |
|---|---:|---|---|---|
| `LeftRoute_Prototype` | Not listed | Unconnected left-route visual draft | Preserve for visual reference | Review before merging any composition into a formal route |
| `NightMap` | Not listed | Empty legacy placeholder | Not connected | Archive or delete only after a deliberate cleanup pass |
| `Result` | Not listed | Empty legacy result placeholder | Not connected; current results are in-scene UI | Archive or repurpose after result-flow design is approved |
| `ShopShrine` | Not listed | Empty legacy shop/shrine placeholder | Not connected | Archive or repurpose after Hub/cafe ownership is finalized |

## Review Rules

- Do not infer that an unreferenced scene or art asset is safe to delete solely from serialized reference counts.
- Keep the Red Oni route and Boss arena separate; changing route presentation must not overwrite the Boss scene.
- Test New Game and Continue from `StartScene` after any save-flow or Build Settings change.
- Run the relevant automated validator first, then perform the listed manual path in Play Mode before marking a scene accepted.
- Record screenshots and test notes under `Logs/` or the relevant level-design document.

## Immediate Review Queue

- [ ] New Game: `StartScene` -> `Stage_1_2`.
- [ ] Opening clear: `Stage_1_2` -> locked-cafe `HubMap_Day` with warehouse available.
- [ ] Cafe unlock: repair interaction -> `CafeInterior_Temporary`.
- [ ] Red Oni route: full upper/lower traversal -> grounded challenge prompt.
- [ ] Boss: all phases, Retry, BGM replacement, result, and return flow.
- [ ] Decide whether `NightApproach`, `Tutorial_00_BasicMove`, and `Level_01_NightShrinePath` remain shipping Build scenes.
