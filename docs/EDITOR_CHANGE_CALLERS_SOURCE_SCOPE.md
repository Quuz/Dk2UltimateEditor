# Original editor change callers — current scope

24 октября2026: [I15 закрыт конечной сверкой](I15_CODE_CLOSURE.md). Все89 increments,5 rollback decrements и5reset/restore assignments сопоставлены; код/тесты не менялись. Остаток11I/23M; датированные результаты ниже относятся к своим этапам.


User decision, 2026-10-15: retain current Undo for accepted patrol pauses and
changed scenario parameters. This is an approved product choice, not an
outstanding request to remove Undo. Equal scenario Save keeps its current
counter-only behavior. This approval does not cover every other historical
difference. See [current history](EDIT_HISTORY.md).

Current source and product scope, 12 October 2026. The canonical implementation
is `src/`, built by `DK2MapEditor.slnx`; current executed gates and unresolved
failures are in [CURRENT_STATUS](CURRENT_STATUS.md). No archived candidate or
old process must be recovered or waited on to build the editor.

## What a caller counter means

An editor acceptance counter is distinct from changed bytes, dirty state,
document revision, activity statistics and Undo entries. The source sometimes
accepts equal values and increments once, sometimes increments for every created
or shifted node, and sometimes increments without creating Undo. Cancel and
early returns must be audited per operation. Generic raw editing APIs retain
their changed-only/no-op semantics. One `document.Edit` is not evidence of every
original caller's behavior.

The current static increment inventory in
`artifacts/Evidence/SelectionTerrain-20261012/nonpainting-caller-inventory.json`
records 89 original increment sites, 83 outside the two painting files. These
counts describe source locations, not completed features or remaining bugs.
Earlier 94-site figures included a different WRITE/decrement inventory. Do not
subtract passing groups from this static location count or use it as readiness.

## Implemented caller paths and verification boundaries

- Parent map properties: accepted equal Apply, resize, source node shifts and
  consented crop removals have explicit source counts within one atomic history
  operation. Nine anchors and retained noncounted camera/CPU/action data are
  covered by model checks. See [map settings](MAP_GENERAL_SETTINGS_UI.md).
- Region properties: independent parent/area/effect drafts, accepted equal Apply,
  point/rectangle choice and private nested hero-gate generation compose into
  one outer Apply/Undo. Generation counts individual source allocations plus
  the parent acceptance. See [region session](REGION_PROPERTIES_SESSION.md),
  [generation counters](HERO_GATE_GENERATION_COUNTERS.md) and
  [private generator](REGION_GATE_GENERATION.md).
- Patrol commands: explicit Start/Pause/Link/Append/Insert/Remove/Clear callers
  count accepted commands once, including equal data. Cursor-only quick Start
  counts zero. Modern pause and no-byte Undo remain deliberate documented
  differences from source history. Generic graph APIs remain unchanged. See
  [patrol callers](PATROL_EDITOR_CALLERS.md).
- Patrol initial manager grouping: the separate read-only source projection
  preserves file-order roots, predecessor insertion, loops and branch cleanup,
  with no edit counts or history changes. Owned manager sessions now retain local
  occurrence lists, empty roots, source indirect-change predicates and explicit
  reload; accepted operations write actual fields atomically. These have native/
  project byte/counter/history/lifetime checks. Owned Append/Insert selection and root/point availability now have independent
  candidate/pause/cancel/one-acceptance/lifetime checks and an EN/RU child window.
  Owned selected/all warning requests now implement runtime suppression and
  read-only guarded replies. Owned Yes/No removal now adds a separate runtime
  bit, checked-No disabling, read-only decline/close and exactly one guarded
  manager acceptance ([removal requests](PATROL_REMOVAL_REQUESTS.md));
  physical modal timing/implicit tree notifications
  and selector interaction remain gates. See [warning requests](PATROL_WARNING_REQUESTS.md),
  [area selector](PATROL_AREA_SELECTION.md) and
  [source grouping](PATROL_SOURCE_GROUPING.md) and
  [local lists](PATROL_MANAGER_SESSION.md).
- Scenario naming and wizard/property routes have explicit caller code, rather
  than globally changing generic no-op behavior. The live naming window calls
  `ApplyEditorScenarioNameDraft`; the historical claim that it still called only
  generic `ApplyScenarioNameDraft` is obsolete. Parameter counts, alias-parent
  Save and spatial choices have their own model groups and remaining full-scope
  acceptance in [FEATURE_PARITY](FEATURE_PARITY.md).
- Shared global-definition pages use private composed contexts and guarded
  atomic Apply. Source GLOBALVARS page Save callbacks compare the whole shared
  draft and do not increment once per visited page. Equal parent map-property
  rules must not be copied indiscriminately to globals. See
  [global rules](GLOBAL_DEFINITION_RULES.md).

- Cut/Paste commands now use source accepted1+each target removal and retain
  one parentUndo even equal. Copy remains read-only; inserted Paste objects do
  not add increments. Generic changed-only methods remain separate. Source
  capture/32models/literal history/counter/activity guards and prepared-only
  EN/RU UI scope: [selection callers](EDITOR_SELECTION_CALLERS.md).

- Rotate/Reflect editor commands now use source acceptance1/oneparentUndo even
  equal. The nested physical-removal helper adds one per moved object, including
  protected-edge objects; size/protection/ID/link rules remain unchanged;
  generic raw transform retained. Composed source capture/32 + 80 literal models and compiled-
  only UI scope: [transform callers](EDITOR_TRANSFORM_CALLERS.md).

The pen caller source route and all39 available cursor-footprint rows have now
been compared with current Core on14 October. The78 native/project footprint
checks and live-pen models pass on the same Core as the last full corpus;
physical capture/timing/mouse/audio remain unproved. Current drawing history
documentation replaces obsolete Test/candidate instructions; see
[drawing and history](EDITOR_DRAWING_HISTORY.md).

These implemented paths do not prove all 83 nonpainting sites or every original
UI interaction. The complete feature ledger remains authoritative. Original
GUI, physical EN/RU controls, system dialogs/audio, hardware/DPI and edited-map
game behavior require their separate acceptance. Current model/build checks
must be read at their actual scope; older UI results do not execute new controls.

## Historical evidence

The previous document, including root411/candidate staging and obsolete waiting
instructions, is preserved verbatim in
`Archive/historical-documents-20261012/EDITOR_CHANGE_CALLERS_SOURCE_SCOPE-before-current-audit.md`.
Its dated commands/status are historical evidence, not current instructions.
The archive is not a build dependency or a source restoration chain.

Continue from [the saved current state](CONTINUE_AFTER_CODEX_RESTART.md).
No Computer Use or automated editor/game launches; user-run checks are in
[manual acceptance](MANUAL_ACCEPTANCE_RU.md). Preserve Source/Binaries, installed
maps and uncommitted work; no reset, commit/push or installed-map repair.
The whole goal stays active while any required gate remains unproved.


Single-object canvas and dialog routes now call ApplyEditorObjectPainting: accepted
1 + all opposite removals, one Undo even equal, and one UsageOfPaintThings.
The nested helper is included in the source proof. Raw APIs remain unchanged.
58 model cases and compiled-only WPF checks are described in
[object painting callers](EDITOR_OBJECT_PAINTING_CALLERS.md).
