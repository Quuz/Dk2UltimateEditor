# Terrain cursor footprints and drawing gestures

25 октября: [I07 закрыт по коду](I07_CODE_CLOSURE.md); [компоненты заливки](EDITOR_FLOOD_COMPONENTS.md) теперь сравниваются по source identity. Датированные остатки ниже уточнены: кодовое размещение закрыто; transform/copy/cut/paste scope и ручная приёмка остаются в управляющем [списке](OPEN_FUNCTIONS_RU.md).
## Source evidence

`Source/Sources/Datas/Datas_Map.c` stores four size limits on every ELEMENT. `Map_Mouse.c`, `Mouse_UpdateCursor`, uses those limits to size the minimum cursor, shifting its origin left/up when it would extend past the right/bottom edge. A zero maximum means unbounded, not fixed to the minimum.

| Terrain / variant | Minimum | Maximum |
| --- | --- | --- |
| Dungeon heart (14 / 1) | 5 × 5 | 5 × 5 |
| Portal (12 / 1), mercenary portal (40 / 1) | 3 × 3 | 5 × 5 |
| Hero gate (28 / 1) | 3 × 1 | 3 × unlimited |
| Hero gate (33 / 1) | 2 × 2 | 2 × 2 |
| Hero gate (34 / 1) | 3 × 2 | 3 × unlimited |
| Oriented gate (37 / 1) | 3 × 1 | 3 × 1 |
| Oriented gate (37 / 2) | 1 × 3 | 1 × 3 |

The other source terrain entries have 1 × 1 minimum and unbounded dimensions. In particular gate terrain 29 remains an unconstrained painting element. Owners/variants follow the separate source-catalogue policy in [EDITOR_TERRAIN_RULES.md](EDITOR_TERRAIN_RULES.md).

`Mouse_LeftButtonDown` stamps fixed footprints immediately. Brush and flood also stamp the minimum rectangle when the minimum is larger than 1 × 1. Other drawing tools define a constrained rectangle; `Mouse_LeftButtonUp` forces it filled for these multi-tile terrain entries, irrespective of line/ellipse/outline mode. `Mouse_UpdateRect` expands an undersized axis in the positive direction, including a reversed drag; it retains the drag direction when reducing an oversized axis. A reversed two-cell axis already satisfies minimum two and must not be expanded again.

`Map_Areas.c`, `Map_CanPaint`, checks the initial minimum cursor for locked tiles on bounded terrain. Expanded drags do not repeat that whole-footprint check on release: `Map_PaintElement` skips individual locked tile writes instead. The modern layer preserves that ordering, including variable-height gates drawn toward an edge.

Painting terrain is distinct from gate-region records. `Compiler_GenerateGates` iterates the separate `MapGates` list; `Compiler_InfoGateType` reads terrain/variant and neighboring solid cells to assign orientation. Painting does not invent a region identity or edit the gate list. Hearts, in contrast, are found from tiles during player/gate generation. Duplicate/irregular hearts and connected portal/gate sizes belong to the original compiler's validation stage, not a prohibition in painting.

## Modern implementation

`LegacyCompilerRules.Element` now retains all four original numeric limits. `EditorTerrainFootprint` calculates minimum cursors and constrained gesture rectangles. `PaintEditorGesture` applies the appropriate filled rectangle or ordinary shape as one undoable edit, normalizes source-defined owners and respects positional edge protection. Unknown terrain receives no guessed size constraint. Invalid origins/shapes fail without edits; oversized end coordinates are clamped to the map. Raw `Paint` / per-tile `PaintEditor` remain distinct and permit exact data editing/conversion without cursor-size coercion.

The canvas uses this gesture layer for mouse drawing and its `DrawGesture` method; `FillAt` stamps a minimum footprint for constrained terrain but retains normal flood-fill behavior otherwise. Minimum/final rectangles are previewed on the canvas. EN/RU status explains dimensions, a map too small for the minimum, a minimum touching the protected edge, or selection clipping. A successful command clears the warning. Language changes retain/retranslate the last result; document close clears previews/warnings without losing the independent copy buffer. Escape/capture loss cancels an unfinished gesture; changing map/tool/paint settings cancels its pending state instead of applying old points to new settings.

The modern selection-clipping rule rejects a partial constrained footprint instead of silently drawing a broken heart/portal/gate. This is an explicit extension of modern selection-limited painting; it is not claimed as an original dialog rule. Enlarging/clearing selection restores the original unrestricted workflow. Existing imported irregular footprints are never normalized or repaired on load. Copy/cut/paste/transform keep their separate original semantics and may still create shapes requiring validation.

## Verification and remaining acceptance

Core native/project fixtures independently check all eight constrained terrain/variant entries across all six shape modes, minimum/fixed/unbounded limits, positive/reversed drags, minimum shifting, border press/release ordering, selection rejection, normal selection-clipped shapes, no-op history and redo, failed-stroke rollback, exact unrelated object/scenario/padding bytes, duplicate-heart editability, save/reopen and compilation. Gate tests confirm that terrain alone creates no gate record, then verify generated types and both oriented variants after explicit gate records are added. Input projects are unchanged.

Four WPF EN/RU × native/project cases exercise actual palette/tool controls and canvas gesture/fill methods: all heart tools, portals, mercenary portals, oriented/variable gates, dimension/preview state, border/small-map/selection errors, live translation, undo/redo, unchanged physical owners/scripts/copy buffers, ordinary selected flood and closed-document cleanup. They also cover terrain sampling into another palette state: picker callbacks previously overwrote the sampled owner/variant between updates; the immutable sampled value now initializes all controls and the next footprint. These are in-process tests, not OS mouse automation, rendered visual acceptance or gameplay acceptance.

Read-only original compiler terrain diagnostics are implemented separately; see [shape/heart checks](TERRAIN_LAYOUT_DIAGNOSTICS.md). Source-default new-map settings and an optional heart starter are implemented; see [new-map scope](NEW_MAP_CREATION.md). Still required: new-map game acceptance; gate-region map interactions; original GUI/in-game comparisons; randomized painting and original GDI shape equivalence. Cursor-size implementation alone does not prove those requirements complete.
