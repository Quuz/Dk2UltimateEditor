# Creature-spawn parameter parent

Current canonical project (17 October 2026): these workflows are in the common
`src/DK2MapEditor.App/Editing/ScenarioParameterWindow.cs` and current Core.
No Test/staging copy or held patch is needed to build. Dated evidence below
belongs to its historical snapshot; current results and manual GUI limits are
in [CURRENT_STATUS](CURRENT_STATUS.md) and [REMAINING_WORK](REMAINING_WORK.md).

The current common parameter window includes the original ACTION_ADDCREATURE page (7203):
creature, player and level choices; unsigned raw X/Y controls limited to 999;
the original seven options; area-to-top-left and actual canvas point picking.
An area is optional. Coordinates are stored directly, without adding one,
and manual source-range coordinates can exceed the map dimensions. Interactive
map picks require a point inside the map. Area identity is never stored as X/Y.

The options child updates only the parent draft. Its six editable flags use
source mask 0xBD; placement bit 0x40 remains disabled. Unknown bits, including
project DWORD bits beyond the native byte, survive every flag combination.
No-op Apply preserves imports. Parent Apply commits choices/flags/coordinates in
one Undo; child and parent cancellation, reload, source changes, raw mutation,
pending generic commits, document replacement and closing invalidate authority.
Canvas picking preserves ordinary selection and copy data, and cancellation or
parent reload/close cleans its handlers and pending mode.

The independent fixture contains the complete original parent/nested/coordinate
source bodies and hashes for 17 original files. Core checks apply all 62 named
choices, all 64 editable-flag combinations over five native/six project import
patterns, disabled/unknown preservation, literal offsets, area/point conventions,
history, save/reopen/compile, invalid/foreign/stale/raw guards and source bounds.
Actual WPF checks exercise all choices and flag combinations in EN/RU × both
formats, the ordinary toolbar, real canvas, translation, pending generic commits,
reentrance, input changes during commit, cancellation and lifetime.

Final isolated Core and WPF builds have zero warnings/errors. Both configurations
pass 7 Core groups and 32 WPF cases (spawn plus shared alias/camera regressions).
The -03 spawn UI runs each repeat four cases and retain 20 final renders. A test
deliberately changes the disabled checkbox to verify it cannot clear stored bits;
the driver now restores its real checked presentation before rendering. Only
this driver line changed after -02 acceptance. App/Core/Validation assemblies
remain identical, independently hash checked. Final evidence:
`Test/scenario-add-creature-staging-20261008-01/spawn-parent-focused-verified-03.json`
and `candidate-snapshot-02.json`. The initial nullable build failure and all
earlier passing logs/renders are preserved.

The dated staged delivery above is historical and has been integrated into
current src/tests. Creation/type-change defaults are a separate source workflow;
existing-record tests alone do not prove their physical GUI acceptance. See
[source defaults inventory](SCENARIO_CREATION_DEFAULTS_SOURCE_SCOPE.md) and
CURRENT_STATUS for current implementation and evidence. No held patch remains
a prerequisite for building the editor.
