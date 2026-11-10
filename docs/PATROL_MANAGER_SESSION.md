# Patrol manager local lists / Локальные списки патрулей

Current implementation: `EditorPatrolManagerSession` in the canonical Core;
the EN/RU patrol tab uses its owned, immutable occurrence snapshots. Initial
groups come from [source grouping](PATROL_SOURCE_GROUPING.md). These groups
are UI state, not serialized route objects or a private copy of the map.

## Why local occurrences matter

Given links `1→2`, `3→4→5`, appending area3 to route `1,2` changes only
`2.nextid=3` and the chosen pause. The source local list becomes **1,2,3**;
area3 retains its outgoing link to4. A subsequent append of area6 changes
**3.nextid=6**, not5.nextid. The other local list **3,4,5** remains visible and
is now indirectly changed. Rebuilding both groups immediately would change
which edge the next command edits. Lists now persist until explicit reload,
document replacement or workspace close.

`RectEx_PathsAddArea` in `Things_Paths.c:701` appends only the chosen occurrence
and preserves its existing outgoing link. Insert rewires its physical predecessor
and the inserted area. `PathsKill:553` updates only the selected list and checks
all remaining lists. Whole-list deletion clears occurrences **before the final
occurrence**; a linked final area keeps its outgoing chain. Deleting the last
point keeps an empty root; deleting that root removes the UI group.

Create empty route is source navigation: no field, counter or Undo change.
Start with selected area retains the modern combined Start/AddArea command:
unlinked area plus pause, one acceptance. Quick-map cursor drafts still count
zero. Quick drawing reloads manager groups when returning from map drawing.
Map Undo does not restore local lists or drafts.

## Source warning predicate

`RectEx_PathsCheck:355` compares each displayed occurrence's outgoing link with
the next displayed ID and skips missing areas. At the final occurrence, an
outgoing target already present anywhere in that same list does not count as
an indirect change. A mismatch to an outside target does.

The tab displays EN/RU notices for an indirectly changed selected route or
routes affected by removal. **Reload route list** recreates initial groups from
current links without editing or creating Undo, equivalent to the source
message's close/reopen recommendation. Deliberate editing of the displayed
occurrences remains possible; physical identities and operation scope must
still be valid. Inline notices differ from the original native modal message;
original request suppression and physical modal behavior are not proved.

## Guards, writes and history

Snapshots bind manager ownership, private version, document revision and area
payload fingerprints. Clients cannot supply replacement occurrence lists.
Foreign, stale or closed snapshots are refused before mutation. Area identity,
record order/offset/length or collection changes require explicit reload. Raw
payload changes without a revision still invalidate an earlier snapshot.
Missing imported endpoints remain visible without automatic link/ID repair;
safe missing-marker removal remains available. Source append skips a missing
predecessor while applying the chosen area's pause.

Only actual `nextid` and `waitdelay` fields are written atomically, with one
acceptance count and one modern Undo, including equal values. Source pauses
and one-point deletions sometimes lacked Undo; the modern improvement remains
explicit in [caller scope](PATROL_EDITOR_CALLERS.md). Generic graph APIs keep
their strict full-route guards/no-op behavior. Accepted map changes remain after
manager close; late closed snapshots cannot resurrect lists. Document observer
exceptions after commit synchronize accepted local state rather than claiming
that the completed map transaction rolled back.

The source has514 array slots, including its zero terminator. The managed
session allows513 occurrences and refuses another insert/append before writing;
it does not reproduce an original out-of-bounds access.

Save and compilation serialize actual area fields, never private occurrences,
empty roots or warning state. Reopening reconstructs groups from those fields.
Official game export still has seven files.

## Verification and physical acceptance

```powershell
dotnet tests/DK2MapEditor.Validation/bin/Release/net10.0/DK2MapEditor.Validation.dll --patrol-manager-only
```

The model gate covers native/DKM linked append followed by another append,
shared-list insert/remove/clear, retained linked final endpoints, closing loop
occurrences, empty/root/cursor drafts, equal/changed pause, explicit reload,
foreign/stale/raw/structural/nested/closed guards, capacity513, close/observer
failure after commit, disabled history, Save/reopen and game compilation.
Whole-file expected deltas use independent packed offsets: native next31/wait24,
32-bit project next52/wait56. Flags, labels, opaque bytes and activity data are
retained; counters and exact Undo/Redo are checked. Source and UI wiring are
audited separately.

The original C/GUI and physical EN/RU controls have not been executed by this
gate. Selection, warnings, confirmation, reload, translation, map picking,
close/reopen and DPI need user-run [manual acceptance](MANUAL_ACCEPTANCE_RU.md).
This checkpoint does not close all [FEATURE_PARITY](FEATURE_PARITY.md) requirements.

Append/Insert now have an independent [area selector](PATROL_AREA_SELECTION.md)
with source filtering, pause draft and Cancel; the toolbar uses owned root/point
availability. Selected/all warning requests now use an owned EN/RU modal and
runtime suppression policy; see [warning requests](PATROL_WARNING_REQUESTS.md).
Physical modal timing, focus and original implicit tree notifications remain open.

Remove/Clear now use a separate owned [Yes/No removal request](PATROL_REMOVAL_REQUESTS.md)
with independent session suppression, replacing the panel confirmation checkbox.
The existing manager command bodies and source data effects are unchanged.

Modify pause now opens a separate [numeric request](PATROL_PAUSE_REQUEST.md).
The request owns a literal draft and one guarded existing SetPause command.
Its initial value is not clamped; unlike the candidate selector it has readonly
bounds and no slider. The negative project wait-delay read-only getter was fixed;
existing graph mutation methods and manager command bodies are unchanged.
Physical pause focus/range/clipboard/ENRU/DPI/audio acceptance remains pending.

The former raw-route WPF harness is now [retargeted and compiled](PATROL_UI_HARNESS_SCOPE.md)
for current local occurrences and owned requests. No new WPF execution proof
exists; its future assertions must not be counted as passed UI cases.
