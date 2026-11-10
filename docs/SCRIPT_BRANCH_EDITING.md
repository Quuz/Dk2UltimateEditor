# Scenario branch movement and deletion

Source of behavior: the preserved original `Source/Sources/Triggers/Triggers_Select.c`, functions `Triggers_SelectMoveUp`, `Triggers_SelectMoveDown`, `Triggers_SelectMoveRight`, `Triggers_SelectMoveLeft`, `Triggers_SelectKillTrigger`, and `Triggers_SelectKillAction`; unlinking and recursive deletion are in `Triggers_Functions.c`.

The branch is the selected event plus its AND children and their complete chains. The selected event's OR/next continuation is not part of that branch. IDs and event parameters are retained during movement. All operations are one undoable transaction in native maps and supported project layouts.

| Operation | Original order retained by the modern implementation |
| --- | --- |
| Up / down | Before previous sibling / after next sibling; carry children |
| Into next condition | Find the next condition among following siblings, prepend to its children |
| Before parent condition | Splice out of the child chain and insert immediately before its parent |
| Move to chosen connection | Splice the old continuation into the source and prepend to the destination chain |
| Delete event and children | Remove the selected event and AND subtree; retain its following OR/next chain |

The modern editor does not reproduce the original move-error path that deletes a failed move. Missing targets, duplicate/zero IDs, cycles, incompatible scopes, descendant destinations and shared deletion descendants instead leave buffers and history unchanged. Multiple incoming connections to the selected event must be explicitly detached before moving or deleting it. Native action `ChildAnd` bytes are reserved payload, not children. Removing an owner binding remains a distinct operation that retains every event.

Core regression coverage (native and project): all four original directions, occupied and empty destinations, moving between owners, original sibling preservation, nested children, condition/action deletion, same-connection no-op, boundary rejection, exact undo/redo, unchanged surviving IDs/parameters/reserved bytes, project backlinks, scope rejection after transient detach, shared roots/children, cycle and dangling rejection, compilation and save/reopen. Four additional EN/RU × native/project WPF cases exercise actual buttons, destination choices, explicit deletion confirmation, undo, live language refresh and document close. See the [parity ledger](FEATURE_PARITY.md) for current suite totals, including later tree-projection and selection-buffer coverage.

Still required for overall parity: historical GUI comparison of resulting scenario files, broader malformed/shared-graph fixtures, expansion-state UX and in-game acceptance. Original sibling-style projection is now implemented and separately tested. These automated tests do not prove full editor parity or gameplay correctness.


## Current ID editing — 15 October 2026

The shared identity remapper now refuses referenced duplicate old IDs and
updates every actual physical edge for unique IDs. Zero repair preserves empty
links; historical category ranges are enforced on changed IDs. See
[SCRIPT_IDENTITY_SAFETY](SCRIPT_IDENTITY_SAFETY.md). No installed map is repaired.
