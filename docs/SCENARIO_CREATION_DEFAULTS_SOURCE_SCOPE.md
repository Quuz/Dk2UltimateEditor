# Original creation and type-change defaults

Source inspection confirms an open editing-parity gap. The original
Triggers_TriggerDlgSetDefault and Triggers_ActionDlgSetDefault explicitly assign
all eight parameters for 70 catalogue types, guarded by `ctrl != NewCtrl`.
The other 33 catalogue types have no explicit branch; their behavior must be
derived from the original new/change/save flow, without inventing resets.

The current LegacyEventSchemas.DefaultPayload is derived from compiler constant
and padding writes. It contains zeros for ordinary parameter operands. Native
SetEventType copies that payload, and the project path zeros all eight parameters
before applying the compiler defaults. These are serialization constants rather
than the original editor's creation defaults. For example, original ADDCREATURE
uses parameters 4, 3, 1, 0x29, 1, 0, 1, 0, while CAMERAPATH's area defaults to 1.

The independently captured inventory covers all 103 catalogue types, all 70
literal eight-parameter assignments, original branch bodies/function positions,
same-type guards, hashes of both original functions and the three current files
that establish the gap. Evidence:
`Test/scenario-creation-defaults-source-20261008-01.json`. The first parser attempt
mistook an earlier function call for its definition; it emitted no proof. The
accepted parser anchors actual `void` definitions and verifies complete branches.

Implementation and acceptance remain required for real new-event and type-change
commands in both formats. Distinguish deliberate initialization of a newly added
record from reselecting an existing type; verify inactive project parameters,
native compiler constants/opaque bytes, graph identity/links, alias tables, repeat,
one Undo/Redo, same-type no-op, parent cancellation and imported maps. Existing
map data must never be silently repaired on load or merely opening parameters.
Compare actual EN/RU command behavior against independently encoded source values.

Neither successful parameter windows nor the currently running full regression
closes this separate requirement. Production is frozen during that run; the
inventory is read-only and original Source/Binaries/maps remain unchanged.

The next [isolated command checkpoint](SCENARIO_DEFAULT_COMMAND_CHECKPOINT.md)
now has terminal Debug/Release focus acceptance. Further source flow inspection
found 26 parameter-free types whose explicit wizard Save resets all parameters,
while the seven other types without default branches retain prior values.
This corrects any inference that all 33 types simply retain on Save.
Constructor values and compiler padding are independently captured in the new
isolated ScenarioCreationDefaults.json fixture. No production change or complete
creation-workflow claim follows from that focused result.
