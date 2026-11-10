# Remaining parameter pages and composition

Current canonical project (17 October 2026): these workflows are in the common
`src/DK2MapEditor.App/Editing/ScenarioParameterWindow.cs` and current Core.
No Test/staging copy or held patch is needed to build. Dated evidence below
belongs to its historical snapshot; current results and manual GUI limits are
in [CURRENT_STATUS](CURRENT_STATUS.md) and [REMAINING_WORK](REMAINING_WORK.md).

This is the historical source inventory of22 pages left after the first
named-condition stage, not22 currently missing implementations. Alias, spawn,
spatial, camera, speech and ordinary controls now compose in the common
parameter window. Complete physical GUI/audio/dialog/DPI/game gates and wider
FEATURE_PARITY requirements remain tracked separately.

`Test/capture_remaining_scenario_pages.py` captures append-only
`Test/scenario-remaining-pages-source-20261008-01.json` from original-source
audits and preserved C/resource files. It reads no production implementation to
infer expected encodings. The artifact retains every remaining page's full
default/init/check/store bodies, resource declarations, resolved source captions,
native operand layouts, source named calls, enum values and direct numeric
bindings. Complete shared callbacks, speech/text helpers and native compiler
body are also retained. All 17 original input hashes are verified before/after
capture. This oracle is preparation, not completed modern-page acceptance.

## Page requirements

| Original page | Required complete inputs and workflows |
| --- | --- |
| TRIGGER_VARIABLE / TRIGGER_TIMER | Existing guarded inline alias/operand drafts composed with the complete condition repeat controls; outer Cancel and one atomic Apply. |
| ACTION_SETVARIABLE / ACTION_SETTIMER | Existing guarded inline alias/operand editors available through the complete parameter workflow; retain stable slot IDs, active operand modes and native alias metadata. |
| ACTION_ADDCREATURE | 45 species, seven owners, ten levels, private nested creature flags, point/area selection and direct X/Y coordinates. |
| ACTION_ADDELEMENT | Seven players, the 49-entry category/ID list and Add/Remove state, paired operands atomically. |
| ACTION_SHOWAREA / ACTION_ENABLEFOG | Current area selector and source enable/disable state. |
| ACTION_ALLIES | Two source seven-player lists and establish/break state; no invented Any sentinel. |
| ACTION_RELEASEBAND | Current bands, destination area and three release modes; source dependent invasion/random controls. |
| ACTION_DESTROYGATE | Project gate identity or native coordinate pair; actual gate selection, unknown/ambiguous import retention. |
| ACTION_CHANGESQUARE | 27 source terrain choices, dependent owner control, offset X/Y and point/area callbacks; creation-only rectangle expansion. |
| ACTION_CHANGEOWNER | Seven players, offset X/Y, point/area callbacks. |
| ACTION_TOGGLEEFFECT | Current effect generator and source enable/disable state. |
| ACTION_SHOWELEMENT | 54-entry category/ID list, highlighting state and six-digit time. |
| ACTION_SAY | Current speech-bank entries, corresponding text/audio preview, four-digit speech ID and correctly labelled suppress-text flag. |
| ACTION_CAMERAPATH | Four-digit path ID, current area, correctly labelled ceiling visibility and private 52-path diagram child picker. |
| ACTION_MOVECAMERA | Current area selector. |
| ACTION_ROTATECAMERA | Current area, three-digit angle and four-digit time with native byte bound. |
| ACTION_GENERATE | 45 species and ten levels. |
| ACTION_SHOWNEXTROOM | Source No room plus fifteen ordinary room Variable IDs. |
| ACTION_CHANGEPARAMS | Contextual Any plus seven players, ten source objectives and area selection, without invented implicit resets. |

## Source-confirmed semantic corrections

After the named-page full regression was terminal and accepted, the ordinary
labels in ScenarioParameterChoices were corrected for two fields. Numerical
encodings and existing descriptions remain unchanged:

- ACTION_SAY param5: resource checkbox 308 is DIALOG_ACTIONSSAYNOTEXT, “Ne pas
  afficher le texte correspondant”. Checked stores 1 and the original formatter
  adds its no-text suffix when nonzero. Value 0 means display text; value 1
  means hide text. Both language labels now match those meanings; an independent
  semantic fixture checks the Core values/captions and actual WPF labels.
- ACTION_CAMERAPATH param3: resource radio 200 means show ceiling and stores 0;
  201 means hide ceiling and stores 1. The ordinary labels now say show/hide
  ceiling. Source camera-library ShowCell chooses
  radio 200 when true, 201 when false; keep that source inversion when composing
  the child choice. Descriptions already say show/hide ceiling correctly.

The old frozen-snapshot, staged-only and running-worker statements belonged
to that checkpoint. Those source page implementations are in current src;
current tests are listed in CURRENT_STATUS. Prepared/current model checks do
not replace manual physical acceptance of the assembled windows.

## Bounds, branches and source quirks

RotateCamera accepts angle 0–999 in the source, not an invented 0–360 clamp.
Time is 0–9999 in the project; native param5 is a byte, so explicit native edits
must also fit 0–255. Camera-path ID is four source digits but native storage is a
byte. Speech ID is four digits with native WORD storage. ShowElement time has
six digits. Source Check/Store still reads duration when highlighting is off;
do not silently erase inactive imported values.

ChangeParams always stores its area selector, regardless of objective. Its
source ParamsCheck does not require a nonempty MapAreas list, unlike the area
highlight/fog/camera/release pages. Do not infer a missing-area failure or reset
area bytes merely by browsing/changing an objective. Explicit modern edits must
retain verified sentinel behavior and current map identities.

ReleaseBand's source param2 is 0 for normal, 1 for invasion and 2 for random
invasion. Random checkbox 501 is disabled and unchecked when invasion checkbox
500 is off. A three-state modern selector must preserve those semantics.

Native DestroyGate has no gate-ID operand. NativeArgument5/7 are WORD coordinates
at offsets 12/14; the compiler writes project gate top-left coordinates plus one.
Hearts are not selectable hero gates. Multiple gates at the same coordinates
remain ambiguous and unknown coordinates survive until explicitly changed.
Use verified map regions and the existing exact native generation convention,
not a fabricated ID in unused bytes. Gate selection and both coordinate writes
must share the parent transaction.

Speech list labels prefix a four-digit one-based bank entry ID before the name;
the resource uses LBS_SORT. Payload/text lookups are one-based. Modern sorted or
filtered selection must use the explicit entry ID for both preview and stored
param1. The bank is the current map SpeechFile; corresponding text strips any
prefix through the first underscore, and MENTOR maps to Speech.str. Browsing or
playing a selected entry must not modify the map or its metadata. Child audio
lifetime and configured resource/player settings must integrate with outer
Cancel without introducing unrelated map edits.

The earlier compound oracle remains authoritative for disabled/unknown ADD flag
bits, coordinate offsets, normalized area top-left, dependent terrain owner and
atomic row-major rectangle creation. Current workflows must retain these rules; test
literal native/project bytes, real EN/RU controls and one-step Undo/Redo before
claiming any of these remaining pages complete.
