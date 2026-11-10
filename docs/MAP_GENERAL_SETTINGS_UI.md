# General map properties parent window

Implemented 2026-10-08; full post-change regression is recorded separately below.
This completes the named parent workflow only when its acceptance gates pass;
it does not establish full original-editor parity or actual game acceptance.

## Source behavior and integration

The Edit menu and Properties toolbar open `MapPropertiesWindow` with a
`MapPropertiesSession` independent of the live map. The named EN/RU fields
cover display name, declared keeper count 1–5, source target IDs 13/16/17,
dimensions, reset-to-current dimensions, and nine resize anchors with the
source top-left default. Inputs below 7 clamp to 7 when dimensions change;
the existing 1024-cell extension is explicitly labeled. Unchanged imported
small dimensions are retained. Expansion uses the full source-stored fill
preset, never the terrain brush. The background is displayed by terrain/owner
name, with an unknown-import label when the preset is unverified.

The child workspace exposes map geometry picking, player/AI settings, map
rules/alliances/creature pools and modifiers, and map variables. Its ten tab
indices remain intact. Terrain/object drawing is guarded at the canvas APIs
and mouse entry point. The unrelated tabs and painting toolbar are hidden.
Player camera/build-area picking still works. Level options and speech/text
open the existing atomic child editors against the independent document.
Explicit child Apply updates that document; outer Cancel closes children and
disposes the session. Changing keeper count preserves all physical player
records, hearts and scenario owners.

Apply all commits ordinary property-grid edits first, refuses unapplied player
or child-window input, stages verified general metadata, and prepares a frozen
resize plan. Changed area above 120×120 requires large-map consent; cropped
records require separate explicit consent with counts. The editor preserves
shifted script targets beyond the resized map, then diagnoses bound project
targets before compilation; see [current script resize behavior](EDITOR_RESIZE_SCRIPT_TARGETS.md).
The generic resize API retains its strict preflight. The live map receives one
atomic operation with exact Undo/Redo, including transaction-only native target
state. Native save/reload still resets that target to 17.

Player geometry follows the source CPU-keeper-only route, including paired
project sentinels, native WORD projection and retained historical ignored bytes.
Out-of-bounds player coordinates produce nonblocking EN/RU warnings; see
[player relocation](EDITOR_RESIZE_PLAYERS.md).

Live document reference/context, edit revision, raw-byte fingerprint, working
document revision/fingerprint, input revisions, child-window identity/lifetime
and child input revisions are rechecked after each prompt and ordinary-field
commit. Reentrant Apply is refused. Input changes followed by reverting text,
language changes, source edit+Undo, raw changes or closing a child/parent cannot
reuse consent. Reload draft explicitly discards the previous session and uses
the current verified live map. A future/unknown metadata layout is not inferred
as writable. Historical V1–V5 disable the absent target field; V6/V7 retain the
stored byte. Unknown keeper/target values and full-width nonterminated names
are retained when unrelated fields change. Changed names use existing strict
CP1252/UTF-16 and overflow checks without imported-text truncation.

## Verification

Whole solution Debug and Release builds succeeded with zero warnings/errors.
Focused Debug `--map-properties-only` completed with exit 0 and six groups in
`Test/editor3-map-properties-ui-focused-debug-05.log`. Actual menu/toolbar,
player/geometry/rule/variable/level/speech controls were exercised in EN/RU
and native/project formats, including compound Cancel/one Undo/save/reopen,
all nine anchors, dimension reset/clamp, large-map denial/acceptance/no-op,
crop denial/acceptance, stale/raw/owner/pending-grid/reentrant guards, historical
V1–V7/future refusal, unknown values/nonterminated names, and all 23 original
background presets in both formats. Tests use temporary fixtures and assert
installed/input files remain unchanged.

Eight arranged WPF images at 1080×800 and 850×650 are stored under
`Test/parent-ui-render-03/`; EN project and RU native minimum-size views were
visually inspected. This is layout evidence, not real hardware/DPI acceptance.

Post-change attempt `editor3-map-properties-parent-20261008-01` stopped on an
app restart before terminal exits and is not accepted. Serial detached attempt
`editor3-map-properties-parent-20261008-02` uses
`tests/Run-DetachedRegression.ps1`, worker PID **15264** (confirmed live at
2026-10-08 11:07 Moscow), with the matching `Test/*-status.json` file.
Actual process state, completion markers and ordered corpus baseline comparison
must be accepted before recording this UI checkpoint as fully validated.

Attempt 02 has confirmed DebugCore exit 0 (85 groups, no failures) and DebugUI
exit 0 (256 checks including four real loaded-map tree checks). Attempt 02
then failed in its wrapper on native stderr; its complete-looking corpus JSON
has no captured terminal exit and is not accepted. The wrapper now treats
stderr as output and uses actual exit codes. Attempt 03 resumes only the three
remaining Release gates, hidden worker PID **13472**, matching status file.
These partial results do not yet close the checkpoint.

Attempt 03 ReleaseCorpus also completed with captured exit 1: 1734 passes /
94 expected failures across 578 KWD inputs. Its entire ordered failure array
is raw-identical to the original baseline, with identical input roots and no
normalization. The accepted comparison is
`Test/editor3-map-properties-parent-20261008-03-corpus-verified-comparison.json`.
Attempt 03 also has terminal ReleaseUI exit 0 (256 PASS lines, including four
loaded-map checks) and ReleaseRealText exit 0 (86 passes, zero failures on the
actual installed Ancients.map). Its status is Completed; worker 13472 is gone.
Together with attempt 02 DebugCore/DebugUI, all five parent-window gates are
accepted. The previous pending/live descriptions above are run history.

Original GUI, external audio/native dialogs, hardware/DPI, actual gameplay,
remaining script/global-definition behavior and historical malformed layouts
remain in the full [FEATURE_PARITY](FEATURE_PARITY.md) goal.


## Grouped creature modifiers — 15 October 2026

The parent map-properties window now opens a private four-parameter species
selector with source nonzero means, elite exclusion, per-parameter writes,
child OK/Cancel and outer atomic Apply/Cancel. The existing matrix remains
available. Source/model proof and compiled-only WPF/manual steps are in
[MAP_CREATURE_MODIFIERS](MAP_CREATURE_MODIFIERS.md).
