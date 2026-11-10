# Selection bounds and internal copy buffer

Current Cut/Paste acceptance counters and parent Undo follow source1+target
removals even for equal values; Copy remains read-only. The generic raw methods
retain changed-only/no-op behavior. See [selection callers](EDITOR_SELECTION_CALLERS.md).



## Original source evidence



- `Source/Sources/Map/Map_Select.c`, `Select_Change` / `Select_ChangeProc`: numeric X/Y/width/height, zero-based coordinates, current bounds as defaults or `(1,1,1,1)` when no selection exists, reject a rectangle exceeding the map.

- The same file, `Select_Copy`, `Select_Paste`, `Select_Load`, `Select_Save`: internal `MapCopy` / `MapCopyThings`, paste at the corrected selection origin, clip at the right/bottom edge, separate load/save of the buffer and selected region. `Select_CopyRequest` asks before replacing a nonempty buffer.

- `Source/Sources/Windows/Windows_MainWindowProc.c`: separate menu commands for buffer versus selected-region load/save; the buffer is released on exit, not normal map changes.



The Windows OS clipboard is not an original editing requirement. Older ledger wording claiming otherwise was incorrect. The modern editor does not read or write the OS clipboard for map copy/paste.



## Implemented behavior



The workspace owns a buffer independent of its open document. Copy/cut create a candidate first; a failed cut leaves both map and previous buffer intact. Closing/opening maps does not clear it. Paste uses selection origin, or cursor when no selection exists, and clips at the target's right/bottom edge. Direct DKZ insertion does not replace the retained buffer. Buffer import/export works without an open map. Selection export uses the current selection, not the buffer.



Cropping retains only placed things within the requested local rectangle, matching physical project scope links and reachable scenario nodes. It preserves payload bytes and absolute source coordinates; only insertion performs coordinate/ID remapping. Each coordinate payload also retains local/external provenance through transforms and the DKCP v2/v3 provenance vector. Cropping turns a local script target outside the smaller slice external; an already external target never becomes local just because it numerically enters rotated bounds. Scope ownership is role-specific: unused creature/door/object fields on map/player links cannot claim a selected object. Duplicate matching physical identities are rejected. Project allocation also reserves IDs from existing orphan links so a newly pasted thing cannot acquire another object's scenario. Native action reserved `ChildAnd` bytes are never traversed or remapped as links.



Target project scenario layouts must match the source payload stride. V2/V3 link mismatches and V1 nested-scenario targets are rejected transactionally; no implicit lossy conversion is performed. Scripted native/project cross-format paste remains refused, while script-free selections can convert. DKZ remains bounded to 64 MiB. SaveNew refuses replacement; SaveConfirmed atomically replaces a destination after the Save dialog accepts it, preserving existing data if validation/publication fails.



Numeric bounds are available under Edit → Change selection bounds. The actual selected rectangle initializes all four inputs; without one, the original `(1,1,1,1)` default is used, adjusted to `(0,0,1,1)` on a one-tile map. Invalid/nonintegral/overflowing/out-of-map input shows a localized inline error and disables acceptance. Dimensions must be positive. Full-map selection is permitted: the original dialog's width/height-minus-one cap and zero-size acceptance are not reproduced. Enter accepts; Escape cancels. Labels, errors and title use live EN/RU resources. Selecting/canceling changes no map bytes/history and does not replace the buffer. Resize/undo clears only selections that no longer fit.



## Evidence and remaining work



Core native/project fixtures verify cropped ownership/graphs, offset alignment, immutable source/buffer, exact undo/redo, reserved bytes, orphan-ID reservation, malformed-owner rejection, incompatible historical-layout rollback, save/reopen and compilation. Four native/project × EN/RU WPF cases cover persistent buffers, clipped/direct paste, cut rollback and cross-format refusal/conversion. Four additional cases construct the actual numeric dialog and controls, exercise bounds/error/button states, live localization, cancellation, one-tile defaults and resize undo. They are in-process checks without visible windows or user-settings writes; they do not constitute mouse/keyboard visual QA or historical/game acceptance.



UI copy excludes protected border objects and their graphs and captures a separate protection mask. Crop/transforms and DKCP v3/v4 preserve it without changing native padding. UI paste skips protected destinations entirely; an accepted destination receiving a masked source cell becomes neutral solid and loses its old physical things. Only accepted source things and their owned/reachable graphs are cloned. UI cut leaves border tiles/objects/scopes unchanged and uses the verified map background when heart/portal/gate terrain is selected. Four additional WPF cases verify masked DKZ reload/paste and all-object in-place transform ordering; see [terrain policy](EDITOR_TERRAIN_RULES.md).



Still required: complete special-placement/band workflows, physical acceptance of buffer replacement/recovery, historical direct-export comparison and historical-GUI/in-game acceptance. Replacement and recovery dialogs are implemented; source/CLI checks do not prove their physical use. Original rotation is square-only, not a rectangle resize: both directions and rejection without buffer replacement are implemented. The extra rotated-copy command is explicit; see [transform details](SELECTION_TRANSFORMS.md). Overall parity is not complete; see [the ledger](FEATURE_PARITY.md).

## Current archive routes

ReadForImport validates AREA1 and the first KMAP independently. Complete archives
load normally. Valid first KTGS records survive a damaged later extension as a
HistoricalData candidate; invalid things yield TerrainOnly. Both need explicit
recovery consent before replacing a buffer or inserting into a document. Changed
file/map/selection/owner lifetime invalidates that consent. Invalid primary
terrain never offers recovery. Strict Read/Parse remain strict.

Current Release archive checks passed17 import and14 write cases. They cover
whole-byte/history/statistics retention, fresh/confirmed destinations and
failure cleanup. No windows were launched; earlier EN/RU offscreen fixtures
remain dated evidence. DKCP4 introduced historical KTGS projection while preserving
original records. The current writer uses DKCP5 for source KMAP projection
and all raw terrain cells, still reading DKCP1/2/3/4; see
[the36-fixture scope](HISTORICAL_SELECTION_PROJECTION.md).

Terrain normalization and raw retention are described in
[SELECTION_TERRAIN_PROJECTION](SELECTION_TERRAIN_PROJECTION.md).

AREA source load/save/default boundaries are described in
[AREA_WORKFLOW_SOURCE_SCOPE](AREA_WORKFLOW_SOURCE_SCOPE.md). Object capture order is now source row-major/category; transformed buffers retain
their existing list. See [object order](SELECTION_OBJECT_ORDER.md) for duplicate
direct-export boundaries. Real historical files, native dialogs/hardware/DPI and
official-editor/game acceptance remain open.
