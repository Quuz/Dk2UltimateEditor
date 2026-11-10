# Bilingual original camera-path selection

Search050 теперь соответствует отображаемой строке050. Прямая библиотека
доступна через MCP25; [проверка проектной ветки/EN и остаток](CAMERA_SEARCH_LIVE_20261103.md).

[Настоящий проход и исправление 3 ноября](CAMERA_PATHS_LIVE_20261103.md): библиотека в мастере получает видимого владельца; камера/флаги появления EN/RU/native/project —24модальных открытия PASS. Остальной ручной объём сохранён.

The source selector is now implemented as a managed WPF equivalent. It is a chooser for the original predefined movements, not a trajectory/keyframe editor; see [the preserved-source audit](CAMERA_PATH_SELECTION_SCOPE.md).

## Catalogue and diagrams

The Core catalogue reads all 52 source IDs and their ShowCell defaults directly from the embedded preserved `Datas_CameraPaths.c`. It verifies numeric bitmap identities (ID + 1000), uniqueness and complete EN/RU descriptions in `CameraPathDescriptions.txt`. All 52 original BMPs are embedded without re-encoding or modifying their source files. Frozen WPF previews retain the original 256 × 192 pixels. No old 32-bit DLL is loaded.

Select a camera-path action in the record properties, then use **Choose camera path**. The window offers all source IDs, search by number/translated description, the original diagram, translated movement details, default cell/area mode, Apply and Cancel. Search and live language switching retain the chosen draft. Unknown imported IDs remain unselected, with no invented diagram, until an explicit known choice is made. Opening/searching/selecting/canceling does not edit the document.

The window displays static diagrams, as the original library did; it does not simulate a rendered game cinematic. English/Russian descriptions preserve the movement information with edited wording, not French strings in an untranslated popup.

## Atomic action editing

`CameraPaths.Supported` requires the compiler-derived native 24-byte action layout or verified current project ACTION schema. A document-owned draft captures exact action bytes, identity/location and its original path ID. Apply accepts only source catalogue IDs and rechecks the current record/baseline; foreign/stale/unsupported actions are refused. Only param1 (path) and param3 (mode) change, in one undo transaction. Param2 (area), graph links, IDs and unrelated/reserved bytes are retained.

The source `ShowCell = TRUE` maps to **param3 = 0**, and FALSE to **1**. Choosing an already canonical path/mode is a byte-preserving no-op. Unknown imported values are retained during draft creation/cancellation; choosing a known path deliberately sets its documented default mode.

The old library's initial lookup does not call its selection-change handler; its caller zeroes ShowCell first. This can return area mode for an unchanged initially known cell-mode path. The managed chooser intentionally applies the selected entry's documented default on explicit confirmation, including an initially known choice. It does not reproduce that initialization defect. Merely opening or canceling still changes nothing.

Workspace Apply commits valid pending generic properties, then verifies the captured action. If those properties changed the record, the chooser refuses its stale draft rather than overwriting them. Such a generic edit remains its own normal undo operation. Inline bilingual failure feedback replaces blocking chooser error dialogs. Document replacement/unload closes the active modal chooser; closed windows clear their callback/image/selection and cannot apply. A factory-created test window also checks current document and selected record before applying.

## Verification and remaining acceptance

`CameraPathChecks` independently reads the preserved catalogue, diagram headers and caller inversion. For every path in native/project documents it compares all bytes against independently encoded expected edits, checks single notifications/no-op, exact undo/redo, full editable-section save/reopen and repeat serialized output, and project-to-native compilation. Native opaque argument bytes are seeded and retained. Unknown initial IDs, changed-area stale drafts, foreign documents, invalid IDs and unrelated camera action types are covered. Corpus native round trips additionally retain full camera-action projections across reopen and metadata undo.

Four EN/RU × native/project `CameraPathSmoke` cases use actual workspace/property controls and chooser widgets. They decode all 52 embedded images and compare every BGRA pixel with the source BMPs; exercise selection/search/description/default mode, live translation, unknown-path refusal, Cancel, atomic Apply/undo/redo, actual pending DataGrid commits, inline stale errors and replaced/closed-source cleanup. Four PNGs in `Test/CameraPaths-20261007-b` were rendered from actual controls; English-project and Russian-native layouts were inspected (the first render set was also inspected). Native modal interaction/keyboard/hardware/DPI and historical-GUI/in-game cinematic behavior remain separate acceptance work.

Focused Core and all four WPF cases pass in Debug/Release. Both solution configurations build with zero warnings/errors. Full Debug synthetic regression is terminal with **66 groups / zero failures** (`camera-paths-debug-20261007.json`). The expanded 578-map Release corpus is terminal with **1715 passes / 94 failed checks**, including 66 synthetic, 560 native, 530 supported-text and 559 original-policy text passes (`expanded-corpus-camera-paths-20261007.json`). Its complete failure array matches the corrected inspector baseline exactly. Both full WPF gates are terminal with **176 passing cases in each of Debug and Release**, exit 0. Whole-editor parity and historical/game acceptance remain incomplete.
