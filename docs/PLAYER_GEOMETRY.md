# Player starting camera and AI build area

The player tab now exposes both coordinate workflows in English/Russian: numeric camera coordinates, a camera point chosen on the canvas, inclusive build-area endpoints, a rectangle chosen on the canvas, existing named map areas, ordinary map selection and separate source-default buttons. Coordinate edits do not apply other typed general/AI drafts, force computer control, move terrain, create areas or touch scenarios.

## Preserved source evidence

- `Properties_Map_Players.c`, controls 110/111 and `Properties_MapPlayerSetCamera`: custom camera is distinct from automatic camera; `Map_ChoosePointEx` uses `MAP_CHOICELMBPOINT` and stores the chosen point.
- `Properties_Map_Computer.c`, page 1: controls 211–214 edit the four build endpoints, restricted to zero through the corresponding map dimension minus one. Area combo 221 copies an existing area's four endpoints. Negative X1 displays the source (2,2)–(width-2,height-2) default. Canvas rectangle picking and using the ordinary selection are convenient additional entry points into these same fields.
- `Properties_Map_Global.c`: source player defaults set all six coordinates to -1.
- `Compiler_Generate.c`: keeper camera X=-1 resolves both coordinates from the heart; NPC cameras compile to (0,0). Build X1<0 resolves all four build coordinates. Native fields are unsigned WORDs, not project LONG sentinels. No guessed 65535 native default is introduced.
- `Compiler_Info.c`, `Compiler_InfoHeartPosition`: scan the first heart room tile in row order, inspect its (+2,+2) centre, and do not skip a malformed first heart in favor of a later complete square. The shared compiler/reset lookup now follows this rule, including room variant 1. Shape diagnostics stay independent; no tiles are normalized by lookup. With no recognized heart the modern editor uses deterministic (0,0), rather than reproducing an uninitialized-coordinate bug.
- `Files_Editor.c` and `Compatibility.h`: V1 lacks active camera fields; V2's stored camera fields are explicitly ignored and reset to -1 on read. Their specialized camera controls require an explicit project upgrade. Verified V1 CPUArea and V2 PlayerCPUArea fields remain editable without inventing ignored camera support.

## Data and interaction rules

`PlayerGeometries()` is a read-only projection of raw and effective coordinates. Imported malformed numeric values are retained and shown, not silently clipped. Current native version-4 player records and all three verified project player layouts use source field names and types. Duplicate/out-of-range native IDs and future versions are not guessed; noncontiguous native players are addressed by actual ID, not physical ordinal.

`SetPlayerCamera` and `SetPlayerBuildArea` preflight coordinates and expected row payloads, then write only the two/four proven fields in one transaction. Build-area endpoints are inclusive. Reversed numeric endpoints are rejected; reversed canvas gestures normalize their two corners. Boundary and one-tile choices are valid. Overflow, missing area/selection and stale row snapshots fail without partial writes or new history. Unchanged repeated settings are no-ops. Other players, padding, resource settings, existing areas, ownership and scripts remain exact.

Explicit source-default reset does not invent clamped defaults for extended tiny maps: (2,2)–(width-2,height-2) can be inverted on a 3×3 map. Those known source values/sentinels are retained; a valid explicit rectangle can repair them. Native encodings that cannot represent a default are rejected before writing. This does not claim tiny maps are playable.

Default camera/build buttons retain project sentinels; native maps store their resolved values. NPC cameras are read-only in this specialized workflow, matching the original disabled controls; projects always compile NPC cameras to (0,0). Generic raw properties remain separate. A native file has no automatic-camera flag: reopening displays the saved coordinate, not an invented persistent checkbox state. The stored summary and typed drafts remain separate; language switching retains drafts, selected player/area and an ongoing canvas choice.

The canvas picker temporarily switches to the map tab and returns to the player tab after a successful/failed completed choice. Escape, the map Cancel button, a changed player/draft/tool, an external document edit or document close invalidates the pending operation. The current row payload is checked again before writing. Selecting coordinates does not replace the ordinary map selection or copy buffer, and does not paint tiles. Choosing a point uses the origin tile, not the end of a drag.

## Verification and remaining acceptance

The independent core regression covers all five keeper cameras, read-only NPC cameras and all seven build-area rows in native/project formats: boundary/interior coordinates, full-map and one-tile rectangles, exact field encoding, nonzero unknown padding, source defaults and heart scan order, invalid/overflowing inputs, stale rows, no-op and atomic notifications, undo/redo, save/reopen, compilation, duplicate/noncontiguous IDs, historical aliases, ignored camera bytes, upgrade and unknown native/project versions. Corpus projections compare geometry after reopening while asserting unchanged source buffers.

Four additional in-process WPF EN/RU × native/project cases exercise actual buttons/text boxes, canvas mouse-release completion, canceled picks, owner/draft invalidation, external edits, existing-area and selection application, reversed rectangles, live translated hints/drafts, defaults, historical control guards and closed-document cleanup. Optional `DK2_PLAYER_UI_ARTIFACTS` writes actual offscreen tab renders. These are not historical-GUI or gameplay acceptance. All custom AI enum/flag/build-order controls, treasury statistics, camera-marker display and full player diagnostics remain separate requirements.
