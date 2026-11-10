# Complete original terrain-definition page

## Current canonical project — 17 October 2026

`GlobalTerrainWindow` and its Core draft are implemented in the current `src/` and
connected through `GlobalVariablesWindow` to the common private parent Apply.
No staging checkout or archived source restoration is required. Current
build/model/full-corpus evidence is in [CURRENT_STATUS](CURRENT_STATUS.md).
Physical EN/RU, focus/audio/DPI/original-editor/game acceptance and the full
catalogue/unit/range inventory remain open. Lower dated Test/staging/candidate
statements describe historical evidence, not the current delivery procedure.

The historical independent `Test/global-terrain-staging-20261009-01` introduced
the Core draft for all22 controls of original resource6820. Current canonical
implementation and delivery supersede that staging procedure as described above.
Historical Debug or UI results are not fresh physical acceptance of this build.

`Test/global-terrain-controls-source-scope-20261009-01.json` captures five
complete original functions, resource6820 and six input hashes. It independently
extracts all38 GTerrains rows, every saved field and all native compiler/reset
offsets. Its Implemented:false identifies the source oracle, not candidate code.

Original MaxHealthType/DestroyedType and their two effects are enabled only
when GTerrains.MinWidth is nonzero: eight source IDs2/3/6/8/9/31/32/35. Their
terrain lists stop before the first MinHeight row, giving exactly13 ordered
choices. Disabled rows retain their full38-row list and imported values.
Visibility is disabled when MaxWidth is nonzero. Merely having a named terrain
catalogue does not establish these original selection policies. The new Core
reads the full original Element metadata; the existing defaults catalogue
intentionally omits those control dimensions and cannot supply this policy.

Eight ordinary limits are Damage -32768..32767; GoldValue/StartingHealth/
MaxHealth/ManaGain/MaxManaGain 0..65535; WibbleH/WibbleV 0..255. The new copy
also adds these limits to the ordinary numeric-table formatter. TerrainLightH
uses integer/4096 with whole-part maximum16000 and the final representable
fraction. Imported values outside manual limits survive exact re-entry; raw
editing remains separate.

The complete draft contains three flags and six RGB bytes. Native flags share
offset0x193 with masks1/16/32; unrelated bits are retained. Source colour dialogs
use COLORREF R|G<<8|B<<16. Original explicit Save zeros all three RGB bytes for
each disabled light, while retaining height. Opening the draft does not run
that cleanup; final Apply does. This distinction is required for Cancel and
imported opaque values, even if explicit Save becomes a real edit.

The new GlobalTerrainDraft/GlobalTerrainControls factory/update/apply API
preflights the full22-field input before any write, preserves inactive and
unknown imports, rejects changed inactive/unknown choices, and applies the
complete page and source cleanup in one operation. All other definition fields,
map files and opaque native bytes remain unchanged. Target baseline/foreign/
missing-layout guards and existing physical-width checks remain enforced.

GlobalTerrainNames.txt reuses all38 exact existing Terrain.ID translations
from the accepted EN/RU resource dictionaries, without a Core dependency on WPF.
There was an initial failed test using ValueCatalogues.Named: generic catalogue
names do not include the border terrain. Diagnostic01 is preserved. Diagnostic02
also preserves a test-builder mistake where a valid first GoldValue overrode
the later invalid endpoint; the input builder now uses the intended final value.

Debug04 validation build has zero warnings/errors. Three targeted Core groups
terminate0: new complete terrain controls, existing definition numbers and
existing decorations. New checks use the independent source fixture and literal
mixed values: all38 rows/22 fields, eight enabled transformations/13 choices,
both endpoints and invalid neighbours of all eight ranges, full RGB/flag/height
composition and six Save-zero branches, complete native expected buffers,
one Undo/Redo, save/reopen/compiler, imported/inactive values, stale/foreign,
missing/duplicate input and missing definitions. Both project/native details pass.

terrain-core-debug-04-progress.json records313C# sources and explicitly sets
FinalAcceptance:false/WpfCases:0. It confirms the accepted scenario snapshot,
all38 full-regression runtime files,1006 original Source/Binaries files and4445
installed editor-map files unchanged. This new copy remains mutable.

Next implement the complete EN/RU window and actual definition command, private
RGB/colour selection, Apply/Cancel and pending/late/reentrant/owner guards.
Existing named terrain effect controls also need the source availability policy;
their inherited API currently enables all terrains. Update their verification
to use an active terrain for repair cases rather than an inactive border.
Then complete WPF minimum-size/scroll/translation renders, both final builds,
Release/full acceptance and source delivery. Broader global catalogues,
historical/original GUI, audio/native dialogs/hardware-DPI/game remain open.

## Complete window and original naming correction (2026-10-09)

The separate candidate now has actual definition-tab command and complete
GlobalTerrainWindow with22 fields, source-filtered type/effect lists, exact
numbers, three flags, six RGB channels, private COLORREF chooser and Apply/Cancel.
All38 rows are checked through the actual command. Apply preflights the complete
draft, checks pending commit/input revision/owner/baseline/reentrancy, and makes
one history edit; disabled lights clear only their RGB on explicit Save.
Actual pending DataGrid commit, Undo/Redo, imports, Cancel, live translation and
late colour/closed-owner responses are exercised in EN/RU/native/project.
Named effect controls now share the source terrain availability policy;
their inherited repair tests use active terrain index2 and retain border imports.
The integrated scenario scope-test fix is also present in this candidate.

Diagnostic window build01 failed on a test helper limited to EditorWorkspace
and an unbraced test loop. Build02 fixed both. Initial UI01 failed because the
offscreen driver measured Window rather than its Content. UI02 then passed
12WPF cases/64renders; UI03 additionally passed actual pending generic commit
and improved English RGB/height captions. All diagnostics/logs/renders remain.

Original RectEx_ChooseColor was audited in a separate immutable
global-terrain-colour-source-scope-20261009-01.json: CC_FULLOPEN|CC_RGBINIT,
16 static custom colours, current/default slots0/1 and COLORREF channel order.
NativeTerrainColourDialog uses ChooseColorW and preserves that process palette.
Injected chooser checks do NOT establish physical Windows-dialog/hardware QA.

First final copy global-terrain-staging-20261009-01 has snapshot02 of317C#,
15fixtures and16DLL, four clean builds and mechanically successful final02
Core3/WPF12/64renders per configuration. It is rejected for inherited
Unknown38 naming: original GTerrains ID38 is szMapWorldClaimScan; Texts_Map.c
calls it Champ de perception. See terrain-final-02-naming-rejection.json.
Do not change/rebuild that frozen copy or treat green counts as final acceptance.

NEW global-terrain-staging-20261009-02 is a source-only successor with
Perception field / Поле восприятия in Core and WPF catalogues. Its independent
fixture also verifies Texts_Map.c and records the original French line.
Core and actual window-title checks require the corrected ID38 name.
Four final01 Debug/Release builds are clean; candidate-snapshot-01.json freezes
317C#/15fixtures/16DLL and unchanged1006 original files/4445installedmaps.
Final focused run01 is now TERMINAL exit0 (unified75486/driver11232); actual
candidate workers are gone. verify_terrain_final_focus.py ranONCE0 and recorded
terrain-focused-verified-01.json VerifiedTerrainFocus:true against snapshotSHA
cf3ea84f86e6527521246582ad6ebf6565d5feb18452d94b315bffc911daf7ad.
Both configurations have3Core groups/12WPF cases/64PNGs, including four actual
pending generic-grid cases; unique EN/RU resource key sets are identical.
Corrected Russian minimum ClaimScan and English Release minimum light-height/
RGB scroll renders were inspected. Full candidate regression/delivery and
physical native-dialog/hardware acceptance remain pending. Root production
source/DLLs stay unchanged while complete-scenario production01 regression runs.

## Actual native colour dialog and full regression (2026-10-09)

Separate native-terrain-colour-probe-20261009-01 references the frozen accepted
App/Core DLLs directly. It constructs only its own in-memory map/window and
loads accepted styles without starting the normal application/settings flow.
Discovery01 opened the real system dialog and checked Cancel at DPI120, but
its black capture is diagnostic evidence only. Full03 failed probe resource
initialization; full04 passed real RGB selection/cancellation but its synthetic
palette click targeted the static child instead of the dialog. These diagnostics
and code/DLL snapshots remain preserved; accepted product files were unchanged.

Full05 is terminal0: twelve actual owned ChooseColorW dialogs, both EN/RU app
languages and both light fields, OK/Cancel with exact RGB channel order, actual
Add-to-custom-colours and retained non-reserved palette entry on reopening,
readable own-window captures, no map mutation before Apply, one atomic Apply
and byte-exact Undo. Russian custom-palette and English ambient-cancel images
were visually inspected. verify_terrain_native_colour_probe.py ranONCE0 and
wrote terrain-native-colour-verified-20261009-01.json. All accepted sources/DLLs,
1006 original files and4445 installed-map files match their frozen hashes.
This accepts the real dialog at the current125% display scale only. Original
editor comparisons, other DPI/hardware and other dialogs remain open. The older
focused proof's native-acceptance:false describes its earlier checkpoint and
has not been overwritten.

Full editor3-complete-terrain-candidate-20261009-01 uses38 exact runtime files
at the original directory depth. Its terminal Core103/0 and real Ancients.map
104/0 have exit0. Corpus1752/94 has captured exit1 and a verified complete
raw-identical ordered94 on578KWD. Driver7172 waits for the live root production
run before its two full UI gates, so full terrain acceptance/delivery remains
pending. Prepared verify_terrain_full_regression.py must run only after all
five terminal gates and worker stop. The next independent
[complete room source scope](GLOBAL_ROOM_CONTROLS_SOURCE_SCOPE.md) captures20
fields/24rows and the permanently inactive BattleEffect source control.
