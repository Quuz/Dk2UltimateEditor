# Current patrol UI harness — prepared, not executed

12 October 2026. `tests/DK2MapEditor.WindowsSmoke/PatrolOwnedRequestsSmoke.cs`
replaces the old CheckPatrolRoutes body. Both the full WindowsSmoke entry point
and the focused format/language entry point use the new body. All41 projects
build with zero warnings/errors. No WPF harness, application, game, HWND, render
or sound test was run for this change. Compilation does not prove these test
expectations or physical behavior.

## What was obsolete

The previous body cast a route to raw PatrolRoute, read its EndID, edited Pause/
Append/Insert through the shared inline picker, and looked for the removed
PatrolConfirmed checkbox. It also expected empty/nondecimal/overflow pause text
to fail inline, conflicting with the original signed numeric request's failed
conversion to0. New owned manager routes instead contain local occurrences,
including separate closing points and empty roots; candidate selection, pause,
warning and removal each own a different window and lifetime.

The old body is retained as evidence in
`artifacts/Evidence/PatrolHarness-20261012/previous-patrol-harness.txt`. It is not
compiled, required for building, or a product restoration source.

## Prepared interaction coverage

The new harness clicks real workspace buttons and processes actual ShowDialog
windows through the WPF dispatcher. It does not install alternate product
presenters or bypass commands with direct Core edits. A typed ordered callback
queue handles child range warnings while the parent OK callback is suspended;
a15-second dispatcher timeout closes newly opened children before parents.
If executed, these paths open real windows and may invoke the system warning
sound. The runner requires explicit UI authorization; none was used here.

Prepared cases run EN/RU × native/project and include:

- Empty-root creation, root/point availability and no-choice selector Cancel.
- Physical-order Append candidates, unlinked-only Insert, candidate pause loading,
  text/slider synchronization, live language draft retention and one acceptance.
- Numeric initial value, readonly0/250 bounds, Cancel, equal/changed OK,
  below/above-range child warning followed by returning to the unchanged parent,
  decimal prefix and failed/overflow conversion to0. Assigning TextBox.Text in
  this harness does not simulate actual clipboard paste or keyboard filtering.
- Separate closing occurrence removal, shared local lists, linked Append without
  expanding the target chain and source final-edge effects.
- Actual Yes/No removal, checked-No disabling, close without remembering,
  Yes with remember, auto-Yes and independent reset.
- Actual indirect-change warning on selection and after shared removal, close
  without remembering, remembered OK and independence from removal preference.
- Byte/history/activity/counter/notification snapshots for cancellation and
  exactly one accepted command, existing Undo/Redo, record/property navigation,
  quick map links, display toggle, existing canvas render and buffer retention.
- Owner document close during selector/numeric/removal requests, then refused
  stale acceptance and empty workspace state.

The fixture uses four independently placed areas and retains an area's scenario.
Expected occurrence sequences and current source grouping replace the former
raw-head route-count expectations. No expectation is labeled passed merely
because it compiles.

## Evidence and execution boundary

`artifacts/Evidence/PatrolHarness-20261012` contains both terminal build logs,
exit0 records, static control-name/type integration audit and SHA256 proofs.
All1306 current product/Source/Binaries input files are unchanged; Source/Binaries
are1006 of those. Installed4453 map files are unchanged. Published App and Core
match the build; Validation and nine model-check Core DLLs retain the prior hash.

The last executed full Core/corpus run remains PatrolPause-20261012:1809 passes,
63 literal ordered existing failures, terminal exit1, complete original94
comparison. It was not repeated for this test-only change. These results do not
prove execution of the newly prepared WindowsSmoke body.

Only after future explicit authorization, a developer may run:

```powershell
dotnet tests/DK2MapEditor.WindowsSmoke/bin/Release/net10.0-windows/DK2MapEditor.WindowsSmoke.dll --patrol-owned-requests-only
```

Even a future passing automated UI run cannot establish original GUI comparison,
real clipboard/keyboard/focus, visual QA, hardware/DPI, actual sound or gameplay.
Current user-run steps are in [manual acceptance](MANUAL_ACCEPTANCE_RU.md).
The full [FEATURE_PARITY](FEATURE_PARITY.md) goal remains active.
