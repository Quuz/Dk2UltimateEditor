# Remaining main-map overlay source requirements

Read-only source audit, 2026-10-07. The subsequent [overlay implementation and evidence](MAP_OVERLAYS.md) now follows these requirements; this audit alone is not an implementation or completion claim. Terrain/zoom is covered by [its separate stage](TERRAIN_ZOOM.md); the physical object subset is separately implemented. The source requirements below remain evidence rather than a substitute for actual control/render/game acceptance.

## Layer order, flags and defaults

`Windows_MainWindowProc.c` WM_PAINT draws terrain; doors; traps; specials; ordinary objects; creature counts; effects; gates; areas; paths; cameras; selection; pointer/cursor layers. Areas must not arbitrarily move behind effects or cameras.

`Config.h` has fifteen flags: terrain 1, creatures 2, doors 4, traps 8, objects 16, specials 32, areas 64, gates 128, paths 256, effects 512, cameras 1024, detailed 65536, expanded gates 131072, object owner colours 262144 and special owner colours 524288. `Properties_Editor_Config.c` enables the first eleven category layers plus detailed by default; expanded gates and the two owner-colour options default off. The original path colour is COLORREF **0x00FFFFFF (white)**, not the current approximate light blue. Persisted unknown flag bits must survive without being interpreted as verified new categories. Existing modern entity/path toggles and previous object/terrain preferences need deliberate backward-compatible migration and consistent actual controls, not competing unsynchronised state.

## Region labels and rectangles

`Things_DrawAreas` / `Things_DrawEffects` call `Map_DrawRectangle`, which skips `rect.hidden`, normalizes reversed inclusive endpoints for display, culls wholly offscreen rectangles and uses the source last pixel `(x2+1)*zoomX-1`, `(y2+1)*zoomY-1`. It draws names before a one-pixel boundary. The name box is the rectangle clipped to the viewport, inset three pixels. The initial black text at +3/+3 is followed by `textcolor` text at +2/+2, producing a one-pixel black offset shadow. `DrawText(DT_LEFT)` wraps inside the available rectangle; this is not centred text or a bounding-box tooltip.

`Files_Game.c` supplies native colour/text-colour pairs: areas 0x00FFFF00, effects 0x007F00FF, gates 0x00AAAAAA. `Files_Editor.c` reconnects `rect.name` to the record name and masks text colour to 24 bits; project hidden/name/text-colour fields remain real editable values, not values synthesized into map bytes. Malformed imported extents must be retained and projected safely. Native/project naming comes from the actual fields/source loader; raw pointers in serialized editor rectangles cannot be treated as live strings.

## Gate expansion

`Things_DrawGates` begins with a one-tile rectangle at the gate origin, even if a stored project rectangle has another second endpoint. Expanded view calls `Things_DrawGatesExpand` only when the origin's editor tile belongs to Gates (group 2). It scans matching **editor catalogue slot** left/right on the original row, then scans up at the resulting left edge and down at the resulting right edge. It compares tile slot and group, not owner; distinct oriented variants must not merge. This is not flood fill, a region union, a connected-component bounding box, or a test of every rectangle interior cell.

The right/bottom tests `if (!*X2 >= width)` / `if (!*Y2 >= height)` are unsafe precedence/boundary bugs. A safe equivalent must test the next coordinate before reading, retain the intended ordered scan for valid data, and never cross row edges/read past the map. Unknown/non-Gates terrain or invalid origins should remain a guarded point/skip, not an automatic terrain or region repair. Focused cases must distinguish the ordered edge scan from a tempting bounding-box approximation, including owner changes, variant changes, irregular strips and all map borders.

## Paths

`Things_DrawPaths` draws one-pixel configurable-colour links from corrected rectangle centres using integer division by two. A self-link is an ellipse inset by integer `(pixel extent)/9` on each side. Duplicate/missing modern native IDs need safe nonmutating guards rather than arbitrary target selection. Visibility of links is independent of area rectangle visibility. The current floating 7/18 radius is an approximation of the integer source ellipse and must not count as exact raster/geometry parity. No arrowhead is an original requirement.

## Keeper cameras

`Map_Cameras.c` uses only five keeper rows, corresponding to actual player IDs 3–7. Resources 20–24 resolve to the five preserved Keeper*Camera.ico files; `Map_CreateCameras` requests **16×16**, independent of cell zoom. Stored X=-1 or Y=-1 suppresses a camera: a project source-default camera is not the compiler's effective heart camera. Historical player reader rules already discard old camera fields and must remain in effect. Unknown native layout, duplicate/out-of-range identity and out-of-map coordinates need safe omission, not guessed camera repair.

The icon origin is tile origin plus integer `(zoomX-16)/2`, `(zoomY-16)/2`; negative division must use C-style truncation toward zero. The base two-pixel owner-colour ellipse has radius two cells around icon-origin +8. Let `l` be the number of **other** keepers with the same stored coordinates and `i` the global keeper index 0–4 (including invisible/absent earlier keepers). If l>0, the ellipse expands by `(l-i)*2` and the icon shifts by `-(l-i)*2` on both axes. This counter is not an occupancy rank or the number of previously painted overlapping cameras; later keeper indices can produce negative expansion. Reference tests must cover all overlap subsets and absent earlier rows, not only two consecutive keepers. Camera projection should refresh on edits/Undo and remain read-only; source culling is based on the centre tile before drawing radius/icon.

## Required evidence before closing this block

Independent source joins/geometry and guarded malformed data, original icon pixels/transparency and proper lifetime, all fifteen flags reaching the actual layers with persisted/migrated settings, source path colour and independent visibility, EN/RU native/project controls and rendered checks, exact byte/history/selection/copy retention, complete regression and unchanged-or-explained corpus failures. Historical GUI/GDI/hardware/DPI/game acceptance remains separate from offscreen WPF tests. This audit implements none of these missing overlays by itself.
