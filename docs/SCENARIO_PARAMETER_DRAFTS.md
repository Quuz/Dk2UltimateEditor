# Scenario parameter implementation checkpoint

This initial foundation checkpoint is followed by twenty source radio groups,
complete direct-case labels and the first actual source parameter windows.
See [current window scope](SCENARIO_CONTROL_WINDOWS.md) and the latest
CONTINUE_AFTER_CODEX_RESTART.md entry for current acceptance. Evidence below
records the earlier thirty-comparison foundation, not acceptance of later code.

The source inventory in SCENARIO_CONTROL_PAGES_SOURCE_SCOPE.md still defines
the complete remaining control scope. This checkpoint implements the shared
transaction and ordinary comparison selectors; it does not claim all original
parameter windows are finished.

## Immutable parent transaction

`ScenarioParameterDrafts.cs` creates a private immutable snapshot using the
canonical current record, source type/page identity, document revision and
whole-document fingerprint. It supports all 69 original parameter pages and
the 26 additional conditions whose only original page is common repetition.
Those repeat-only conditions expose only the byte at project 55/native 23;
their imported parameter bytes remain untouched. The eight actions with no
original parameter page cannot acquire one through this API.

Project parameter-page snapshots retain all eight LONG operands, allowing a
future page to compose dependent/hidden inputs without resetting inactive data.
Native snapshots use only literal source compiler fields, including the two
DestroyGate coordinate WORDs rather than inventing a native project gate ID.
Graph IDs, links, action repeat/padding and unknown native constants are outside
the transaction. Field offsets, primitive types, boundaries, disjoint ranges
and repeat layout are checked. The original compiler and native reader both
require trigger collection version 4; future native headers are refused.

Sparse updates compose into a new immutable draft. All primitive encodings are
validated before returning it or applying any writes. Apply commits the whole
page with one localized history operation; equivalent decimal/hex spellings
produce no change/history. Foreign/stale/raw mutation/edit-and-Undo/reentrant
drafts are rejected. No alias table is edited by this transaction. The accepted
four alias pages retain their separate composed operand/name draft API.

This is the byte/transaction layer. Its primitive ranges are not a substitute
for source-specific choice, unit, active-state and nested-page validation.
The forthcoming page controllers must impose those source rules and owner/
window/pending-grid lifetime checks before applying this shared transaction.

## Actual ordinary comparison selectors

`ScenarioParameterChoices.cs` supplies six bilingual suggestions for param1
on exactly the thirty independently inventoried original comparison pages.
The values are literal 1..6 from controls 200..205, not display indexes. They
route through ValueCatalogues into the existing actual property selector.
Unknown imported operators keep their current numeric value/custom entry;
browsing/translation does not repair them. Other operands/event types are not
misidentified as comparisons. Future native collection versions receive no
verified comparison suggestions.

The new actual-control test exposed retained items in the hidden named-value
picker after document close. ClearNamedValue now clears its ItemsSource and
caption as well as its field/draft references.

## Independent evidence

`ScenarioParameterPages.json` freezes all 103 original type/page identities and
literal native compiler locations, independently captured from the original
source audit/compiler fixture. `ScenarioComparisonPages.json` freezes all
thirty pages and six radio/control values. Capture scripts under Test refuse
to overwrite their outputs and verify original source hashes.

`ScenarioParameterDraftChecks.cs` covers both formats, all 95 eligible drafts,
all primitive fields/boundaries, opaque/inactive preservation, one Apply,
exact Undo/Redo, numeric no-op, all 360 comparison writes, duplicate/graph/type/
overflow/foreign/raw/edit-and-Undo/reentrant inputs, future native headers and
save/reopen. Final focused Debug Core passes exit 0 in
`Test/editor3-scenario-parameters-core-focused-03.log`.

`ScenarioParameterSmoke.cs` exercises all thirty actual property pickers and
all six Apply/Undo/Redo values per format/language (720 actual UI applications),
unknown operator retention, live translation, unchanged browsing/history/dirty
state, copy-buffer identity and close. It is registered in focused and full
WindowsSmoke suites. The first focused run failed only the close check and is
retained as diagnostic evidence. The corrected focused run is terminal exit 0:
`Test/editor3-scenario-parameters-ui-focused-02.log`, four passing cases.
Release repeats the four cases successfully, terminal exit 0:
`Test/editor3-scenario-parameters-ui-release-focused-01.log`.
Release Core also passes exit 0 in
`Test/editor3-scenario-parameters-core-release-focused-01.log`.

Debug final build `editor3-scenario-parameters-build-debug-07.log` and Release
`editor3-scenario-parameters-build-release-01.log` are terminal exit 0, both
with zero warnings/errors. Sessions 35185/2507/68305 are terminal, not live work.
Four minimum-width renders are retained under
`Test/scenario-parameters-ui-render-02`; EN project and RU native views were
inspected with full comparison captions visible. Release retains another four
under `Test/scenario-parameters-ui-render-release-01`.

## Remaining scope

Complete all source-specific named operand catalogues and parameter labels,
the actual parent windows, units/time arithmetic, dependent controls, nested
creature flags, point/rectangle/gate workflows and original resource/audio
selection. Preserve the four alias pages and camera selector when composing
these controls. Verify actual EN/RU pages and final layouts in both formats,
then run all five gates on frozen C#/assemblies and compare the complete ordered
94-error baseline. No full regression is accepted for this new code yet; the
accepted description checkpoint predates these edits. The full original-editor
GUI/dialog/audio/hardware/DPI/game scope remains active.
