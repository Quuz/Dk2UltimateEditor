# Original camera-path selector: source scope audit

This is the preserved pre-implementation audit. The managed chooser is now implemented; see [implementation and acceptance evidence](CAMERA_PATH_SELECTION.md). Preserve the original DLL/source/bitmaps. The audit alone is not implementation acceptance.

## What the original actually does

`Source/Includes/CameraPaths.h` passes only `CAMERAPATH { LONG PathId; BOOL ShowCell; }` to the library. `Source/Sources/Libraries/Libraries_CameraPaths.c` exposes `Editor_SelectCameraPath`, not a trajectory-file editor. Its dialog lists existing path IDs, displays a static 256 × 192 diagram and a read-only description, and returns a selected ID and cell-versus-area default. It does not create/edit trajectory keyframes or write a camera-path file. Such an editor must not be invented as an original parity requirement.

`Source/Sources/Datas/Datas_CameraPaths.c` has **52** entries before sentinel `-1`: 0–3, 50–67, 100–101, 150, 201, 203–219, 221–222 and 230–236. Twenty-four have `ShowCell = TRUE`; twenty-eight have `FALSE`. All **52** corresponding BMP files are present in `Source/Bitmaps/Chemins de caméra`; resource IDs are path ID + 1000 in `Source/Resources/CameraPaths.rc`. Descriptions are in `Source/Sources/Datas/Texts_CameraPaths.c`.

The initial library lookup sets selection/description for a known ID but does not invoke its change handler. Unknown IDs remain unselected with a black preview. A selection change sets both output values and redraws the diagram. The caller ignores output on cancellation. A modern draft should not implicitly rewrite imported IDs merely by opening the chooser; confirmed selection should use the source defaults, and Cancel must preserve its caller's draft.

## Action integration

`Source/Includes/Triggers.h` defines `ACTION_CAMERAPATH = 0x1F`. `Source/Sources/Triggers/Triggers_Actions.c` opens the selector from control 315 only for that action. The action has:

- `param1`: path ID;
- `param2`: selected area;
- `param3`: 0 for the cell radio (control 200), 1 for area mode (201).

Consequently `ShowCell = TRUE` maps to **param3 = 0**, not 1. The selector must retain area/other action fields. Original compiler output in `Compiler_Generate.c` contains three bytes for these parameters, followed by a reserved zero byte and four reserved zero bytes. The existing modern compiler-derived event schema already exposes these values; the missing feature is the visual bilingual selector and its draft/atomic editing integration, not basic numeric access to the action.

## Required implementation / verification

Use all 52 preserved diagrams and translated EN/RU descriptions. Cover known and unknown initial IDs, exact source defaults/inversion, all entries, search/selection, Cancel versus explicit Apply, pending property commits, source/document changes, unrelated area/graph/reserved bytes, no-op/undo/redo, native and project formats, save/reopen/compile and actual rendered controls. Treat dynamic loading of the historical 32-bit DLL as unnecessary for a managed equivalent; do not execute it solely to extract already-present data. Historical-GUI and in-game camera behavior remain separate checks.
