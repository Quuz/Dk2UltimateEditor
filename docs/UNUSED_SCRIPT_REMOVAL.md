# Explicit Test/Compile unused-event consent

Implemented 2026-10-07 from the [compiler source audit](UNUSED_EVENT_SOURCE_SCOPE.md), using the [bounded Core analysis and exact transaction](UNUSED_SCRIPT_REMOVAL_CORE.md). This document describes the human workflow, not completion of the whole editor.

## Actual commands and decision

The main-window **Validate toolbar button** calls `RequestScenarioValidation`; the actual **Compile DKM menu** calls `RequestCompileProject`. Both commit pending generic properties, run non-script/gameplay prerequisites and retained-root/layout/action checks, then offer an immutable read-only candidate window when eligible unused events exist. They do not invoke a disconnected cleanup utility or filter exported events. Automatic `ValidateDocument` remains full, read-only and nonmodal.

The EN/RU window lists IDs, condition/action categories and translated event types, with separate unused counts and retained count. Keep is the default, Remove is explicit, and Cancel/Escape/close stops the command. `Misc_Requests.c:125–136` zeroes `defaultgadget`; its button construction at 341–354 defaults to the last gadget for ordinary questions, so the original cleanup Yes/No question defaults to No. Modern Keep retains that nondestructive default; the separate Cancel-command button is an explicit extension, not silent deletion. Long type labels wrap and have tooltips; a minimum type-column width prevents WPF star-width collapse.

Keep preserves every event and compiles all remaining entries, including unused events. Remove changes the **live editing collection** in one exact Undo/Redo, followed by fresh full diagnostics and compilation validation. Unused dangling components can be explicitly removed despite full global diagnostics reporting them: those diagnostics remain visible, but do not incorrectly replace the retained-root eligibility gate. Retained cycles/missing links, unsupported identity/layout/scope and source action problems prevent the question. Keeping a malformed unused component still faces full compiler validation; no repair is performed.

If the user accepts removal and subsequently cancels the destination or generated-warning question, the already-confirmed live removal remains undoable; destination cancellation does not roll it back. Compilation writes only a newly generated output directory. The project input and native basis are never overwritten.

## Authority and lifetime

The shell checks its document identity, workspace identity, context epoch and Core monotonic revision/content fingerprint after the answer. Reentrant Test/Compile, edit+Undo, raw payload mutation, replaced-and-restored workspace, Unloaded and closed shell invalidate authority. Actual attached consent windows close on document/lifetime invalidation; merely constructing a window gives it no editing authority.

Generic properties are committed before the preview and again before applying an accepted decision. An input introduced during consent is retained as its own edit, invalidates that preview and cannot authorize removal. Cancel does not undo an independently committed earlier property edit. Language changes retain unchanged consent and translate candidate rows. Destination and warning replies are also rechecked before writing output, including edit+Undo while either question is open.

The production prompt delegates default to real modal/native dialogs and notices. Explicit injected decision/location/warning/notice seams let in-process WPF tests exercise the **same actual button/menu command routes** without interactive dialogs or user-settings writes. Their passing results do not claim physical modal/hardware/DPI behavior or in-game acceptance.

## Acceptance evidence

Focused Debug/Release EN/RU × native/project cases pass. They cover actual Validate and Compile events; immutable counts/list translation; real Keep/Remove/Cancel button events and window close; no-op; read-only automatic validation; exact live Undo/Redo; retained-cycle/non-script blockers; unused dangling Keep versus Remove; pending generic edits before and during consent; language switching; raw bytes, edit+Undo, reentrance, workspace replacement/unload and shell close; three-event Compile/Keep versus one-event Compile/Remove; canceled/stale destination and stale warning replies; and byte-identical disk inputs/basis. Core independently covers all serialized roots/scopes, DAG sharing, unresolved owners, duplicate bindings, version/identity/layout/action guards, native reserved bytes/tails, project alias retention, save/reopen and a 12,000-event graph.

Initial property-grid fixtures forgot to select/layout the actual properties tab after refresh. The corrected fixtures assert genuine BeginEdit/TextBox/commit behavior without weakening production guards. Rendering then exposed the collapsed type column; production minimum width/wrapping and an independent column-width assertion fix it. All four corrected final EN/RU native/project renders in `Test/unused-scripts-render-final-20261007/` are inspected. Earlier failed logs/images are preserved.

Final terminal gates are recovered:

- Debug/Release solution builds: zero warnings/errors. Focused WPF cases pass in both configurations; final renders are inspected.
- Full Debug Core: **77 groups / zero failures**, exit 0, `Test/unused-scripts-final-core-debug-20261007.json`.
- Both complete loaded WPF configurations: **234 passing cases each**, exit 0, `Test/unused-scripts-final-ui-{debug,release}-20261007.log`, terminal `WINDOWS UI SMOKE PASSED`. The real Alcatraz native and Ancients text/project fixtures remain included.
- Release 578-map corpus: **77 synthetic + 560 native + 530 supported-text + 559 original-policy = 1726 passes / 94 failures**, exit 1, `Test/expanded-corpus-unused-scripts-final-20261007.json`. The complete ordered failure array exactly equals the copy-consent baseline and the Core-prerequisite report. No new corpus refusal is introduced; 94 checks are not 94 maps or missing features.
- Original `Source/`/`Binaries/` have no tracked diff; `git diff --check` is clean. UI fixtures explicitly compare input/basis disk bytes; corpus previews do not edit installed inputs.

This closes the named functional cleanup workflow under these tests, not whole-editor completion or physical modal/hardware/DPI/game acceptance. Semantic/catalogue/historical-format/original-GUI/game requirements remain open in [FEATURE_PARITY](FEATURE_PARITY.md). The next [source-confirmed named scenario variable/timer workflow](SCENARIO_NAMES_SOURCE_SCOPE.md) distinguishes already-editable raw project aliases from missing named UI/reference integration and native editor metadata requirements.
