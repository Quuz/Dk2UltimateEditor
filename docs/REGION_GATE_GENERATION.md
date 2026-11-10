# Private region gate generation

Gate properties have a **Generate hero gates** child. It operates on the
independent parent draft. Preview does not change the live map. Confirming
**Keep generation in draft** retains that child's approved additions; cancelling
the child discards only its additions. Outer Cancel discards every accepted child
and every property change. Outer Apply commits everything in one Undo/Redo step.
The independent generator manager remains available.

The child exposes the source gate/area groups, individual generation flags,
destruction-event dependency, keeper count and project name/information/colour
templates. Group switches retain inactive child selections. Input changes clear
the preview and its confirmation. EN/RU resources update labels and preview
descriptions without resetting entered values. Native templates remain disabled
because game records have no editor-label fields. Colour buttons reuse the
existing system colour selector; current physical dialog acceptance is pending.

## Source and storage authority

`RectEx_Proc` button112 selects generation only for RECTEX_GATE.
`RectEx_GenerateGates` creates a fresh child, scans gate sites and calls generation
only after accepted child closure. `RectEx_GenerateInit` uses ALL flags, default
template text, gate colours from the parent context and default area colours.
Modern defaults retain these rules, including keeper count from map properties.
The preserved source generates live; the requested independent modern parent
instead stages these effects until its final Apply.

`RegionGateGenerationSession` carries a parent/version/owner lifetime. Its public
preview contains immutable values, counts and IDs, with no mutable working
document, native records or file buffers. Plans are tied to that child. Only one
generation or geometry child is allowed at a time. Parent preflight/Apply refuses
pending children. Cancel/disposal is idempotent; stale, foreign, closed and late
acceptance is refused. Live Edit followed by Undo remains a stale parent because
the revision changed; direct raw mutation is also detected by the fingerprint.

Approved operations store immutable options and the parent property payload at
acceptance. `ReplayRegionGeneration` clones the current verified baseline with
the existing `ClonePropertiesDocument` helper and replays those commands in
order. It checks file topology and uses the same guarded generator for every
allocation, ID, root and event. The final property payload is applied after
replay. The frozen outer plan contains private copies of the resulting files;
publication replaces their buffers inside one existing transaction. No input
file is written during this process. Seven-file game export is unchanged.

This design preserves earlier accepted children when a later child is cancelled
or fails. A subsequent child sees the generated draft regions/events. Repeated
generation without work adds no allocations. Source counter totals are the sum
of child gate/area/logical-binding/condition/action allocations plus one for
outer accepted properties, including equal acceptance. Modern operation count
and live notification remain one. See [counter rules](HERO_GATE_GENERATION_COUNTERS.md).

Source project destroy-gate actions target a gate ID; compilation resolves its
current point. Native destroy-gate actions use coordinates. Newly staged native
actions targeting the selected physical gate are retained as bindings during
replay and receive that gate's final coordinates if it moves in the parent.
Only those new actions are rebound; imported and unrelated event payloads retain
their original bytes. Existing generic coordinate APIs are unchanged.

## Evidence and remaining acceptance

`RegionGateGenerationChecks` covers the 400 native/project option/keeper cases,
private preview/confirmation, one parent Undo and counter Undo/Redo, whole Cancel,
repeat children, event-only second generation, no-work, inherited templates,
pending-child guards, stale/private/raw/foreign/disposed cases, malformed branch
rollback, final moved-gate bindings and real Save/reopen/project compilation.

```powershell
dotnet tests/DK2MapEditor.Validation/bin/Release/net10.0/DK2MapEditor.Validation.dll --region-generation-only
```

Executed results, including any initial failed harness/build attempts, are kept
in `artifacts/Evidence/RegionGeneration-20261012` and summarised in
`CURRENT_STATUS.md`. Compiled WPF code and model evidence do not establish actual
modal focus, colour-dialog behavior, physical EN/RU layouts, DPI, original GUI
comparison or gameplay. User instructions are in [manual acceptance](MANUAL_ACCEPTANCE_RU.md).
The complete FEATURE_PARITY goal remains open.

## GateGenerationRequests-20261017

Current confirmation no longer uses a checkbox. Workspace Apply and child
Keep show the shared source Yes/No request; capacity failure has a separate
OK notice, three variants and independent remember/reset. See current
[request behavior and evidence](GATE_GENERATION_REQUESTS.md). Earlier WPF
results bind their own dated builds; the current suite was compiled only.
