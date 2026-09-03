# Art And Scene Inventory

Generated: 2026-09-03 13:43:45 local time

## Snapshot

- Art textures: 1620
- Scene files: 15
- Enabled build scenes: 11
- Enabled build scenes with missing files: 0
- Art files with no serialized reference: 1411
- Art files sharing a filename: 879
- Textures larger than 4096 pixels on one axis: 0

Complete row-level lists are stored in `ArtAssetInventory.csv` and `SceneInventory.csv` beside this file.
Human purpose, flow, and acceptance decisions are tracked separately in `../SceneReviewChecklist.md` so regeneration never overwrites review notes.

## Audit Boundary

`SerializedReferenceCount` covers dependencies from scenes, prefabs, controllers, animations, materials, and ScriptableObject assets. A zero does not prove an asset is unused: resources loaded dynamically by code, editor-only source art, future art, and documentation references may legitimately report zero. Review before moving or deleting anything.

## Art Categories

| Category | Files | Referenced | No serialized reference | Duplicate names | >4096 px |
|---|---:|---:|---:|---:|---:|
| Backgrounds | 79 | 37 | 42 | 1 | 0 |
| boss | 18 | 9 | 9 | 0 | 0 |
| cafe_icon | 362 | 94 | 268 | 4 | 0 |
| farm_icon | 22 | 0 | 22 | 0 | 0 |
| material_store_icon | 5 | 0 | 5 | 3 | 0 |
| material_store_runtime | 6 | 6 | 0 | 4 | 0 |
| player_hit | 37 | 12 | 25 | 0 | 0 |
| puzzle | 1 | 0 | 1 | 0 | 0 |
| Root | 30 | 17 | 13 | 0 | 0 |
| Sprites | 866 | 0 | 866 | 866 | 0 |
| Spritesheets | 12 | 0 | 12 | 0 | 0 |
| stage_icon | 19 | 6 | 13 | 1 | 0 |
| start | 34 | 21 | 13 | 0 | 0 |
| Tools_icon | 14 | 5 | 9 | 0 | 0 |
| uber | 1 | 0 | 1 | 0 | 0 |
| UI | 2 | 2 | 0 | 0 | 0 |
| Vector | 112 | 0 | 112 | 0 | 0 |

## Scenes

| Scene | File | Build | Index | Size KB | Art deps | Script deps | Path |
|---|---|---|---:|---:|---:|---:|---|
| StartScene | Present | Enabled | 0 | 71.2 | 21 | 2 | `Assets/Scenes/StartScene.unity` |
| Stage_0_0 | Present | Enabled | 1 | 180.3 | 8 | 12 | `Assets/Scenes/Stage_0_0.unity` |
| NightApproach | Present | Enabled | 2 | 84.5 | 7 | 7 | `Assets/Scenes/NightApproach.unity` |
| Stage_1_1 | Present | Enabled | 3 | 310.8 | 12 | 15 | `Assets/Scenes/Stage_1_1.unity` |
| Stage_1_2 | Present | Enabled | 4 | 357.3 | 14 | 17 | `Assets/Scenes/Stage_1_2.unity` |
| Tutorial_00_BasicMove | Present | Enabled | 5 | 83.3 | 7 | 10 | `Assets/Scenes/Tutorial_00_BasicMove.unity` |
| Level_01_NightShrinePath | Present | Enabled | 6 | 270.4 | 8 | 15 | `Assets/Scenes/Level_01_NightShrinePath.unity` |
| HubMap_Day | Present | Enabled | 7 | 33.8 | 27 | 5 | `Assets/Scenes/HubMap_Day.unity` |
| CafeInterior_Temporary | Present | Enabled | 8 | 50.7 | 107 | 5 | `Assets/Scenes/CafeInterior_Temporary.unity` |
| Stage_1_Boss_RedOni | Present | Enabled | 9 | 102.8 | 12 | 17 | `Assets/Scenes/Stage_1_Boss_RedOni.unity` |
| Stage_1_Route_Prototype | Present | Enabled | 10 | 923.7 | 22 | 25 | `Assets/Scenes/Stage_1_Route_Prototype.unity` |
| LeftRoute_Prototype | Present | Not listed | -1 | 198.4 | 28 | 0 | `Assets/Scenes/LeftRoute_Prototype.unity` |
| NightMap | Present | Not listed | -1 | 4.6 | 0 | 0 | `Assets/Scenes/NightMap.unity` |
| Result | Present | Not listed | -1 | 4.6 | 0 | 0 | `Assets/Scenes/Result.unity` |
| ShopShrine | Present | Not listed | -1 | 4.6 | 0 | 0 | `Assets/Scenes/ShopShrine.unity` |
