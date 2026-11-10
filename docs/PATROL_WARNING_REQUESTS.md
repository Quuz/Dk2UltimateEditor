# Patrol warning requests / Предупреждения патрулей

The current Core owns `EditorPatrolWarningPolicy` and its disposable requests;
App owns an EN/RU `PatrolWarningWindow`. This completes the implementation of
the selected-route and explicit all-after-removal warning requests. Actual
window interaction remains a user-run acceptance gate.

## When a warning appears

The manager keeps private occurrence lists until Reload or close. Editing a
shared area can make those displayed lists differ from the actual map links.
Selecting a root or one of its points checks that root's list. Accepted Append
and Insert select their resulting point and run the same check. Remove and
Clear check all remaining roots and warn for the first changed list. The
source's message says "another route", but its All predicate does **not**
exclude the selected route. Pause has no additional explicit warning call.

Missing area markers are skipped by the source comparison. A final outgoing
link to an area already inside the same local list is not itself a change;
an earlier edge mismatch still is. These rules use the existing source-derived
manager predicate, rather than rebuilding lists after every edit.

The warning has one OK button, the original question icon, and a **Do not show
this warning again during this editor session** checkbox. It recommends
reloading the lists before further editing. OK only acknowledges the warning;
it does not reload, undo, repair or otherwise change the map.

## Suppression and cancellation

The original request uses `Config.RequestsFlags`, bit `0x2000`, default button
1 and checkbox 1200. Only the default reply remembers the checkbox. Close or
Escape releases the request without remembering its checked state. A suppressed
warning returns without displaying a window.

The modern policy belongs to the main workspace/editor session. Closing a map,
opening another map or reloading patrol lists does not reset suppression.
Restarting the application begins with suppression off. **Show patrol
warnings again** explicitly resets it during the same session. The reset is a modern
convenience; it does not create map history or change counters.

This flag is not written to KWD/KLD/DKM or AppSettings JSON. Source startup
zeroes Config through `Config_SetToDefault`; separate disk persistence of this
request bit has not been established. Do not infer it from the registry helpers
for recent files or the game folder. Patrol removal confirmation now uses a
separate [owned Yes/No request](PATROL_REMOVAL_REQUESTS.md) and source bit `0x1000`.
Resetting its policy does not change warning suppression. Copy-buffer confirmation
is also separate.

## Ownership and positioning

A request is tied to one policy, manager, owned route snapshot, private manager
revision, map revision and area payload. Closing/replacing the owner, changing
selection or changing the map invalidates the old response. Invalid responses
cannot change suppression. An already committed map edit is retained if showing
the warning fails; the status explicitly reports that fact. Binding refreshes
do not open duplicate warnings.

The embedded icon is the unchanged 2238-byte `Question32.ico`, original resource
83. The source `Window_UnderCursor` places the top-left at the mouse and adjusts
against the primary screen/taskbar. The modern window uses device-pixel metrics
and clamps to the nearest monitor's work area, preserving visibility on multiple
monitors. This is a documented improvement, not literal primary-screen geometry.
Actual DPI, keyboard focus, taskbar and monitor transitions are unverified.

## Evidence and remaining acceptance

```powershell
dotnet tests/DK2MapEditor.Validation/bin/Release/net10.0/DK2MapEditor.Validation.dll --patrol-warning-only
```

The Core check covers 80 native/project flag/reply/request-kind cases, shared
routes, selected and all predicates, final loops and missing markers, session
replacement/restart/reset, private/raw/revision/structural/foreign/closed guards,
and six independent cursor/work-area geometry cases. Read-only replies compare
all map bytes, opaque payloads, private lists, history, counters, notifications,
dirty state and activity data. Source constants/calls, original icon bytes,
unchanged manager/selector/raw APIs, EN/RU key parity and App lifecycle wiring
are audited separately in `artifacts/Evidence/PatrolWarning-20261012`.

The first focused run failed because its fixture selected an already linked
cursor area. The fixture now uses an unlinked area and creates its indirect
change explicitly; product validation was retained. Both logs are preserved.

No editor, game, original C or WPF window was executed by this checkpoint.
Implicit native TreeView notifications during deletion can cause additional
selection checks; their exact count/timing is not proved by the explicit caller
and model evidence. Physical original/modern modal timing, EN/RU wrapping,
checkbox/OK/close/Escape, focus, icon rendering, DPI and multiple monitors remain
open in [manual acceptance](MANUAL_ACCEPTANCE_RU.md). Older WPF tests do not
execute this new window. All [FEATURE_PARITY](FEATURE_PARITY.md) requirements and
the whole active goal remain in scope.

The current [UI harness](PATROL_UI_HARNESS_SCOPE.md) now prepares actual owned
warning replies on selection and shared removal, close versus remembered OK,
and independent suppression. It has only build/static evidence, no UI execution.
