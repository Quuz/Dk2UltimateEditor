# Patrol editor command counters

User decision, 2026-10-15: retain current Undo for accepted patrol pauses and
changed scenario parameters. This is an approved product choice, not an
outstanding request to remove Undo. Equal scenario Save keeps its current
counter-only behavior. This approval does not cover every other historical
difference. See [current history](EDIT_HISTORY.md).

The patrol manager and both quick-link routes use explicit editor callers in
`EditorPatrolCallers.cs`. Accepted Start-with-first-area, pause, append, insert,
remove, clear and quick-link commands increment `EditorChangeCount` once and
commit one modern history operation. Equal accepted values still count once.
Rejected values, foreign/ambiguous records, stale link sequences and invalid
endpoints leave bytes, history, counters and revision unchanged.

## Source scope and deliberate history difference

`Source/Sources/Things/Things_Paths.c` provides the branch authority:

| Source operation | Accepted increment | Source history |
| --- | --- | --- |
| CreateNewPath (522) | 0; UI-only empty draft | None |
| PathsAddArea (701), append/insert | 1 (836), including equal chosen pause | Add/insert link Undo |
| PathsKill (553), point/route removal | 1 (686), including one-point draft removal | Conditional link/route Undo |
| PathsModifyPause (845) | 1 (874), including equal pause | No Undo call |
| DrawPath (1117), choosing first/new point | 0; cursor state only | None |
| DrawPath continuation | 1 (1173) | Undo_CreatePathPoint (Map_Undo.c:296) |

The source confirmation/cancel branches return before these increments.
`PathsProc` maps buttons101–105 to create/add/insert/remove/pause; it has no
general region-property button. Its IDOK and IDCANCEL both close the manager
without rolling back earlier live edits. The modern extra property-navigation
button therefore does not establish a missing original callback.

Modern Start chooses an unlinked first area and its pause. This combines the
source empty-draft creation with its first accepted AddArea: one increment and
one Undo. Quick map Start only chooses the next cursor endpoint, changes no
serialized fields and counts zero; continuation changes a link and counts one.

The previously implemented Undo for pause changes is retained as a deliberate
modern improvement. Equal accepted pause and accepted no-byte one-point removal
also receive a modern Undo step. This differs from source operations with no
Undo allocation. Such accepted commands clear modern redo, and Undo/Redo restore
their before/after counter values. This is not proof of identical historical
history behavior. When history depth is zero, accepted commands still count,
but no Undo/Redo entry is stored. Generic raw APIs keep their previous changed-only
behavior: an equal raw pause consumes neither redo nor a counter increment.

All editor callers use the existing transaction core and unchanged graph
operations. A command cannot nest inside another live edit or active pen stroke.
Only the selected `nextid`/`waitdelay` fields change; IDs, names, scenarios,
definition data, coordinates and opaque padding remain intact. Removing a path
does not delete its areas. Existing source graph/loop rules are documented in
[PATROL_ROUTES](PATROL_ROUTES.md).

## Evidence

`EditorPatrolCallerChecks` independently encodes complete expected buffers for
native/project equal/changed pause and first area, quick links/self loops,
append/first insertion, empty-link removal/clear, closing/middle loop removal and
whole loop clear. Each accepted command checks one increment/notification/Undo,
exact Undo/Redo bytes/counters and unchanged unrelated activity. Rejected/stale/
foreign/nested calls preserve history. Tests distinguish generic no-op redo
retention from explicit accepted editor commands and cover history depth zero.
The older patrol group retains its independent 552-case removal sweep.

```powershell
dotnet tests/DK2MapEditor.Validation/bin/Release/net10.0/DK2MapEditor.Validation.dll --editor-patrol-only
```

Current terminal build/model/full-regression evidence is recorded in
`CURRENT_STATUS.md` and `artifacts/Evidence/PatrolCallers-20261012`. Static route
inspection proves which methods the compiled interface calls; it does not prove
physical mouse/keyboard, original tree ordering, original GUI history behavior,
EN/RU display, DPI or gameplay. These remain explicit acceptance requirements.

## Current numeric pause route

The toolbar now opens [an owned numeric request](PATROL_PAUSE_REQUEST.md) before
calling the existing accepted SetPause command. Cancel and range retry produce
no caller increment. Equal OK retains the documented modern Undo improvement;
the source function still has no explicit pause Undo or PathsCheck. Start's
inline pause and the Append/Insert slider are separate controls. Static routing
and literal Core checks do not prove original implicit tree notification timing.
