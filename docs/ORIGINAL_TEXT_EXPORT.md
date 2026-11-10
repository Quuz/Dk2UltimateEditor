# Explicit original-editor no-scenario MAP export

File → Export text MAP with complete editable copy now has two explicit choices:

- Project supported scenarios: the existing projection and full input-graph validation remain the default core API behavior.
- Original editor: no scenarios in MAP: writes `TRIGGERS` with a zero count, skips projection of all scenario data and owner links, and preserves their exact stored bytes in the mandatory complete editable copy.

Both choices preview limitations, require the existing confirmation, choose a parent folder and publish a new unique directory containing `Map.map`, `EditableCopy/` and bilingual `Export-notes.txt`. The original-mode warning explicitly says that MAP contains **no scenarios**, even when the input has none. The report identifies the selected policy, omitted verified event count and nonzero owner-link count. Unknown/opaque scenario data remain in the full snapshot; the counts are not a claim to decode future layouts. Only an explicit original-mode command selects omission; unsupported normal export never silently retries this mode.

## Preserved source evidence

`Source/Sources/Files/Files_Text.c: Text_GenerateFile` writes `szTextMapTriggers`, a newline, then `Files_WriteData(FileHandle,szNumber,0,DATATYPE_TEXTNUMBER)`. It never iterates `MapTriggers` in this writer. `Files_Functions.c` formats `DataSize` with `wsprintf(Number,(char *)Data,DataSize)`, and `Texts_Global.c` defines `szNumber` as `%ld`; the stored count is literally zero. This is distinct from `Text_ExtractTriggers`, which can **import** supported scenario syntax.

`Files_Save.c: Files_SaveMapQuick` calls `Files_SaveCompatibility` and then `Text_GenerateFile`. The compatibility report is an optional lossy-format warning, not an instruction to export the graph. The modern supported-scenario writer is an extension; no-scenario policy restores this original workflow without removing the extension.

The policy name describes scenario handling, **not** complete byte/text/UI equivalence to the old exporter. Existing terrain, entity, band, metadata and variable projection rules remain unchanged. The historical ASCII format still cannot contain all editable properties; retain/open EditableCopy for further work.

## Validation and preservation boundaries

`TextMapScriptMode` is an explicit validated enum. `TextMapFile.Project/SaveCopy` default to SupportedScenarios and return the selected policy in the projection. OriginalNoScenarios invokes ordinary non-script validation with script checks excluded, does not build its script dictionary or traverse scenarios, and emits an empty TRIGGERS section. All non-script structural guards, bounds, band-layout checks, text limits and generated-text parsing still apply. The complete snapshot always comes from the original in-memory document, never from reimporting the lossy MAP.

Normal interactive validation and compilation continue checking scripts by default. Zero/duplicate IDs, dangling links, unknown types and cycles are not repaired or normalized. Source tiles, owner fields, scenarios, aliases, unknown bytes and dirty/undo/redo state remain unchanged. Existing output directories/files are refused; staging is removed on failure and publication remains atomic. The bilingual disk report records omission and the path of the full source copy.

The two menu children have explicit closed-document guards and share the same non-modal preview/warning preparation used by the real click handler. Preview chooses the mode from the actual command identity; unknown commands fail. After confirmation, SaveCopy receives the preview's policy. The click handler still commits pending property fields before preview and still handles cancellation/error dialogs; the tests below do not automate those modal dialogs.

## Verification

`OriginalTextExportChecks` reads the original writer and number formatter independently. Fourteen native/project fixtures cover valid graphs, zero IDs, duplicate IDs, dangling owner roots, dangling edges, cycles and unknown types, with all nine scope owners. Tests compare literal zero-count output, omitted counts, parsed/reopened scenario-free MAPs, byte-exact complete native sections/project serialization, bilingual reports, immutable source/history/redo, existing-output refusal, cleanup, unchanged supported defaults, invalid enum rejection and non-script coordinate rejection. Ordinary validation still reports graph errors after the new mode is used.

`TextExportSmoke` adds four EN/RU × native/project actual-MainWindow cases: real submenu membership/enablement, both mode previews, live resource translation, explicit warning and mandatory-copy text, unchanged bytes/history, dangling-graph refusal versus explicit omission, published/reopened full snapshots, invalid non-script geometry and closed/unknown-command guards. No visible windows, user-settings writes or modal file/confirmation dialogs are used.

The corpus runner retains existing `text projection` checks unchanged and adds separate `original text projection` checks for every KWD: parsed scenario-free text, zero root targets, inspector acceptance and exact full native snapshots. Report totals now include an additional check per map; compare categories, not aggregate failed-check totals alone. A passing no-scenario projection does not prove that supported scenarios can be projected or that the map plays correctly.

All process handles are terminal: Debug/Release solution builds have zero warnings/errors; all 62 synthetic groups and all 160 WPF cases pass in both configurations. `original-text-debug-20261006.json` contains 62 passes and no failures. The Release `expanded-corpus-original-text-20261006.json` records 560 native, 530 supported-scenario text and 559 original-policy text passes across 578 KWD files. Its 94 failed checks comprise nine inspector errors, eighteen native refusals, forty-eight supported-text refusals and nineteen original-text refusals; these overlap and are not 94 distinct maps.

The original policy permits 29 maps previously refused solely for scenario errors. `XXLevel10.kwd` still fails because band hero coordinates exceed its map bounds; omitting scenarios does not bypass this non-script guard. The other eighteen original-policy refusals are nine missing tile companions and nine truncated Variables files. Historical-GUI/native-dialog/game acceptance, additional obsolete/opaque project fixtures and full text-export equivalence remain open.
