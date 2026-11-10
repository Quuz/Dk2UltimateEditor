# Keeper spell fields and private projectile composition

## Current canonical project — 17 October 2026

`GlobalSpellWindow` and its Core draft are implemented in the current `src/` and
connected through `GlobalVariablesWindow` to the common private parent Apply.
No staging checkout or archived source restoration is required. Current
build/model/full-corpus evidence is in [CURRENT_STATUS](CURRENT_STATUS.md).
Physical EN/RU, focus/audio/DPI/original-editor/game acceptance and the full
catalogue/unit/range inventory remain open. Lower dated Test/staging/candidate
statements describe historical evidence, not the current delivery procedure.

The independent `Test/global-spell-controls-source-scope-20261009-01.json`
captures9 original source hashes,18 complete functions and six resource pages:
6824 and6840..6844. The source has13 named spells in order
4,2,1,6,10,3,8,13,7,9,11,5,14. Its16 fields comprise12 main controls and four
private operands in the primary and upgraded projectile slots.

| Control | Original range or choices |
| --- | --- |
| ManaCost | Integer0..999999 |
| ManaDrain | Integer0..32767 |
| RechargeTime | Unsigned fixed-point/4096; whole part0..32767 |
| UpgRechargeTime | Integer0..32767; it is not fixed-point |
| Attacking, Defensive | Boolean0/1 |
| HandAnim, NoGoHandAnim | Source IDs0/1 |
| CastRule | Source IDs1..5 |
| TargetRule | Source IDs1..6 |
| ShotType, UpgShotType | All66 source shot types |

Both slots use the same original five editable forms: two numeric operands,
one numeric operand, creature/count, creature/level and object/gold. Form255
has no options. Signed operands are -32768..32767; unsigned operands are
0..32767, count0..100, level1..10 and project gold0..80000. The native WORD
storage preflight rejects gold above65535. Unknown imported choices and wide
values can be retained unchanged, but newly selected values require source
catalogues and ranges. Signed WORD aliases retain their original representation.

Opening a source child submits main fields to the private context. Child Cancel
retains earlier operands; child OK changes only the selected slot. Changing
the main type without child OK retains its stored arguments. Explicit child
OK clears gold for a non-gold object. Compiler-only zeroing of argument2 for
forms1/255 is separate from parent Save. Two flags occupy0xDC masks04/08;
all other native offsets and reset mappings are captured independently.

The isolated `Test/global-spell-staging-20261009-01` implementation is now
frozen339C#/19fixtures/16DLL. It contains the complete field draft, actual EN/RU
workspace command/parent and two private child routes. Four final builds are
clean. Terminal Debug/Release Core each passes8 groups, accepted by
`spell-core-verified-01.json`, with Trap333/production311/original1006/maps4445
unchanged. The parent Trap only had accepted Core at cloning; its WPF/full
acceptance is still pending and must not be inferred from this checkpoint.

Actual WPF driver5532 is alive `WaitingForTrapUI`, behind Trap2968/native16636/
Door2516/Room7508. The queue will run16 gates, with32 WPF cases/496 renders
expected per configuration until actual results:160 spell,120 trap and216
inherited. `verify_spell_final_focus.py` is prepared and syntax-checked, not
executed. It requires terminal actual gates, no workers, matching hashes and
manual viewing of24 minimum renders: four parent views and all five child forms
for both slots in DebugRUproject and ReleaseENnative. No spell renders have yet
been produced or inspected. Full regression, delivery and fresh production
regression remain open.

These are per-definition field windows. They do not establish the original
[shared five-page context](GLOBAL_VARIABLES_PARENT_SESSION_SOURCE_SCOPE.md),
including row switches and private current/all reset before a common Apply.
That workflow and the broader historical/original-GUI/audio/dialog/DPI/game
requirements remain explicit completion gates.
