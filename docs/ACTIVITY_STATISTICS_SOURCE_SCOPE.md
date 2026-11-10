# Activity statistics — current source scope

24 октября2026: [I16 закрыт конечной кодовой сверкой](I16_CODE_CLOSURE.md).12I/23M; ручная приёмка времени/окон остаётся. Датированные остатки ниже описывают прежние этапы.


23 October 2026: [save conversion now continues the live session](ACTIVITY_SAVE_CONTINUATION.md). Six deterministic cases and EditorUnifiedSaveChecks pass on the current DLL. The remaining I16 lifecycle/caller audit and manual acceptance are open. Lower dated results remain historical.


12 October 2026. The current canonical Core contains the KSTS codec, runtime
session, document load/new/Undo/save/compile integration. The application contains
EN/RU activity rows and mouse/paint/task bindings. This is implemented code, not
proof of complete historical GUI or physical event parity.

Current Release DLL core-model and document-workflow checks finished with exit 0;
see `artifacts/Evidence/HistoricalWorkflowAudit-20261012`. The full current CLI
regression is 1796 passes / 63 failures; see CURRENT_STATUS. No UI/game launch
was performed in this audit.

Earlier staging instructions were preserved verbatim in
`Archive/historical-documents-20261012/ACTIVITY_STATISTICS_SOURCE_SCOPE-before-current-audit.md`.
They are historical evidence, not alternative source trees or current blockers.
The frozen `ActivityStatisticsSource.json` records an earlier source audit; its
ProductImplemented flag is not the current product status.

## Active source call graph

Map_PaintElementSimple has no active callers. Active pen down allocates full-map
Undo, resets the previous point to (0,0), invokes Mouse_Moved immediately and
counts a nonorigin initial LINE even for equal/protected bytes. Origin matches
the sentinel and skips that first LINE; stationary moves skip. Free terrain
accepts protected initial clicks (LMB counts; writes remain protected), while
fixed object footprints reject before LMB. Valid equal fill counts once;
protected fill does not. Document Undo counts once, independently of workspace.

## Original wire and lifecycle rules

`Source/Includes/Stats.h` defines the 192-byte `MAPEDITSTATS` payload in the project `KSTS` section, version 1. It contains four 16-byte local `SYSTEMTIME` values, ten unsigned 32-bit scalars and an opaque 22-element extension. The independently captured layout, source hashes and 24 mutation sites are in `Test/activity-statistics-source-scope-20261009-01.json`.

The original general statistics page shows elapsed days/hours/minutes/seconds; left/right map clicks; Undo uses; object painting, fill, ordinary painting and random painting uses; project save count; and last save, creation and compilation dates. `StartTime`, persisted `RealTicks` and extension bytes are also retained even though this page does not directly display them.

Loading a project copies the stored payload and sets `TotalTicks` to persisted `RealTicks`. Elapsed time adds the source tick interval since map load/new, including elapsed time without focus. The source uses 32-bit storage. Source event sites, rather than a count of model transactions, determine painting/click/Undo counters.

Project saving increments the count and records elapsed ticks/local save time after file creation but before writing the statistics section. Temporary saving restores only the save count; its date/tick side effects remain. Compilation updates its date when the task finishes, including tasks that report failure. Any deliberate correction to these semantics must be explicit and tested; ordinary successful-save counters alone would be narrower than the original behavior.

Native game loading starts runtime statistics and derives creation/compile dates from the game header. Do not invent a game statistics file or alter installed maps to store these editor counters. Project statistics and runtime native statistics require distinct persistence rules. Viewing statistics must preserve malformed imported dates, unsupported versions, opaque extension bytes, map selections and history.

## Remaining acceptance

Exact nonpainting caller/counter behavior, broader no-op history, current actual
mouse/task/window flows and original GUI comparison remain open. Old offscreen
staging evidence does not prove current hardware input, audio, system dialogs,
DPI or game acceptance. Inspect and test actual source call sites when extending
these semantics; counting every transaction would be incorrect.
