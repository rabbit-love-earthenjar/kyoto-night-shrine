# Cafe Guest Visual Audit

Date: 2026-08-25

Scope:
- Checked runtime cafe guest frames under `Assets/Art/cafe_icon/guest_runtime`.
- Expected frame set per guest: `front/back/left/right` x `idle/walk_01/walk_02`.
- PNG contents are not modified. Import metadata is changed only for individually confirmed inconsistent runtime frames; no broad importer or batch reimport is used.

## Summary

Most current runtime guests have the full 12-frame set. The main confirmed issue is `priest`, which is missing left/right and front walk frames. A few visitors have noticeable bounding-box width variation, which can cause walking size jitter even when the cutout itself is usable.

Runtime stabilization now keeps each visitor's movement root at a fixed scale and applies bounded frame normalization only to a `GuestSpriteVisual` child. Request bubbles remain on the stable root, so they no longer resize with walk frames. The common height reference is calculated from directional idle frames instead of the largest frame in the whole animation, and `gramma` / `traveler` no longer receive cross-direction width stretching.

The 2026-08-25 audit found 24 runtime frames across `gramma`, `traveler`, and `nekomata` imported as `Multiple / PPU 100`, while the rest of each matching animation set used `Single / PPU 128`. Those 24 existing `.meta` files are now aligned to `Single / PPU 128` without changing GUIDs or PNG contents. This removes the approximately 28% size jump that occurred when animation switched between differently imported directions.

## Guest Checklist

| Guest visual id | Frame status | Notes |
| --- | --- | --- |
| `child_girl_kimono` | 12/12 present | Width varies noticeably across directions. Keep runtime normalization on; inspect left/right side frames when polishing. |
| `girl_kimono` | 12/12 present | Looks usable in the audit sheet. |
| `gramma` | 12/12 present | All runtime frames now share `Single / PPU 128`. Side-walk frames still show small bright speckle/matte artifacts around the hair area and remain candidates for manual cleanup. |
| `kappa_yokai` | 12/12 present | Looks usable in the audit sheet. |
| `middle_aged_office_worker` | 12/12 present | Looks usable in the audit sheet. |
| `nekomata` | 12/12 present | All runtime frames now share `Single / PPU 128`; verify direction changes in Play Mode. |
| `priest` | 4/12 present | Missing `front_walk_01`, `front_walk_02`, all left frames, and all right frames. Keep out of normal walking visitor use until completed. |
| `student_girl_uniform` | 12/12 present | Looks usable in the audit sheet. |
| `tanuki_yokai` | 12/12 present | Looks much more stable than before; keep checking side-view scale in Unity. |
| `traveler` | 12/12 present | All runtime frames now share `Single / PPU 128`. Backpack width remains preserved instead of being squeezed to another direction's width. |

## Notes For Next Art Pass

- Do not use a global sprite auto-importer for these fixes.
- Prefer replacing individual PNG frames or cleaning source art manually.
- After replacing any frame, test that guest in `CafeInterior_Temporary` walking in and leaving.
- The runtime fallback system should warn instead of crashing when frames are missing.
