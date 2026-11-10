# Patrol area selection / Выбор области патруля

Актуальный частичный живой результат1ноября: [окна EN/RU и связи патрулей](PATROL_LINKS_20261101.md). Он уточняет историческое «не запускалось» ниже только для перечисленных подпунктов; полная ручная группа остаётся открытой.

`EditorPatrolAreaSelectionSession` owns the Append/Insert candidate and pause
draft, bound to the manager's immutable local-route snapshot. The current EN/RU
`PatrolAreaSelectionWindow` calls it; no map field changes before acceptance.
Original source rules and remaining warning scope are in
[PATROL_SELECTOR_SOURCE_SCOPE](PATROL_SELECTOR_SOURCE_SCOPE.md).

## Workflow

Append offers all physical areas, including linked targets and targets already
occurring in the local route. Linked candidates have an EN/RU indication.
Insert offers only areas with no outgoing link. Both preserve physical file
order. Duplicate/zero/out-of-range identities remain visible and unavailable
for acceptance; no imported ID/link is silently repaired.

There is no initially selected candidate, pause starts0 and Apply is disabled
until a valid selection. Selecting an area initializes the private pause from
its stored value, clamped to0–250; selecting another replaces that draft with
the new area's pause. Deselecting disables Apply and retains the pause text.
The slider has a0–250 range, tick interval10 and integer pause updates; textbox
and slider synchronize without recursive updates. Bounded integer text clamps
to the range; invalid/empty integer text converts to0. No clamping writes back
to an imported record until Apply. Language changes rebuild captions while
retaining candidate identity and draft pause.

Cancel, Escape or window close dispose the selector without changing map bytes,
manager local lists, acceptance counter, dirty/history or activity. Apply calls
one existing manager Append/Insert, commits only actual next/pause fields with
one modern Undo and source acceptance count, then closes the selector. Append
still adds only the chosen occurrence and preserves its outgoing chain.

The toolbar now separates route-root and point selection. Selecting a route
clears the selected point; point-only Insert/Pause/Remove are disabled until a
point is chosen. Whole-route Clear remains available for an empty root. Append
is disabled for a closing repeated endpoint or lack of unlinked availability;
Insert requires a valid point, capacity and an unlinked area. Modern identity,
missing-predecessor and513-occurrence capacity guards additionally prevent
unsafe source cases. Remove/Clear still require explicit confirmation.

The existing extra Start affordance retains its selected-area picker and pause;
its label now identifies the area as belonging to a new route. Both quick-map
drawing routes retain their old counters and source point matching. Generic
raw graph/editing APIs and existing manager command bodies remain unchanged.

## Lifetime and verification

The selector accepts only its own immutable candidate objects. Private manager
navigation, a document revision, raw area-byte mutation, area structure changes
or manager closure invalidate it. Owner replacement/workspace close closes the
child; late events cannot apply to the replaced owner. Notifications after
commit may close the manager or throw: accepted fields remain committed and
the selector loses authority, preventing a second acceptance.

```powershell
dotnet tests/DK2MapEditor.Validation/bin/Release/net10.0/DK2MapEditor.Validation.dll --patrol-selection-only
```

The focused Core gate passes native and DKM with16 independently authored
physical-order/filter masks per operation (64 cases across both formats),
source pause initialization/range/deselection, navigation/Cancel/no-choice,
equal-byte first acceptance, linked append followed by another append,
insertion, whole expected buffers and literal packed offsets31/24 and52/56,
one counter/notification/Undo, activity retention, Save/reopen/game compilation,
foreign/stale/raw/structural/nested/closed guards, malformed candidates, root/
point/loop/missing/unlinked/capacity availability and post-commit observer
exception/reentrancy/close. Initial manager grouping and generic raw API gates
remain in the full validator.

Window wiring, EN/RU key coverage and language/close subscriptions have a static
audit and build evidence. The new window has not been executed or rendered by
the agent. Actual lists, slider/typing/keyboard, modal focus, Cancel/Escape,
language changes and DPI require user-run [manual acceptance](MANUAL_ACCEPTANCE_RU.md).
Source warning requests and session suppression now use a separate owned
[EN/RU warning window](PATROL_WARNING_REQUESTS.md). Physical warning/modal timing
remains an independent acceptance requirement. This does not close
all [FEATURE_PARITY](FEATURE_PARITY.md) or game acceptance requirements.
