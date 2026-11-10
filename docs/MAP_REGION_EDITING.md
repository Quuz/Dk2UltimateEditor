# Area, gate and effect geometry

The map tab now has a bilingual region manager. It lists areas (including patrol
nodes), gate points and effect-generator rectangles, with category, ID, project
name where present, inclusive bounds and record offset. The offset remains
important: native `bGateID` is not a unique record identity.

Select a kind to add a region from the ordinary selection or directly on the
map. Select an existing region to replace its bounds from the selection or pick
new bounds on the map. Areas/effects accept a dragged rectangle in either
direction; gates accept one point. Show bounds changes only the ordinary
selection; Properties opens the actual typed record. Point selection cycles
overlapping regions, without editing them. Escape/Cancel releases a pending
geometry choice without discarding the copy selection.

## Source evidence and representation

- `Source/Sources/Things/Things_RectEx.c`: `RectEx_AddRect`,
  `RectEx_ModifyRect`, `RectEx_Modify` and `RectEx_GetFreeID` establish normalized
  rectangles, point gates, retained properties and separate area/gate/effect ID
  lists. IDs 1–255 are allocated independently, including during duplication.
- `Source/Sources/Files/Files_Game.c` and
  `Source/Sources/Compiler/Compiler_Generate.c`: native area endpoints are
  inclusive; native effect width/height are endpoint **deltas**, not tile counts.
  A one-tile effect encodes zero width/height. Project rectangles retain both
  inclusive endpoints; point gates write both endpoints identically.
- `Source/Sources/Compiler/Compiler_Info.c`: explicit project hero-gate records
  compile with ID 1, owner Heroes, health 100 and terrain-derived type/orientation.
  Horizontal/vertical gate-37 variants inspect the next row/right neighbour;
  other terrain falls back to type 33, orientation 0. Native creation uses the
  same source-derived helper. The native fourth tile byte is not the original
  editor's in-memory catalogue discriminator.
- `Source/Sources/Things/Things_Functions.c`: `Things_IsSolid` includes rock,
  earth, gold, gems, wall and both boundary IDs 30/254. Shared orientation logic
  now includes the indestructible boundary too.

`MapRegions`/`GetMapRegion` are read-only. Reversed imported endpoints are
normalized for display only; malformed or overflowing geometry is never repaired
automatically. Explicit `SetMapRegionBounds` on a verified current record can
repair coordinates; unknown rectangle kinds are refused. This setter changes
only coordinate fields, preserving IDs, patrol links, waits, flags, colours,
effects, frequencies, owner/scenario links and opaque bytes. Existing native gate
type/orientation/health remain editable properties and are not overwritten by a
coordinate-only edit. Project compilation derives gate type/orientation as above.

Adding or moving a region is one undo transaction. Gates are points, all bounds
must lie inside the map, and rectangles may include the outer tile row: terrain
painting's protected-border policy does not forbid region geometry there.
Map picking does not paint terrain, activate a heart/portal footprint, replace
the copy selection or modify the independent copy buffer. The pending record's
payload is checked before applying picked bounds; document changes, actual tool
changes, another region selection and closing cancel the pending operation.
Live language switching preserves it. Failures have a localized summary and a
technical reason in the status tooltip.

## Verification

Core fixtures cover native and project formats, all three categories, exact
coordinate-only byte changes, one-step undo/redo, independent ID allocation and
duplication, copied area scenario ownership, exhausted area IDs not blocking
gates, zero-delta effects, border/full-map rectangles, reversed endpoints,
explicit repair with exact undo, unknown kinds, stale payloads, foreign records,
invalid/overflowing bounds and no-op/failure redo retention. Save/reopen compares
all authoritative buffers; project re-export compares complete serialized bytes.
Compilation checks area endpoints, effect deltas, gate points, retained patrol
links/scenarios and every solid-neighbour gate orientation including ID 254 and
unknown-variant fallback. Geometry operations leave terrain byte-identical.

Four in-process WPF cases (EN/RU × native/project) exercise the actual manager
buttons, kind/record controls, shared mouse-release picking operation, reversed
rectangle/point choices while Fill/Heart is selected, highlighting, property
navigation, overlap cycling, no-hit selection, edge/single-tile bounds,
language refresh, undo/redo, cancellation, real tool/record changes, document
close and unchanged copy-buffer bytes. These are not OS pointer automation or
historical-GUI/game acceptance.

## Still required for full parity

The geometry manager itself does not implement gate-region generation or
terrain/gate-footprint interactions. Specialized patrol operations are now in
the separate [patrol manager](PATROL_ROUTES.md); route `nextid`/wait/flags and
generator properties also remain editable in typed records, with existing
reference guards. More semantic controls, effect naming,
historical binary project corpus, rendered desktop QA and acceptance in the
original editor/game remain open in `FEATURE_PARITY.md`.
