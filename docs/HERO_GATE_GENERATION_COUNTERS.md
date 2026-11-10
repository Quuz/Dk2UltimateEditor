# Hero-gate generator editor counters

The manager calls `ApplyEditorHeroGateGeneration`. It commits all additions in
one byte transaction and one Undo entry, while `EditorChangeCount` counts the
individual allocations in the preserved source's `RectEx_Generate`:

| Allocation | Increment |
| --- | --- |
| New gate | 1 |
| New area | 1 |
| New logical area scenario binding | 1 |
| New claim-area condition | 1 per keeper |
| New destroy-gate action | 1 per keeper |

For example, four new gates, four new areas, one new binding and three
condition/action pairs increment the editor counter by **15**, with one Undo.
Modern committed-operation count remains one. No-work acceptance creates no
Undo, counter increment, revision or change notification. Undo/Redo restore
both the buffers and the respective counter values. Failed generation restores
the original buffers and publishes no history, counter or revision change.

## Logical binding versus physical root

Project DKM stores `TRIGGERLINK` records. An existing empty link is a binding and
must not be counted as newly created. Native GAMEAREA always stores a root word,
including zero. `Triggers_CreateActionPtFromArea` in
`Source/Sources/Triggers/Triggers_Functions.c` returns immediately for zero, so
the source game importer would create no logical link for that native area.
Generating its first events therefore counts a new logical binding even though
the native root field already exists. A nonzero existing native root counts no
new binding. This does not insert an extra file or metadata record into the
seven-file game bundle.

Evidence sources: `Source/Sources/Things/Things_RectEx.c` lines 1507–1737;
`Source/Sources/Files/Files_Game.c` area import at line 694;
`Source/Sources/Triggers/Triggers_Functions.c` lines 186–200.

The generic `ApplyHeroGateGeneration` API retains its existing one-increment
atomic-operation semantics. The explicit editor caller supplies the audited
source allocation count; generic `Edit` and other callers are unchanged.

## Verification and remaining work

`HeroGateGenerationChecks` checks independently derived counter totals for
40 valid option combinations × five keeper counts × native/project formats,
plus absent, empty and nonempty bindings, no-work reruns, Undo/Redo and malformed
scenario rollback. Focus command:

```powershell
dotnet tests/DK2MapEditor.Validation/bin/Release/net10.0/DK2MapEditor.Validation.dll --gate-generation-only
```

Executed results are recorded in `CURRENT_STATUS.md` and
`artifacts/Evidence/GateGenerationCounter-20261012`. The subsequent
[private nested generator](REGION_GATE_GENERATION.md) replays approved children
and commits their allocation counts plus one outer property acceptance in one
Undo. Its current evidence is in `artifacts/Evidence/RegionGeneration-20261012`.
Current physical WPF, official-editor and game acceptance have not been run by
the agent. No installed maps are modified.
