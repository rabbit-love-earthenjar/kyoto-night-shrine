# Cafe Guest Visual Audit

Date: 2026-06-21

Scope:
- Checked runtime cafe guest frames under `Assets/Art/cafe_icon/guest_runtime`.
- Expected frame set per guest: `front/back/left/right` x `idle/walk_01/walk_02`.
- This audit does not modify art, import settings, or `.meta` files.

## Summary

Most current runtime guests have the full 12-frame set. The main confirmed issue is `priest`, which is missing left/right and front walk frames. A few visitors have noticeable bounding-box width variation, which can cause walking size jitter even when the cutout itself is usable.

## Guest Checklist

| Guest visual id | Frame status | Notes |
| --- | --- | --- |
| `child_girl_kimono` | 12/12 present | Width varies noticeably across directions. Keep runtime normalization on; inspect left/right side frames when polishing. |
| `girl_kimono` | 12/12 present | Looks usable in the audit sheet. |
| `gramma` | 12/12 present | Side-walk frames show small bright speckle/matte artifacts around the hair area. Good candidate for manual cleanup. |
| `kappa_yokai` | 12/12 present | Looks usable in the audit sheet. |
| `middle_aged_office_worker` | 12/12 present | Looks usable in the audit sheet. |
| `nekomata` | 12/12 present | Looks usable in the audit sheet. |
| `priest` | 4/12 present | Missing `front_walk_01`, `front_walk_02`, all left frames, and all right frames. Keep out of normal walking visitor use until completed. |
| `student_girl_uniform` | 12/12 present | Looks usable in the audit sheet. |
| `tanuki_yokai` | 12/12 present | Looks much more stable than before; keep checking side-view scale in Unity. |
| `traveler` | 12/12 present | Width varies because of backpack silhouette. Runtime normalization should help, but inspect side-walk jitter in scene. |

## Notes For Next Art Pass

- Do not use a global sprite auto-importer for these fixes.
- Prefer replacing individual PNG frames or cleaning source art manually.
- After replacing any frame, test that guest in `CafeInterior_Temporary` walking in and leaving.
- The runtime fallback system should warn instead of crashing when frames are missing.
