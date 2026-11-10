# Complete event descriptions: original source and next acceptance

Read-only audit, 2026-10-08, prepared while the combined alias/native-variant
full regression ran with production/test C# and built assemblies frozen.
That run has now finished all five gates with terminal acceptance at 14:19:02
Moscow; the freeze is lifted for implementation. Its accepted snapshot is in
`Test/editor3-scenario-alias-events-20261008-01-five-gates-verified.json`.
This is the next full scenario-description requirement, not an implemented
formatter or evidence of full original GUI/game acceptance.

## Complete source coverage

`Source/Sources/Triggers/Triggers_Texts.c` implements `Triggers_FmtTriggerText`
at line 29 and `Triggers_FmtActionText` at 611. The active
`Datas_Triggers.c` catalogue has **63 conditions and 40 actions**. The read-only
inventory finds an exact formatting case for every one: no missing active case
and no case outside that catalogue. Grouped cases share their real body, rather
than being counted as unrelated empty implementations.

Evidence: `Test/scenario-description-source-audit-20261008.json`, produced by
`Test/audit_scenario_descriptions.py`. Each case records source line, grouped
body, logical parameter numbers, helper calls, text symbols and ctrl use.
All four original input SHA256 hashes remain unchanged. This inventory proves
the source scope; it does not prove that every formatting branch is ported.

`Test/scenario-description-text-dependencies-20261008-03.json`, produced by
`Test/audit_description_text_dependencies.py`, closes each case's transitive
helper/text dependencies: **103 cases, 23 helper functions, 240 resolved text
symbols and 552 source output writes**. Each case retains actual conditional
expressions, nested switch operands, ordered string/printf writes, helper
closure and source literal tokens/printf signatures with file/line provenance.
No case lacks an output write and no required symbol is unresolved. All audited
original bytes remain unchanged. The final case boundaries were independently
checked: unknown-type defaults and common postprocessing are stored separately
under `CommonFormatters`. Earlier unsuffixed/`-02` reports include the footer
in the last case; use `-03` for implementation, branch counts and write order.
These are syntactic source inventories, not proof of path coverage or a
working modern formatter. Whole EN/RU sentences must be authored from these
semantics; do not translate and concatenate French word endings mechanically.

The condition formatter's common postprocessing at lines 588–594 appends
infinite repeat for byte 255 and an explicit count for 2–254; 0 and 1 have
no repeat suffix. This applies to every condition, including unknown types.
The action formatter's lines 953–954 capitalize its initial ASCII letter;
author natural EN/RU whole sentences instead of applying French string edits
to arbitrary imported names. DEBUG_EVENTS appendices are compile-time
diagnostic addresses/links and are separately identified in the audit.

The modern `ScriptTreeControls.cs:26` currently formats a node as `#ID` plus
the translated event type. That omits source operands and quoted alias names.
The same helper supplies ordinary tree entries, branch/incoming choices and
target selectors, so a complete implementation must cover every one of those
routes while retaining stable IDs and graph markers. Source descriptions are
also used on selection/load (`Triggers_Select.c:548,601`) and create/edit tree
updates (`Triggers_Triggers.c:1955–2014`, `Triggers_Actions.c:1958–2016`).

## Actual native operands, not guessed project positions

`Test/scenario-description-native-operands-20261008.json` independently
reconciles all 103 formatter cases with `Compiler_Generate.c` writes. It uses
actual BYTE/WORD/LONG stepping and verifies the compiler default LONG param1/
param5 branch. **102 cases have explicit compiler branches**; TRIGGER_TURNCOAT
uses that verified default. The audit found one formatter logical operand
which is not serialized as itself: **ACTION_DESTROYGATE param5**.

At `Triggers_Texts.c:866–868`, that project action names its gate ID. The
compiler at `Compiler_Generate.c:1813–1817` stores two WORD values for gate
rectangle x/y plus one, after four reserved bytes. A native descriptor must
use the actual coordinates or a verified unambiguous gate match. Do not read
the x WORD as a gate ID. Missing/duplicate matches retain an explicit unresolved
description, never a fabricated ID or modification of imported data.
`EntityReferences.cs:91` already distinguishes that coordinate representation;
`HeroGateGeneration.cs:228–229` writes NativeArgument5/7 as x+1/y+1. Selection
editing also recognizes those coordinate names. Those are reusable evidence
and guards, not proof that a complete gate description exists yet.

The other source formatter operands are represented by known actual compiler
fields. This does not mean every source editor operand is serialized: the
already verified CREATURESLEVEL/LOSTCREATURES param2 omission remains real, but
their text branches do not require it. Keep project/native operand mapping
independent and do not invent native fields or editor metadata by analogy.

## Helpers, aliases and signed semantics

Descriptions depend on the original number, time, comparison, object-operation,
creature/elite, player/object-player, room, door, trap, spell, terrain, element
and speech helpers. Active/inactive modes, zero/None, source plural/case choices
and unknown numeric values must be read from each actual branch. Existing
named catalogues can supply stable IDs and EN/RU vocabulary; they do not by
themselves implement the surrounding sentence or conditional source wording.

The variable/timer alias helpers at `Triggers_Texts.c:983–1021` quote raw aliases
or fall back to slot numbers, with the source numeric/length collapsing quirk
documented in [the alias-wizard audit](SCENARIO_ALIAS_WIZARD_SCOPE.md). Reserved
Score/time-limit subjects are overridden by their calling branch, not by a
universal replacement of raw names. SetVariable has six distinct source
sentences and separate Score source/destination branches at 643–698.

`Triggers_FmtNumberText` at 1099 consumes a **signed LONG**. The time helper at
1027 likewise uses signed division/remainder and omits zero components. Source
input controls can display/store unsigned LONG bits, as covered by the new
alias editor; descriptive rendering is a separate operation. Do not blindly
reuse the editor's unsigned `4294967295` display where the source description
uses LONG `-1`. Test all extreme/imported bit patterns without normalizing
stored bytes. EN/RU grammar requires translated whole messages and appropriate
plural forms, not concatenating French masculine/feminine suffixes.

`Test/scenario-description-catalogues-20261008-02.json`, reproduced by
`Test/audit_description_catalogues.py`, checks eight real source catalogues:
45 trigger creatures, 15 trigger room genders, 15 variable rooms, eight doors,
14 traps, 12 keeper spells, 29 terrains and 11 interface entries. Every lookup
ID is unique within its catalogue. Creature/door/trap/spell lookup uses source
ID, room lookup uses the `Variable` value, and terrain lookup uses the high
byte of the source ID. None uses the row ordinal. Gender tables contain French
grammar metadata, not modern translated names or native creature definitions.

All creature/room/door/trap/spell names have existing Core EN/RU vocabulary.
Thirteen terrain symbols do not map through the general `ValueCatalogues.Named`
prefix rule, but all 29 exact source terrain IDs have EN/RU `Terrain.ID` App
resources, independently checked and retained in this report. Reuse those
actual identities/vocabulary without inventing a Core catalogue key.
The 11 interface entries need explicit descriptive vocabulary; one is the
source's `szUnknown` entry and must keep an unknown label/its actual ID.
`LegacyCompilerRules.ReadElements` currently does not read Datas_Triggers.c,
so asking it for TrCreatures/Terrain/Interface would throw. That integration
must be implemented and verified deliberately, without changing other
catalogue identities, native layouts or original source data.

## Implementation and acceptance requirement

Implement a bounded read-only semantic projection for **all 103 types** and
their source conditional branches. Keep the active source cases explicit and
auditable; a generic type-plus-raw-parameter list does not close this requirement.
Add EN/RU complete descriptions to tree entries and all existing linked choice
routes, retaining IDs, owner/scenario bindings, cycle/shared/unlinked markers,
selection and graph topology. Changes to aliases/operands/language must refresh
actual bound labels. Guard unknown/future layouts, source symbol/format tokens,
invalid references and native coordinate ambiguity without map mutation.

Independently verify every active type and mode/comparison/sentinel branch,
project/native operand bytes, quoted and reserved aliases, unusual imported
names, signed extrema, durations/plurals, catalogue choices and gate coordinate
cases. Projection and browsing must preserve every game/editor byte, dirty
state, history, selections and copy buffers. Test actual EN/RU tree/choice
labels, edits and Undo refresh, language changes, long-text wrapping and rendered
wide/minimum layouts. Capture all full terminal Core/WPF/corpus/real-map gates
and compare the entire ordered baseline failure list. Original GUI/hardware/
gameplay remains a separate full-goal requirement after these checks.
