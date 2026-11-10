# Named action parameter windows

The next twelve original action pages are implemented through the guarded
parameter window: ADDELEMENT, SHOWAREA, ENABLEFOG, ALLIES, RELEASEBAND,
TOGGLEEFFECT, SHOWELEMENT, MOVECAMERA, ROTATECAMERA, GENERATE, SHOWNEXTROOM
and CHANGEPARAMS. This exposes 59/69 original parameter schemas and all 26
repeat-only conditions. The remaining ten pages are four alias parent pages
and ADDCREATURE, CHANGESQUARE, CHANGEOWNER, SAY, CAMERAPATH and DESTROYGATE.
Schema availability does not close their acceptance or broader FEATURE_PARITY.

`tests/DK2MapEditor.Validation/ScenarioActionPages.json` is derived from the
independent remaining-page/source catalogue oracles. It contains all thirty
direct store operands, ordered source choices, source numeric limits, native
layouts and original ParamsCheck prerequisites. There is no area prerequisite
for CHANGEPARAMS. Other area actions require current areas; RELEASEBAND needs
bands and areas; TOGGLEEFFECT needs generators. Both Core preflight and actual
window Apply enforce these prerequisites, with EN/RU messages retained during
editing and translation. Band ID zero remains selectable.

Paired category/ID choices update atomically. Explicit input obeys both source
limits and real byte/word storage. Imported unknown/inactive values, unrelated
bytes and the copy buffer are retained; Apply is one edit, Cancel discards the
private inputs. SHOWELEMENT time is retained independently of its highlighting
flag, and CHANGEPARAMS retains its area for every objective.

The first complete focused checks passed: four Core groups and sixteen actual
WPF cases in each configuration, with 250 action applications per language/
format. Both initial builds had zero warnings/errors. Forty-eight minimum-size
renders per configuration were generated; EN project ROTATECAMERA and EN native
ADDELEMENT were inspected. Those initial renders revealed a source mismatch:
three imported Int32 numeric inputs displayed signed values, whereas original
SetDlgItemInt/GetDlgItemInt use FALSE (unsigned).

The current correction marks SHOWELEMENT param5 and ROTATECAMERA param3/param5
as unsigned, displays the original DWORD bits, and normalizes an unchanged
unsigned re-entry before sparse range validation. This keeps unknown imported
bits without enabling an out-of-source-range edit. An independent source check
verified all three FALSE setters/getters before adding the fixture flags. Core
and WPF now test unsigned display/re-entry and unchanged bytes/history.

The first-stage focused results prove the earlier snapshot only. Corrected
Debug/Release builds `editor3-scenario-named-actions-*-build-20261008-02.log`
are terminal exit 0, zero warnings/errors. Both corrected focused Core runs
have four passing groups and exit 0. Both corrected WPF runs are terminal
exit 0 with sixteen cases each. The focused verifier returned Verified true;
workers 7960/3036 are gone. Proof:
`Test/editor3-scenario-named-actions-focused-verified-20261008-02.json`.
Each configuration has 1000 action applications and all 48 expected 540×420
renders; EN project rotation, EN native highlighting, RU project rotation/
objectives and RU native highlighting/release layouts were inspected.
Full five-gate run `editor3-scenario-named-actions-20261008-01` is running.
Keep all production/test C#, fixtures and assemblies frozen until its terminal
verification; this focused checkpoint does not close full acceptance or parity.
An independent original-source audit now verifies all 31 Number controls with
matching FALSE setters/getters. Three action fields and the full-DWORD count
already display unsigned values; 27 capped scalar/condition fields still need
correction. Evidence: `Test/scenario-all-number-unsigned-display-source-20261008-01.json`.
The combined seven-file -02 patch adds explicit flags and Core/WPF imported
re-entry tests. Its memory-only application and unchanged frozen/original files
are verified in `Test/scenario-all-number-unsigned-preparation-verified-20261008-02.json`.
The patch remains unapplied and its staged C# uncompiled/unexecuted until full
acceptance. Preparation does not prove implementation. Wider original GUI/audio/dialog/DPI/game
requirements remain open.
