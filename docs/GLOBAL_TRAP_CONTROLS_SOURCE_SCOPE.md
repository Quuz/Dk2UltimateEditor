# Complete trap page and nested parameters

## Current canonical project — 17 October 2026

`GlobalTrapWindow` and its Core draft are implemented in the current `src/` and
connected through `GlobalVariablesWindow` to the common private parent Apply.
No staging checkout or archived source restoration is required. Current
build/model/full-corpus evidence is in [CURRENT_STATUS](CURRENT_STATUS.md).
Physical EN/RU, focus/audio/DPI/original-editor/game acceptance and the full
catalogue/unit/range inventory remain open. Lower dated Test/staging/candidate
statements describe historical evidence, not the current delivery procedure.

Independent source scope: `Test/global-trap-controls-source-scope-20261009-01.json`,
produced once by `capture_global_trap_controls_scope.py`. It hashes ten original
inputs and captures twenty complete functions, seven resource pages
(6823,6830,6840..6844), fifteen rows and original nested choice tables. This is
historical source evidence; the complete modern trap parent is now in the canonical project described above.

GLOBALTRAP stores28fields. The main page writes24fields unconditionally.
TriggerData is edited through dialog6830; ShotData1/ShotData2 through five
projectile forms. ShotDelay is stored/reset/compiled at0x23D as LONG, but the
main page and nested routes do not expose it. Preserve it without inventing a
source control or ordinary-control range. The editing scope is24main plus three
nested fields, with one additional hidden retained field.

Source trap IDs in array order:4,5,6,9,8,1,10,11,2,3,7,14,15,16,12. The final row
is Magic Door, ID12/index14. Material,Crate,DeathEffect, all six flags,
GoldCost,BuildPoints,Health,HealthGain,ManaUsage are disabled on that row:
fourteen fields and27label/edit/choice/spinner controls. The five corresponding
numeric spinners are explicitly disabled too, unlike door ManaCost. Save still
reads all disabled values without conditional zeroing. Other rows re-enable
exactly the same control IDs.

| Main fields | Original limits |
| --- | --- |
| GoldCost,BuildPoints,Health,HealthGain,ManaUsage,ManaCostToFire,Threat | integer0..65535 |
| IdleEffectDelay,ChargeTime,RechargeTime,ThreatDuration | unsigned /4096, whole part0..16000 |
| Invisible,Disarmable,RevealedWhenFired,RequiresMana,SolidObstacle,FPObstacle | boolean |
| TriggerData in dialog6830 | integer0..32767 |

Material has8choices, Crate13, each of three effects371. TriggerType has IDs1/2/3
with distinct captions. GShots has66choices0..65, including None with form255.
Nested creature/object choices use the captured original GCreatures/Objects.

| Shot form | Source fields/behavior |
| --- | --- |
| 255 | No options; source shows an information message |
| 0 | Two numeric operands; source signedness selects -32768..32767 or0..32767 |
| 1 | One numeric operand; editor retains the second operand |
| 2 | Creature plus count0..100 |
| 3 | Creature plus level1..10 |
| 4 | Object plus gold0..80000; gold enabled only for object1, zeroed on nested OK for other objects |

SetTypeOptions/SetShotOptions submit main fields into the original private global
context before opening a child. Child Cancel retains prior nested values; OK
copies only its operands back. Live-map parent Apply is separate. Modern
composition must retain that distinction and commit the accepted parent in one
Undo. The source compiler zeroes ShotData2 for forms1/255; this is compiler
behavior, not main-page Save normalization. Check native widths and wider
project operands independently.

Two original quirks require explicit compatibility tests: numeric shot
spinner105 calls the helper for control101, and the trap compiler clears0x1EC
with0x23 while Disarmable uses0x10. That clear-mask omits Disarmable and includes
another bit. The modern compiler currently updates each verified bit independently,
preserving unrelated bits. Do not silently change that accepted compiler or
opaque-data policy to reproduce the old clear-mask quirk; assess intended flag
changes and exact bytes in dedicated tests for the complete trap window.

Existing named-rule/decorations/numeric/projectile drafts are partial building
blocks. Current decorations Enabled returns true for traps, including MagicDoor
DeathEffect; the named-rule subset lacks a MagicDoor availability guard. These
are remaining fidelity gaps in named UI routes. A new isolated successor should
align availability and test every source row/control in the full parent, while
preserving raw editing as a separate existing capability.

Still required: private main/nested composition, availability/ranges,
unknown/wide/hidden retention, stale/pending/owner guards, literal fields/masks,
compiler/reopen/Undo, actual EN/RU commands and inspected renders, full regression
and raw ordered94 comparison, delivery and fresh full production regression.
Source scope alone does not complete this block or the wider goal.

## Implementation checkpoint (2026-10-09)

The separate `Test/global-trap-staging-20261009-01` successor is now frozen:
333C# files,18 source fixtures,16 assemblies. It inherits the exact Door327
checkpoint, whose Core focus is accepted; parent Door WPF/full acceptance was
pending at creation and remains a separate requirement. Production311 is unchanged.

`GlobalTrapControls` retains all28 fields with24 main inputs,3 private submitted
child operands and hidden ShotDelay. The actual workspace command opens the
EN/RU parent and its original trigger/five editable shot forms. Child Cancel
retains prior nested values; child OK stays private until parent Apply. Parent
Cancel discards the composition. Parent/child translation, stale/closed owner,
pending table commit, reentrant Apply and minimum-size layouts have actual WPF
tests written; those tests are queued, not yet accepted.

Four final builds have zero warnings/errors. Terminal Core Debug/Release each
passes7 groups: complete trap, door, room, numeric units, named rules,
decorations and terrain. `trap-core-verified-01.json` verifies matching frozen
inputs and unchanged1006 original files/4445 installed maps. Trap tests check
all15 rows, complete main/child catalogues,66 shot types, numeric bounds,
literal native offsets/masks and unrelated bytes, compiler/reopen/Undo,
unchanged signed WORD aliases, imported unknown/wide fields and hidden stale
changes. Explicit child non-gold Save is distinct from parent no-op retention;
forms1/255 zero the second argument only in compiled output. Source MagicDoor
guards also apply to the earlier named-rule and decorations forms in this copy.

Actual WPF queue driver2968 waits for the same native-door probe16636 after
DoorUI2516/Room7508/Terrain7172. It will run14 actual gates, with28 WPF cases
and336 renders per configuration expected until measured (120 trap parent/child
renders plus216 inherited renders). `verify_trap_final_focus.py` is prepared
and syntax-checked, not executed. It requires actual terminal gates, no workers,
unchanged snapshots and `trap-visual-inspection-01.json` recorded only after
viewing the four specified parent renders and both-language minimum-size trigger
and all five nested shot forms. Do not manufacture visual inspection from
filenames or passing tests. WPF, full regression, production delivery, native
original GUI and the wider goal remain open.
