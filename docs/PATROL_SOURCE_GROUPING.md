# Source patrol manager grouping / Группировка патрулей

`NativeMapDocument.EditorPatrolRoutes()` implements the read-only initial tree
assembly of `RectEx_PathsSetGroups` in `Source/Sources/Things/Things_Paths.c`,
lines 219–339. `List_AddEntry` appends in file order. A seed is an area with an
outgoing link that has not already appeared in an earlier group. The algorithm
traces its links, inserts missing incoming predecessors with a file-order scan
that restarts after each insertion, rotates partial loops, and removes inserted
occurrences that do not follow a real outgoing edge. Shared areas can occur in
several groups. The repeated loop endpoint is a separate closing occurrence.

This changes tree grouping and root order, never serialized links. For example,
file order `2→3, 3→2, 1→3` produces two source groups: `2→3→2` and `1→3→2→3`.
A generic graph traversal from incoming-free heads produces only the second.
The existing `PatrolRoutes()` and `PatrolRouteFrom()` graph APIs retain their
behavior; the manager uses the source projection. Every projected group must
also match `PatrolRouteFrom()` from its first physical area, so existing guarded
append/insert/remove/clear operations remain valid.

The source uses zero-terminated 514-entry arrays. For unique area identities
1–255, a traced path plus inserted predecessors fits that capacity. The managed
implementation uses bounded identity counts and lists. Zero, duplicate or
out-of-range IDs and out-of-range targets retain the existing diagnostic graph
projection: source grouping has no defined identity semantics for these imports.
No import is renumbered or repaired. Missing targets within 1–255 remain visible.

The manager now keeps [owned local occurrence lists](PATROL_MANAGER_SESSION.md),
including empty roots and explicitly started one-point drafts. Commands update
the chosen list; shared lists remain visible with indirect-change notices.
Explicit reload reconstructs current groups. Generic graph views remain separate.
Document replacement/workspace close dispose the manager. No private route state
is serialized. Physical historical dialog and warning/selection comparison remain
user-run acceptance gates.

## Independent verification

`tests/Fixtures/PatrolGrouping/source-groups.tsv` contains 15,404 cases: every
directed one-successor graph over 1–4 identities (including zero endpoints) and
every physical file-order permutation. Expected lists come from a separate
literal fixed-array simulation of the preserved C routine, not from product
code. Nine manually specified fixtures also cover nonconsecutive IDs, the
historical `29→30→4` / `47→4` shared branch, missing targets, self/full/partial
loops and isolated areas. `level10.json` records independently parsed packed
GAMEAREA fields (bytes 30/31) and actual installed file SHA256.

The installed Level10 has 56 areas and 12 patrol groups. Its links for areas
29/30/47 differ from the historical comment's example. The comment fixture and
the installed-file fixture are separate evidence; the installed map is not
modified to match the comment.

Run the current model gate from the repository root:

```powershell
dotnet tests/DK2MapEditor.Validation/bin/Release/net10.0/DK2MapEditor.Validation.dll --patrol-grouping-only
```

The gate checks native and DKM projections, exact group/occurrence order, raw
route compatibility, all file bytes, counters, notifications, history/redo,
the 255-identity boundary, malformed imports and actual installed Level10.
The reference simulator is evidence, not an executed original GUI or compiled
original C binary. Physical EN/RU manager selection, draft handling and original
dialog comparison remain user-run checks in [manual acceptance](MANUAL_ACCEPTANCE_RU.md).
