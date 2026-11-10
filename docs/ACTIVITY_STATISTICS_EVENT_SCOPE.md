# Activity event integration — current scope

24 октября2026: [I16 закрыт конечной кодовой сверкой](I16_CODE_CLOSURE.md).12I/23M; ручная приёмка времени/окон остаётся. Датированные остатки ниже описывают прежние этапы.


23 October 2026: [save conversion now continues the live session](ACTIVITY_SAVE_CONTINUATION.md). Six deterministic cases and EditorUnifiedSaveChecks pass on the current DLL. The remaining I16 lifecycle/caller audit and manual acceptance are open. Lower dated results remain historical.


12 October 2026. Active code is in MapCanvas, LivePenStroke,
EditorWorkspace.ObjectPainting, NativeMapDocument.ActivityStatistics and the
Test/Compile task lifecycle. The activity panel displays EN/RU read-only rows.

Terrain/object eligibility precedes click counters. Accepted initial pen LINE,
equal geometric/fill operations, stationary movement and protected footprints
follow distinct source rules; see [the source scope](ACTIVITY_STATISTICS_SOURCE_SCOPE.md).
The runtime counters are independent of editable-byte Undo snapshots. Undo is
already counted by the document and must not be counted again by the workspace.

Test/Compile completion uses the captured target and records completion after a
task starts, including failure. Cancelled location selection does not start the
task. Replacement/closure and date behavior need actual UI acceptance; a model
CompilationTaskFinished check alone is insufficient.

Current Release ActivityStatisticsCoreChecks and ActivityStatisticsDocumentChecks
finished with exit 0 against the published Core DLL. Model checks cover literal
layout, opaque bytes, source-clock wrapping and failure/lifetime handling;
document checks cover factory/load/save, temporary/failed publication, Undo,
native header dates and AREA outer-signature retention. Evidence:
`artifacts/Evidence/HistoricalWorkflowAudit-20261012`.

The dated October9 offscreen mouse/task/render evidence is preserved in
`Archive/historical-documents-20261012/ACTIVITY_STATISTICS_EVENT_SCOPE-before-current-audit.md`
and the earlier experiment archive. It has not been rerun on the current DLL;
no editor or game was launched for this audit.

Full nonpainting caller/counter audit, current physical mouse/task/window checks,
original GUI comparison, audio/dialog/DPI and game acceptance remain open.
The complete goal is not achieved by these model checks.
