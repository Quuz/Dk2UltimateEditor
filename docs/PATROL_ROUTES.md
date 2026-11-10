# Patrol route editing

The bilingual **Patrol paths** tab edits the original area's outgoing `nextid`
and `waitdelay` fields (native `bAreaNextID` / `wAreaWaitDelay`). Routes are not
separate serialized objects. Areas, coordinates, flags, colours, names, attached
scenarios and opaque bytes stay intact when route links change.

## Original evidence

`Source/Sources/Things/Things_Paths.c` defines the reference operations:

- `RectEx_PathsCreateNewPath` creates a UI-only draft. The modern Start command
  chooses its first unlinked area and pause; a single-point draft still has no
  serialized route link. Clearing/closing the draft never deletes the area.
- `RectEx_PathsAddArea`: append may choose an already linked area and preserves
  its outgoing link; inserting before a selected occurrence requires an area
  whose outgoing link is zero, and rewires only the preceding edge and the new
  area's edge. The chosen pause is applied to that physical area.
- `RectEx_PathsKill`: removing a point rewires the selected preceding edge and
  clears the removed area's outgoing link only when it is no longer represented
  in the remaining path, or it is now its final point. Whole-route removal clears
  its outgoing edges without deleting the areas or unrelated incoming edges.
- `RectEx_PathsModifyPause` / `RectEx_PathsSelectInit`: the original UI pause
  range is 0–250. Larger imported field values are displayed unchanged and remain
  accessible in raw typed properties; this manager does not silently clamp them.
- `RectEx_DrawPath`: a new quick start must be unlinked; continuation starts
  from an unlinked endpoint but may target an already linked path or itself.
  Matching requires an area containing the entire chosen rectangle, prefers an
  exact match, and otherwise skips a later candidate only if it is larger in
  **both** dimensions. This is not a smallest-area heuristic.
- `Source/Sources/Things/Things_Display.c`, `Things_DrawPaths`: links connect
  area centres; a self-link is an ellipse inside the area. The new abstract
  canvas draws these independently of object/region visibility via Patrol links.

## Graph and editing behavior

`PatrolAreas`/`PatrolRoutes`/`PatrolRouteFrom` are read-only projections. Open
paths, multiple incoming edges/shared tails, cycles, one-area loops, partial
loops and missing targets remain visible. A route's repeated closing target is
a **separate occurrence**: removing it opens the final edge instead of deleting
the loop-entry area's other outgoing edge. Missing-target markers can be removed
to explicitly open a broken final link.

The generic graph API gives each head its complete path view, including shared
tails; otherwise unseen cycles receive a view. The manager now uses the separate
[source file-order grouping](PATROL_SOURCE_GROUPING.md) projection, including
predecessor insertion, partial-loop rotation and branch-occurrence cleanup.
Data operations use the
selected occurrence's physical predecessor, not a guessed global predecessor.
Changes to a shared area's outgoing link or pause naturally affect every path
that uses it. The [manager session](PATROL_MANAGER_SESSION.md) retains local
occurrence lists and indirect-change notices until explicit reload. It never
silently rewires other incoming edges or expands a chosen linked target into
a different logical endpoint for the next append.

Add, insert, remove, clear, pause and quick-link operations are atomic undoable
edits. Source-style pause editing now has undo too. An old route whose identity
or next-link sequence changed is refused until refreshed. Foreign records,
duplicate/zero/out-of-byte-range area identities and invalid pauses fail without
partial buffer changes. Ambiguous imported routes are displayed rather than
automatically renumbered. Existing raw fields remain available for explicit
repair. Ordinary UI drafts are not undo-history objects.

The manager now uses [explicit editor acceptance counters](PATROL_EDITOR_CALLERS.md).
Each accepted command counts once even with identical bytes; generic raw APIs
retain changed-only behavior. Modern pause/no-byte Undo is a documented history
improvement rather than a claim of identical source Undo behavior. Quick Start
only chooses a cursor endpoint and remains a zero-increment navigation action.

The manager provides named area choices, ID/next-link/pause/coordinates/offset
labels, property navigation, map highlighting, confirmed link removal and
rectangle-picking quick start/continuation. Quick map picking uses the shared
geometry release operation, never terrain painting. Escape, real tool changes,
document changes and close cancel pending picking. Switching EN/RU retains the
pending choice and uncommitted pause text. Copy selection/buffer bytes are not
changed by path editing; Show bounds is an explicit selection navigation action.

## Verification and remaining acceptance

The 40th core regression group independently encodes expected byte changes for
native/project append, insertion (including first/closing occurrences), node and
whole-path removal, end removal, self/partial/full loops, joins, shared-tail
effects, pauses and exact transaction undo/redo. It verifies immutable buffers,
source-order containing-area matching, stale/foreign/linked-endpoint rejection,
no-op/failure redo retention, missing-target handling and imported duplicate-ID
rejection. Save/reopen and project compilation retain links and pauses.

An exhaustive 552-case removal sweep additionally covers every occurrence in
routes of lengths 1–8, with open endings and every possible cycle entry, for
both formats. Expected edge changes are independently encoded into complete
buffer snapshots; no-op and exact undo/redo behavior are checked too.

Four additional WPF cases (EN/RU × native/project) exercise actual controls,
draft creation, append/insert/pause, confirmations, closing occurrence removal,
property/show navigation, quick map picking and joining, invalid input, inline
failure details, live translation, link visibility, actual offscreen canvas
rendering, clear/undo/redo and closing. Areas/scenarios and retained copy-buffer
bytes are checked. Offscreen rendering is not an assertion of pixel-identical
GDI rendering or OS pointer behavior.

The new source-grouping model gate covers all 15,404 graphs/file-order
permutations for 1–4 areas in both formats, nine hand cases, 255 identities and
independently parsed installed Level10. It verifies exact grouping and unchanged
bytes/counters/history. The preserved C routine is independently simulated;
the original GUI has not been executed for this comparison.

The manager's local-list command model is now implemented and checked in both
formats, including linked-target append, indirect warnings and explicit reload.
Real historical project/path-topology fixtures, original-GUI list/selection/
warning comparison and in-game behavior remain separate acceptance work.
Hero-gate generation has its own implemented private workflow
and verification documents; physical generation/footprint acceptance remains
open. This path manager does not complete the overall parity goal.

## Current numeric pause interaction

Modify pause uses an owned [OK/Cancel number request](PATROL_PAUSE_REQUEST.md),
with a private literal draft, unchanged signed initial value, readonly bounds,
range-error retry and one accepted manager command. Its Cancel leaves the map
and private route lists unchanged. Start and the area selector retain their
separate controls. Earlier WPF case counts describe older dated interactions;
CheckPatrolRoutes is now retargeted and compiled, but its new expectations
remain unexecuted; see [harness scope](PATROL_UI_HARNESS_SCOPE.md).
No physical execution of the new modal requests has been performed.
