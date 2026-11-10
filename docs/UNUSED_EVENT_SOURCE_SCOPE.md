# Unused-event cleanup: compiler source and integration scope

Read-only source audit, 2026-10-07. This document does not implement cleanup or claim acceptance.

Follow-up: the [Core prerequisite](UNUSED_SCRIPT_REMOVAL_CORE.md) implements immutable analysis, monotonic revision/fingerprint guards and one reversible removal. The later [human Test/Compile workflow](UNUSED_SCRIPT_REMOVAL.md) implements actual EN/RU commands/decisions and records terminal full acceptance. References to missing implementation below describe the audit baseline. Whole-editor and historical-GUI/game acceptance remain open.

## Actual call order and mutation

`Compiler_Compile.c:47–105` exposes Compile, Test and Run, all using `Compiler_BeginTask`. `Compiler_Task:131–141` always calls `Compiler_TaskTest` before `Compiler_TaskGenerate`; generation separately returns when disabled or failed. Thus the unused-event question is not exclusively an export operation: original explicit Test also executes it. Modern automatic/read-only diagnostics must remain nonmutating, but faithful explicit test/compile workflows need a separate consent step, not only a new disconnected cleanup utility.

`Compiler_TaskTest:495–508` checks version, terrain, players, placed things, areas, gates and finally triggers. `Compiler_TaskTestTriggers:1068–1185` skips when already failed; traverses every `MapActionPoints` link root; counts unmarked conditions/actions; reports separate counts; and asks Yes/No only when traversal succeeded and there is unused work. Yes removes those records from live editing lists. No leaves all records. Temporary marks are later cleared. The emergency branch instead removes every non-area owner binding; reproducing that automatic destructive repair is not required or safe.

`Compiler_Generate.c:1439–1443` writes counts from the complete remaining MapTriggers/MapActions lists. `Compiler_GenerateTriggers:1488` and `Compiler_GenerateActions:1676` loop all remaining entries, not only marked/reachable events. Therefore keeping unused events must retain them in both the project and generated native collection. Removing them must be an actual editing transaction with Undo, not output-only filtering.

## Edges, validation and non-repair

Project IDs are signed shorts: positive conditions, negative actions. Zero ends a link. A condition follows OR and AND, selecting condition/action by each target sign. An action follows only next. Native action ChildAnd bytes are reserved payload, not a second graph link.

`Compiler_TaskTestAction:1278–1392` also validates reachable action coordinates, required areas/camera-path areas, the crystal object and release-band conditions/area. Those semantic checks affect whether the question is offered; graph reachability alone is not complete compiler validation. Existing modern validators/compilation need auditing against these source checks before marking integration accepted.

Original recursive marking treats any second visit as “already used”, conflating shared acyclic branches with loops. Modern analysis must be bounded and iterative, preserving valid sharing while recognizing reachable cycles. Unreachable cycles can be candidates for explicit deletion because original traversal never enters them. Unreachable dangling nodes and unknown types must not trigger silent graph repairs; distinguish their removal preview from validation of retained roots. Duplicate/zero IDs, wrong signed categories, opaque layouts and genuinely missing reachable edges prevent ambiguous removal authority.

All serialized roots participate, including project bindings whose physical owner cannot be resolved. A missing owner is not permission to discard its reachable graph. Duplicate bindings also cannot be treated as absent roots; owner ambiguity and event-ID ambiguity are distinct conditions. Old scenario layouts need the verified reader/upgrade guard, not guessed offsets.

## Audit-baseline implementation seams and required acceptance

`ScriptGraph.Build` already exposes actual serialized roots and verified OR/AND/next edges. `ScriptBindings` is a diagnostic owner resolver, not the reachability-root filter. `ScriptBranchEditing.DeleteScriptBranch` handles one tree branch and deliberately rejects sharing; it is not an unused-event sweep. Its clear-internal-links/descending-offset deletion pattern can inform a fresh atomic operation, but retained records, links, backlinks, IDs, owners and opaque tails must remain byte-exact.

At this audit's baseline, `MainWindow.Compile_Click` validated, chose native base/output, compiled and validated the result without unused-event consent. Automatic `ValidateDocument` was read-only and remains so. The later workflow adds the immutable candidate preview, document identity/content and monotonic lifetime guards, explicit Keep/Remove/Cancel, fresh atomic operation and preview invalidation after edit+Undo. A byte fingerprint alone would allow reuse after Undo without a monotonic revision.

Further baseline checks identified three concrete integration hazards addressed by the follow-up, while preserving compiler safety:

- `NativeMapValidator` / `ScriptGraphDiagnostics` check dangling edges throughout the graph; the original compiler visits roots only before offering deletion. Running that global error gate first would prevent removing an unused dangling component the source can remove. Keep strong read-only diagnostics, but distinguish retained-root errors from unused candidates when deciding whether to offer consent. Keeping a malformed component still faces the existing safe compiler/export guards.
- `LegacyProjectCompiler` loops all project events and resolves their types, validates the entire input first, and requires crystal/gate targets for particular output mappings. It must compile all unchanged unused entries after Keep; Remove must precede compilation and trigger a fresh semantic/result check. Reachability cannot be an implicit compiler filter.
- At the audit baseline there was no monotonic document edit revision: outer Edit, Undo and Redo raised Changed, but the change counter was reversible and fingerprints could return to an earlier value. The Core prerequisite now adds monotonic EditRevision and verified bounded collection removal inside one transaction. Actual consent-window lifetime still needs integration; retained roots/backlinks cannot be cleared as a shortcut.

Acceptance must cover native/project shared branches; roots in all nine scopes; unresolved owners and duplicate bindings; reachable/unreachable cycles and dangling edges; action reserved bytes; unsupported/malformed layouts; stale/foreign previews; Keep/close/no-op; retained byte order/tails/backlinks; exact one-step Undo/Redo; save/reopen and compile-with-Keep versus compile-after-Remove; and actual EN/RU explicit test/compile controls. Do not count a Core preview, a detached utility button or a filtered output as the complete source workflow.
