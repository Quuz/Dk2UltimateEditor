# Complete original door definition page

## Current canonical project — 17 October 2026

`GlobalDoorWindow` and its Core draft are implemented in the current `src/` and
connected through `GlobalVariablesWindow` to the common private parent Apply.
No staging checkout or archived source restoration is required. Current
build/model/full-corpus evidence is in [CURRENT_STATUS](CURRENT_STATUS.md).
Physical EN/RU, focus/audio/DPI/original-editor/game acceptance and the full
catalogue/unit/range inventory remain open. Lower dated Test/staging/candidate
statements describe historical evidence, not the current delivery procedure.

Independent source capture: `Test/global-door-controls-source-scope-20261009-01.json`.
It hashes eight original inputs and captures full original procedures, page6822,
both spinner helpers, nine reset/compiler offsets, eight source rows and four
complete catalogues. Original Source and Binaries are never edited.

| Field | Original control | Native offset/width | Source choices/range |
| --- | --- | --- | --- |
| Cost | 114 / spinner214 | 0x22E / WORD | 0..65535 |
| BuildPoints | 116 / spinner216 | 0x234 / WORD | 0..65535 |
| Health | 118 / spinner218 | 0x22C / WORD | 0..65535 |
| HealthGain | 120 / spinner220 | 0x21C / WORD | 0..65535 |
| ManaCost | disabled122 / independently enabled222 | 0x238 / WORD | Stored WORD; read-only control and inactive spinner input |
| Material | 104 | 0x226 / BYTE | 0..7 |
| DeathEffect | 106 | 0x232 / WORD | 0..370 |
| Crate | 108 | 0x246 / BYTE | 0,52,53,54,55,56,57 |
| Key | 110 | 0x247 / BYTE | 0,17,104,105,106,107,137 |

Source door IDs in array order are 6,1,3,2,4,5,7,8. Numeric edits limit text to
five characters and EN_CHANGE clamps0..65535. Every numeric control has its
separately created up/down, including ManaCost. Page6822 disables its label121
and edit122, but `Misc_CreateUpDownButton` creates spinner222 with
WS_CHILD|WS_VISIBLE|UDS_ALIGNRIGHT and no WS_DISABLED; the door notification
handler accepts222 and `Misc_SetUpDownInt` can change edit122 programmatically.
No init/get/procedure branch disables222. This establishes HWND availability,
not whether the native spinner sends a notification for a disabled buddy.
The paired owned-control Windows probe03 now establishes that it sends none:
both arrows retain0/123/65535, with zero UDN_DELTAPOS and EN_CHANGE notices.
Enabling only the owned buddy makes the same native control and callback work
in all six positive counterchecks. Physical original-editor GUI/mouse/DPI
acceptance remains open; owned-control evidence does not cover those scopes.

Current accepted correction is in mutable
`Test/creature-globals-session-staging-20261009-02`, preserving all prior stages.
`door-buddy-correction-verified-01.json` accepts12Core routes/17PASS groups per
configuration and four actual EN/RU/project/native door WPF cases per configuration,
with64renders each and six actually viewed minimum images. Both final build
configurations have zero warnings/errors. Mana stepping is rejected by the model
and public window route, buttons are unavailable, bilingual hints say read-only,
all nine stored fields still save/reset and ordinary numeric buttons still work.
One-operation Apply/Undo, private Cancel, imports, translation and lifetime guards
pass. Parent351/21/16, frozen ancestors, root311/14/16,1006original files and4445
installed-map files are unchanged. The old frozen Door01 active-Mana-spinner
behavior claim is superseded; its historical reports/files are preserved.
The queued/probe descriptions below are historical, not current live work.

Native evidence: `Test/native-door-spinner-probe-20261009-03/results-01/native-results.json`.
The experiment contains six original disabled-buddy cases and six explicitly
enabled-buddy positive controls. Its old frozen Core comparison reproduces four
incorrect nonboundary Mana changes. No notification is fabricated; only owned
offscreen nonactivating windows are touched. It is measured current-Windows
behavior, not a visible historical original-editor run. Probe01 and dependent
Trap/Spell/Shared01 remain terminal failures, preserved unchanged.

Save writes all nine values unconditionally. The complete window must not invent
inactive-field zeroing. Unknown imported named values and wider project numbers
are retained on unchanged Apply; explicit changes use verified catalogues/ranges.

The isolated `Test/global-door-staging-20261009-01` source successor of the
accepted room checkpoint now implements GlobalDoorControls/Draft, the complete
GlobalDoorWindow and the actual workspace command. Five numeric steppers, four
named lists and all nine values use one private draft and one atomic Apply/Undo.
Mana text remains disabled while its decrement/increment buttons are enabled.
Owner, stale baseline, pending generic grid, revision, closed and reentrant
guards follow the accepted room form. Thirteen EN/RU resource pairs are unique
and match; all source door names use the bilingual original catalogue.

Four final01 Debug/Release Validation/WindowsSmoke builds have zero warnings and
errors. `candidate-snapshot-01.json` freezes327C#/17fixtures/16DLL. Independent
`door-core-verified-01.json` accepts six Core groups per configuration, all twelve
terminal0: complete door, room, ordinary/fixed numbers, named rules, decorations
and terrain. Door checks independently compare all eight rows,393 source choices,
five ranges/endpoints/invalid neighbors, all nine literal offsets/full buffers,
one atomic Apply, byte-exact Undo/Redo, native/project reopen and project compile,
unknown/wide no-op and explicit repair, stale/foreign/incomplete/duplicate guards.
Accepted room and production snapshots,1006original files and4445maps are unchanged.

The early diagnostic Core01 failed because the new door draft selected GLOBALROOM
storage. It is preserved in `door-core-debug-check-01.log`; the schema reference
was corrected to GLOBALDOOR before final01 builds/snapshot. Do not reinterpret
the diagnostic failure or overwrite its log. The final frozen candidate is immutable.

Actual WPF tests are implemented for all eight commands/control availability,
EN/RU/project/native, private stepping/translation/Cancel, all-nine Apply/Undo,
import repair and actual pending generic grid/lifetime/reentrant guards. Four
states at two sizes/top-bottom produce64door images per configuration. These
tests are queued, not accepted: `Run-DoorFinalUI01.ps1`, driver2516, waits for the
same room/terrain full runs to complete and their actual drivers/UI workers to
stop. It then checks frozen hashes and runs six UI groups in each configuration.
Follow that same run; do not start competing UI tests or restart on an observation
timeout. After terminal UI gates, inspect actual renders and verify focus.

Full candidate regression, ordered94 corpus comparison, actual native original
spinner, production delivery with backups/rebase and fresh full production
regression remain open. Broader global definitions/historical/original GUI/audio/
other dialogs/hardware-DPI/in-game acceptance also remain open. Core focus alone
does not complete the door block or the whole goal.

Prepared native check: `Test/native-door-spinner-probe-20261009-01`. It uses exact
source CreateUpDownControl style/buddy/range parameters on its own hidden parent,
disabled label121/edit122 and spinner222. It drives the native button procedure,
never fabricates WM_NOTIFY, and records real deltas/EN_CHANGE/clamped values
at0/123/65535 for both arrow halves. Modern Step is compared with the native
result. Direct Reference uses the frozen Core DLL without rebuilding it. Clean
build and ten probe inputs are fixed in prepared-snapshot-01.json. Behavior has
not passed yet.

Queued native driver16636 runs Run-NativeDoorSpinnerQueued01.ps1 after door UI
driver2516 completes all12gates and actual UI workers stop. Follow the same queue;
verify_native_door_spinner_probe.py must wait for terminal results/workers gone.
The probe covers owned native control behavior; visible original-editor GUI,
physical mouse and DPI acceptance stay open. Primary references:
[CreateUpDownControl](https://learn.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-createupdowncontrol)
and [UDN_DELTAPOS](https://learn.microsoft.com/en-us/windows/win32/controls/udn-deltapos).

verify_door_final_focus.py is prepared/syntax-checked, not executed. It requires
all12terminalUIgates,24cases/216renders per configuration and a separate
door-visual-inspection-01.json written only AFTER actually viewing images.
The four minimum-size requirements cover DebugRUselectedbottom/ENnativebarricade
top and ReleaseENselectedbottom/RUprojectmagic top. Use view_image; do not create
that manifest from filenames or automatic green gates. Full regression and
production delivery remain pending.
