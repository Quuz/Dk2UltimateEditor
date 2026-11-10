# Scenario spatial pages and creation rectangle

Current canonical project (17 October 2026): these workflows are in the common
`src/DK2MapEditor.App/Editing/ScenarioParameterWindow.cs` and current Core.
No Test/staging copy or held patch is needed to build. Dated evidence below
belongs to its historical snapshot; current results and manual GUI limits are
in [CURRENT_STATUS](CURRENT_STATUS.md) and [REMAINING_WORK](REMAINING_WORK.md).

CHANGESQUARE, CHANGEOWNER and DESTROYGATE use the current common parameter
window and private spatial composition. ACTION_SAY has its separate embedded
speech component in that same window. Wider parity and physical acceptance
remain open.

The independent ScenarioSpatialSource.json fixture captures original
Triggers_Actions.c Init/Check/Store/Save/callback and gate helpers, three
resource pages7209/7221/7208 and original compiler branches. Its three source
inputs are SHA256 checked. Coordinates on CHANGESQUARE and CHANGEOWNER display
stored-minus-one, accept0..999 and store1..1000. Map point/optional area use
actual normalized coordinates/area identities; spawn retains its different
raw-coordinate convention.

ScenarioSpatialParameters.cs adds private point/area/gate composition.
Project gates use actual IDs. Native gates use exact one-based WORD X/Y at
offsets12/14; GAMEGATE.bGateID is a0/1 kind, not a project ID. Keeper hearts,
future/unverified layouts and out-of-range coordinates are excluded. Missing
or ambiguous imported references stay untouched on opening. Duplicate project
IDs cannot be selected; duplicate native coordinates retain ambiguity.

Actual common parameter toolbar opens all three EN/RU pages. Named terrain/
owner controls use original catalogues; point and area compose into a private
draft. Native gate picker displays coordinates; project picker displays
identity/name/info. Unknown references display retained stored operands.
Apply is atomic, Cancel preserves the map, and translation retains inputs.
Reload/Cancel/owner replacement clean up actual canvas-pick authority.

The new creation-only rectangle option follows the original Save loop:
x advances first, then y; each inserted action links after the previous one,
preserving any existing tail. Finish applies all actions in one live Undo and
selects the last inserted action. Submitted Back/Next/Cancel retain private
rectangle context. Finish resets rectangle mode and restores temporary
coordinates to the first cell. Existing-event editors have no rectangle
expansion control. Metadata revisions invalidate previously prepared plans;
free identity capacity is checked before expansion.

The embedded creation explanation now describes Back/Next/Finish. The
rectangle checkbox also invalidates Finish when changed during a pending
generic commit, restores its submitted state and prevents stale composition.

Diagnostic logs remain preserved:

- spatial-core-debug-01.log initially attempted a duplicate identity through
  ordinary SetField. That correctly rejected it; the imported-duplicate test
  now constructs malformed imported bytes independently.
- spatial-ui-debug-02.log used a shared reflection helper that rejects null
  properties; the gate placeholder has a legitimate null key, now read
  explicitly.
- candidate-snapshot-01.json and final-01 logs preserve the failed inherited
  coverage expectation. ScenarioNamedPageChecks now includes the three pages
  from the independent original spatial fixture; it still asserts exact
  supported-page coverage.

The final02 snapshot fixes306 C# sources,12 fixtures and16 assemblies.
Four final02 Debug/Release builds are terminal0 with zero warnings/errors.
Final focused driver20415 is terminal0 and its actual worker8572/14088 and
isolated dotnet test processes are gone. Both configurations have13 passing
Core groups and40 actual WPF cases, including3 new Core groups/16 new WPF cases.
verify_scenario_spatial_focused.py02 ranONCE exit0 and wrote immutable
spatial-focused-verified-02.json, VerifiedSpatialFocus true. It confirms matching
sources, DLLs, logs,108 rendered images/configuration and unchanged accepted
creation/production snapshots. SnapshotSHA256:
1b0af416ece5f7bd0e72d56fb6d947f1acf11dd28603f388feb53eda15f1c412.
Preserve this accepted copy and both historical snapshots/proofs.

Independent Core checks cover all34 terrain/owner choices for CHANGESQUARE
and7 owners for CHANGEOWNER, exact literal project/native writes with opaque
bytes/repeat retained, optional-area top-left, imported zero/high values,
no-op/Undo/Redo/reopen/compiler, and stale/raw/foreign owners. Gates cover
real IDs/native XY, keeper-heart exclusion, unknown/duplicate identities and
literal compilation. Rectangles cover row-major chain/tail, exact payloads,
project compiler literals, one Undo, canceled context/Finish reset,
metadata/owner/raw/foreign/closed guards and identity-capacity rejection.

Actual WPF checks use common toolbar/Add and MapCanvas selection, EN/RU
translation, unknown/missing/private creation gate behavior, rectangle
Back/Next/Cancel/Finish, first/last cell behavior, generic-commit invalidation
and exact history. Minimum540x420 and700x580 images are retained; the RU
project cell-selection image was inspected and its lower fields are reachable
through the scroll view. Actual Release minimum RU/project rectangle and
RU/native gate-selection images were also inspected; text/control labels are
readable and the embedded creation hint uses Back/Next/Finish wording.

The old alias/camera production run, live-worker observations and pending
SAY staging instructions were historical. Current speech/spatial/wizard code
is in src, with current model/corpus status in CURRENT_STATUS. Earlier renders
and94-error reports do not certify the latest physical GUI/game behavior.
