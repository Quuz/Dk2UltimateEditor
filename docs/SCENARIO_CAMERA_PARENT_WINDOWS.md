# Camera-path parent and library composition

Current canonical project (17 October 2026): these workflows are in the common
`src/DK2MapEditor.App/Editing/ScenarioParameterWindow.cs` and current Core.
No Test/staging copy or held patch is needed to build. Dated evidence below
belongs to its historical snapshot; current results and manual GUI limits are
in [CURRENT_STATUS](CURRENT_STATUS.md) and [REMAINING_WORK](REMAINING_WORK.md).

The common ACTION_CAMERAPATH page now has the original unsigned path input
(0–9999 in projects, additionally limited to 0–255 by native storage), an area
identity selector, ceiling visibility and the original 52-profile library.
The source's TRUE flag means ceiling shown (operand 3 = 0); FALSE means hidden
(operand 3 = 1). The prior cell/area captions and explanatory hint were corrected.

A library selection updates only the private parent inputs. Area selection is
retained, and the parent commits path, area and ceiling in one Undo transaction.
Child Cancel retains only previously accepted draft selections; parent Cancel
discards all private changes. Reload, source edits, document replacement and
parent close invalidate the active child. Unknown imported DWORD values display
unsigned and survive a no-op Apply, including values beyond the manual range.
Missing areas block Apply, matching the source prerequisite.

Independent Core tests read original profile IDs/flags, check all 52 selections
in both formats, literal byte locations, opaque/unrelated bytes, private drafts,
numeric bounds, area identities, Undo/Redo, saved/reopened/compiled operands and
stale/foreign/raw mutation guards. Actual WPF tests cover EN/RU × both formats,
the common toolbar, every profile, private child Apply, source ceiling captions,
translation, generic pending commits, reentrance, reload/cancel and lifetime.
The existing standalone library's original bitmap pixel checks also pass.

Final isolated builds have zero warnings/errors. Debug/Release each pass six
Core groups and eight WPF cases. Each retains 20 parent/child renders and four
standalone library renders. The initial -01 UI checks passed, but visual review
found the stale explanatory hint; accepted -02 checks include the corrected
standalone and private-parent hints. Proof:
`Test/scenario-camera-parent-staging-20261008-01/camera-parent-focused-verified-02.json`.

All changes, four alias parent pages and 27 unsigned display fixes were applied
to production together in a verified 27-file delivery, with eight new files.
The previous 281-source baseline's complete five-gate run is accepted and all
workers were gone before application. Original source inputs remain unchanged;
previous edited bytes are backed up under Test. Receipt:
`Test/scenario-alias-camera-unsigned-20261008-01-applied.json`.

The historical checkpoint covered64/69 pages. That count is not the current
remaining inventory. Camera composition is in the common current window;
physical GUI/library/audio/DPI/game acceptance remains separately tracked.
