# Patrol selection and warning interaction — remaining source scope

Read-only comparison, 12 October 2026. The current local-list implementation is
described in [PATROL_MANAGER_SESSION](PATROL_MANAGER_SESSION.md). This audit
compares preserved C source with current code; it does not execute the original
or modern GUI, and it does not claim physical acceptance.

## Candidate selection and pause

`Source/Sources/Things/Things_Paths.c:701–737` sets `allowlinked` to false for
Insert and true for Append before opening resource6711. `PathsSelectInit:1018`
walks `MapAreas` in its stored order, excluding a linked area only when
`allowlinked` is false. Append shows all areas, with distinct linked captions.
There is no per-route exclusion: a candidate already in the chosen local list
may be offered. Do not turn this into a generic acyclic-graph picker.

The selection dialog starts with no accepted candidate, pause text0 and a
0–250 slider with ticks every10. `PathsSelectChanged:1087` disables OK without
a selected area; otherwise it copies the selected area's ID and existing pause
into the context and textbox. Selecting a different area replaces the pause
draft with that area's current value. Typed pause and slider synchronize under
`donotupdate`; text changes clamp to0–250. Cancel returns0 without changing map
fields, counters or manager occurrence lists. OK reads the pause and returns1;
only the caller subsequently modifies fields and adds an Undo/counter.

`Context->useexisting` occurs once, as an assignment at735. It is not read by
the selection dialog and is not evidence of an area-creation workflow. The
source dialog-launch error branch also warns then returns1 for result−1;
that stale-context failure behavior must not grant modern mutation authority.
Failure to open a selector should leave the command unapplied.

The former shared picker offered all areas for Start/Append/Insert and deferred
Insert's filtering to an acceptance refusal. The current implementation now
opens an owned Append/Insert selector with physical candidate order, linked
indication, an independent pause draft/slider and Cancel. Start retains its
extra modern picker. See [current selection](PATROL_AREA_SELECTION.md). Core
checks cover model filtering/lifetime/writes; actual selector GUI remains
unexecuted, so bounds alone still do not prove physical selection behavior.

## Availability and warnings

`PathsIsButtonActivable:457` defines five separate predicates:

| Command | Preserved source condition |
| --- | --- |
| New | At least one area with `nextid=0`. |
| Append | A selected root/point, no final occurrence repeated earlier in the same list, and at least one unlinked area. The chosen candidate itself may be linked. |
| Insert | A selected point resolving to an area, plus at least one unlinked area. Root selection does not enable insertion. |
| Remove | Any tree selection, including an empty root. |
| Modify pause | A selected point resolving to an area. Root selection does not enable pause editing. |

Modern `CanAppend` covers the source linked-ending/unlinked-availability rule
plus explicit identity/capacity guards. The toolbar now calls owned
`GetAvailability` for root/point selection and retains removal confirmation.
Selecting a route clears point selection; it does not infer selection kind
from a numeric native pointer. Safe missing-predecessor/identity checks add
refusals where the original could attempt an invalid operation.
Empty-list source Append scans `areas[-1]`; modern code supports empty roots
without reproducing that invalid array access.

`PathsCheck:355` is called only for tree selection (`All=FALSE`, line95) and
after removal (`All=TRUE`, line694). It resolves a selected child to its root,
or checks all roots until the first changed route. The source message has
different selected/other-route text, OK, `REQUEST_RECTEXMODPATH` suppression,
`MBF_AUTOREPLY`, a question icon and a cursor-following position. Current Core
implements the comparison predicate and owned selected/all requests. App now
shows an owned EN/RU modal with the original question icon and runtime-only
suppression. Close/Escape do not remember the checkbox; OK does. Actual modal
timing/focus and native implicit tree notification count remain acceptance gates.
See [warning requests](PATROL_WARNING_REQUESTS.md).

`Requests.h:17` defines the changed-route bit as0x2000. `Misc_Requests.c:189`
returns defaultgadget1 without opening the dialog when that bit is set.
`Misc_MessageBoxExtReset:847` clears the bit then copies checkbox1200 into it
only on the default reply. Startup calls `Config_SetToDefault`, which zeroes
the entire Config before assigning defaults; RequestsFlags has no nonzero
default override. Separate persistent storage of this flag is not proved by
the registry helpers for recent files/game path. Current modern AppSettings
has an independent copy-buffer replacement preference; do not reuse that
preference or assume source request suppression survives an application restart.

## Next implementation and evidence

The owned draft, EN/RU child selector and root/point availability are now
implemented with native/DKM model checks. Source/RC comparison remains their
independent reference; actual physical EN/RU selector acceptance is pending.
The source request path is now audited and implemented with a session-only
policy, original icon and owned EN/RU warning window. The distinct source
removal-confirmation bit0x1000 now has an owned Yes/No request; checked remember
disables No, No/close return0, Yes/default1 owns one existing removal command.
See [removal requests](PATROL_REMOVAL_REQUESTS.md). Numeric pause at source845
now uses an owned OK/Cancel literal draft, signed initial value, readonly0/250
bounds and nested range retry; see [numeric pause](PATROL_PAUSE_REQUEST.md).
Physical modal acceptance and raw Win32 clipboard-edge conversion remain open.

Model fixtures should independently enumerate linked/unlinked candidates in
physical order, duplicate/missing identities, repeated route occurrences,
selection/no-selection, imported pauses outside bounds, cancellation, source
range endpoints, structural reload and manager close/replacement. Check whole
expected buffers and one manager acceptance only after confirmation. Build and
full ordered regression remain separate gates. Physical EN/RU candidate lists,
slider/keyboard, selection, Cancel, warning suppression/focus and DPI require
user-run acceptance; no automated editor/game launch is authorized.

All other [FEATURE_PARITY](FEATURE_PARITY.md) requirements remain in scope.
