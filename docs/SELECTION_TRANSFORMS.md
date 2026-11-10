# Original selection transforms and coordinate provenance

25 октября: [I09 закрыт по коду](I09_CODE_CLOSURE.md). Terrain variant переносится буквально; отдельный gate-region anchor неподвижен. При DKM compile ориентация следует текущим south/east neighbours; native direct bytes и source-retention имеют отдельную явную политику. Физическая приёмка остаётся в [списке](OPEN_FUNCTIONS_RU.md).
25 октября: [I08 закрыт по коду](I08_CODE_CLOSURE.md): band selection и специальные copy/cut/paste rules сопоставлены. Global bands/members не являются spatial things; paste сохраняет буквальный terrain/object payload. I09 gate transforms и ручная приёмка остаются в [управляющем списке](OPEN_FUNCTIONS_RU.md).
Actual in-place editor commands now retain source accepted1+N physical removals/one parent Undo even
when the result is equal. Generic transform remains changed-only/no-op.
See [editor acceptance](EDITOR_TRANSFORM_CALLERS.md). Physical acceptance
of these updated routes remains pending; dated UI evidence below is historical.

## Source-derived requirements

`Source/Sources/Map/Map_Select.c`, `Select_Rotate(BOOL Clockwise)`, rejects non-square selections before modifying tiles or objects. It implements both clockwise and counterclockwise directions. For a square of side N and local point `(x,y)`, the mappings are `(N-1-y,x)` and `(y,N-1-x)`. `Select_Symmetry` maps `(W-1-x,y)` or `(x,H-1-y)` and requires at least two tiles along the reflected axis. Both routines move the original five physical thing lists: creatures, doors, traps, objects and magical objects. They do not move areas, bands, effects or gates as separate rectangle records, and they copy other object fields unchanged. All selected physical things are moved before tile-mask checks, even to/from locked cells. Tile writes preserve locked destinations and substitute neutral solid terrain for masked sources.

Earlier modern UI behavior incorrectly treated a rejected rectangular in-place rotation as a command to replace the copy buffer. It now rejects the shape without changing the map, history or buffer, with an inline translated explanation. Both square directions are accessible in Edit. An explicitly named extra command copies a clockwise-rotated selection to the buffer; this is an enhancement, not the original in-place operation. Pending property edits are committed before successful transforms, as for other edit commands.

The in-place implementation preserves existing object identities, script IDs and exact owner links rather than deleting and reallocating them. Local scripted coordinates follow the same tile transform; external coordinates remain unchanged. Native reserved action child bytes are untouched. Each operation is one reversible transaction.

## Why source bounds were not sufficient

A selection `(2,2,4,3)` excludes an external script target `(4,5)`. After a 90° turn its bounds become `(2,2,3,4)`, which numerically contain that target. Inferring locality again from the new rectangle incorrectly moves it on paste or a subsequent inverse turn. Payloads now retain their original local/external classification across rotations and reflections. Crop may turn a local target outside the smaller slice external, but never promotes an external target.

DKCP version 2 retains the v1 record/header layout and appends one byte for each thing then script: 0 = no coordinate fields, 1 = external point, 2 = local point. Local points must fit source bounds, and physical thing points must be local. Unknown flags, missing/truncated vectors, incompatible point flags, out-of-bounds local points and future versions fail. Exact native/project record data remain unchanged. Original KMAP/KTGS precede the extension and remain readable independently.

DKCP version 3 retains the v2 prefix/provenance vector, appends one mask byte per tile (0 = editable, 1 = protected), then appends the original packed four bytes of each protected tile in row-major order. Protected historical KMAP cells must equal canonical `(30,2,1)`; raw bytes are restored only from the extension. Invalid/truncated flags, inconsistent projection and unknown tails fail. The mask is transformed/cropped in the same geometry as tiles, without being inferred from native terrain/padding.

The current writer uses DKCP5, retaining that provenance/mask prefix and appending
all raw cells for exact retention beside source-compatible KMAP normalization.
It includes DKCP4 source-correct historical KTGS1–5 projection in compatible KTGS6;
DKCP1/2/3/4 remain readable with their previous projection rules. See
[historical selection projection](HISTORICAL_SELECTION_PROJECTION.md).

Version 1 and 2 extensions remain readable and resave as v5. V1 source bounds supply the initial coordinate classification. DKCP1/2 and original DKZ have no mask and are not assigned one speculatively. A historical v1 file that already lost locality after a nonsquare transform cannot be repaired unambiguously from numeric bounds alone; no guessed repair is claimed. See [raw terrain retention](SELECTION_TERRAIN_PROJECTION.md).

## Regression evidence

Native/project fixtures calculate tile permutations and expected coordinate bytes independently for all four transforms, compare every unrelated byte, verify seven native physical categories, project physical things, stationary area/band/gate records, scope/backlink/identity preservation and reserved action bytes. They cover exact one-step undo/redo, opposite rotations, four counterclockwise turns, square/axis-size/out-of-map rejection, unchanged captured buffers, DKZ round trips, transformed/cropped external targets, save/reopen and project compilation. File comparisons account for the legacy container/KMAP being regenerated from the editable tile buffer. Original project files remain unchanged.

Four in-process WPF EN/RU × native/project cases call actual workspace commands, verify direction and tile/object alignment, warning translation, unchanged state on rejection, explicitly rotated buffer generation and closed-document cleanup. No visible windows, OS clipboard or saved user settings are involved. These checks do not establish rendered GUI appearance, original executable acceptance or gameplay behavior. Current suite totals and all remaining requirements are in [the parity ledger](FEATURE_PARITY.md).

Additional protection fixtures independently calculate complete native/project bytes for all four in-place transforms: tile locks do not block physical things from moving to/from the edge, IDs/scopes and unrelated records remain exact. Nonsquare buffered turns, crops and DKZ round trips preserve masks and coordinate provenance. Four additional WPF cases exercise actual masked transform commands and protected DKZ paste. Placement/footprint rules and historical/game acceptance remain pending.
