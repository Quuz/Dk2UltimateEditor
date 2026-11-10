# Scenario parameter windows: first complete source pages

The goal remains all 69 original parameter pages and the wider FEATURE_PARITY
requirements. This checkpoint adds actual windows for 19 source pages and the
separate repeat page for all 26 conditions without a parameter page. The other
50 parameter pages still need complete controllers, named catalogues, active
branches and nested settings. Existing alias/camera windows are preserved;
their existence does not establish complete parent composition.

## Implemented controls

`ScenarioControlPages.cs` explicitly lists these pages instead of displaying
all eight retained project operands as generic controls:

- Seven conditions: elapsed time, remaining health, mined/current gold, mana,
  slap count and possessed-group size, with all six literal comparisons.
- Eight actions: system messages, moods, timer speech, portals, view, running,
  attacking and primary target, with original radio encodings.
- Four actions: health-display duration, remaining slaps, music level and
  display timer. Music has the original 0–100 slider. The disabled timer
  selector does not become an alias editor; its operand stays unchanged.

Digit limits come from ParamsCheck: health three digits, gold nine/seven, mana
six, counts and health-display duration four. Native field width is an additional
bound: native health is a byte (255), while the project page accepts 999.
Do not invent a 100-percent clamp. Time units use original 23/59/59 callback
limits and a read-only total. Untouched signed imported time bits display their
unsigned source duration without writing the payload. Repeat Count allows
0–254, Once writes 1, Infinite 255; inactive count text is ignored.

## Draft and lifetime

`ScenarioParameterWindow.cs` is opened by the actual workspace button. Changed
controls enter one private immutable draft. Apply validates a sparse whole-page
batch and commits one history operation; Cancel/close discards it. Imported,
inactive and opaque operands, graph links/IDs, aliases, selection and clipboard
stay intact. Unknown choices stay selected. Imports outside the editing range
have an EN/RU preservation hint; effective numeric bounds are displayed.

Document notifications invalidate Apply until reload. Reload cannot retarget
the window to another event type/identity. Whole-document fingerprints detect
raw changes. Pending grid commits, inputs changed during commit, reentrance,
live language switching and workspace/window lifetimes are guarded.

## Independent evidence

`ScenarioScalarPages.json` freezes the 19 original resources/check/store bodies,
literal limits/choices, 26 repeat-only names and complete repeat callback.
`Test/capture_scenario_scalar_pages.py` checks original hashes and refuses
overwrite. Native offsets come from the independent parameter-layout fixture.

Core checks all 45 supported types in both formats, source/physical boundaries,
all choices, every finite repeat count, untouched imports, sparse whitelist,
exact bytes and atomic Undo/Redo. Actual WPF checks all 45 windows EN/RU × both
formats, real commands, batches, translation, no-op/history/dirty/clipboard,
callbacks, Cancel and pending/raw/type/lifetime/reentrance guards.

The previous option stage added 79 missing semantic labels plus two physical
gate-coordinate captions in each language, and 20 source radio groups with
53 choices. Its independent fixture and 932 ordinary selector applications
remain covered. Source/Binaries and installed maps are untouched.

Final Debug/Release builds have zero warnings/errors, focused Core passes two
groups in each configuration, and actual WPF passes eight cases each. Both
configurations retain 24 final minimum-size renders; RU project time, EN native
imported timer and RU native imported music were inspected. Evidence is in
`Test/editor3-scenario-control-window-*-final-*.log` and
`Test/scenario-control-window-render-*-final-01`.
Full five-gate run `editor3-scenario-control-windows-20261008-01` is now terminal
and accepted, finished 2026-10-08 18:37:36 Moscow. Core has 90 passes/0 failures,
both complete WPF configurations 324 PASS, corpus 1739/94 with the complete
raw-identical ordered baseline on 578 KWD, and real Ancients.map 91/0. All exit
markers, loaded-map cases, source/assembly timestamps/hashes and original-source
hashes passed `Test/verify_scenario_control_window_regression.py` (exit 0).
Proof: `Test/editor3-scenario-control-windows-20261008-01-five-gates-verified.json`.
Workers are gone and the code/assembly freeze is lifted. This stage does not
close remaining controls or original GUI/audio/dialog/hardware/DPI/game.
