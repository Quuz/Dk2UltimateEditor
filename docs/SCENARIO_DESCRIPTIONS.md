# Complete EN/RU scenario descriptions

Implementation checkpoint, 2026-10-08. This covers descriptive projection of
the original 63 condition and 40 action types, not every remaining scenario
editing control or the full original-editor parity goal.

## Source and implementation

The independent source/ABI/phrase/catalogue evidence is in
[the complete source scope](SCENARIO_DESCRIPTION_SOURCE_SCOPE.md). Use the
`-03` phrase audit; earlier inventories incorrectly included the common footer
in the last active case. Original Source/, Binaries/ and installed maps are
not edited by this implementation.

`ScenarioDescriptions.Build` creates an immutable read-only projection keyed
by actual file/offset/length. All 103 cases and their conditional operands are
explicit. Logical project LONG values and actual native compiler fields are
read independently. Missing, foreign or unverified data receives an explicit
unverified/unknown description; no record, alias, reference or imported value
is repaired while browsing. Edits/Undo/language changes rebuild the projection.

Complete EN/RU sentences include signed LONG numbers, original modes and
comparison operators, source-context Score/time-limit overrides, quoted names,
the bounded source numeric-name collapsing quirk and all 256 repeat values.
Duration formatting retains signed C division/remainder and omitted zero
components while using EN plurals and Russian number/case forms. Source format
tokens in imported names remain literal data, never formatting instructions.
Unknown/future name companions stay opaque with an explicit unavailable hint.

The source catalogue reader now deliberately includes TrCreatures/TrRooms/
Terrain/Interface from Datas_Triggers.c. ID/variable/high-byte mappings are
distinct. All 29 terrain names retain the existing EN/RU App vocabulary in a
verified Core resource. Interface entries have explicit descriptive labels.
Door/trap ID collisions retain both possibilities instead of inventing a
unique entity kind. The source's ambiguous trap-first description is documented
by the audit; the modern label makes that ambiguity visible.

ACTION_DESTROYGATE uses project gate ID only for a project. Native descriptions
read the actual two WORD coordinates minus one and check actual non-14 gate
locations. Zero/multiple/invalid matches remain explicitly unresolved; the x
WORD is never interpreted as a gate ID. Other coordinate/source sentinels and
inactive/omitted operands follow the original formatter branches.

Source SEE means the creature is attacked and TAKE means it picks something
up. Both full descriptions and the ordinary EN/RU type picker captions now
match the actual original texts, rather than guesses from C symbol names.

## Actual UI routes and verification

The shared ScriptEventLabel supplies full sentences to tree nodes, link target
choices and move/incoming choices, retaining stable IDs, scopes, markers and
selection. Long choices wrap and expose their whole text as a tooltip. Tree
text width follows the viewport and actual node depth; horizontal navigation
remains available for deeply indented branches.

The literal independent test matrix `ScenarioDescriptionOperands.json` records
all 103 original type IDs and direct BYTE/WORD/LONG offsets. Core checks write
these bytes independently, compare both formats/languages, assert all source
cases, exact representative wording, all repeat values, variable operations,
comparison operators, signed extrema/durations, reserved/shared/quoted aliases,
literal malicious-looking format text, every named creature/room/door/trap/
spell/terrain catalogue, interface entries, bool branches, omitted project
operands, gate coordinate ambiguity, export/reopen and opaque future metadata.
They also assert unchanged bytes, history, revision and dirty state.

Final Core `editor3-scenario-descriptions-core-focused-final.log` passes, exit 0,
including the final duration-case grammar. Final WPF
`editor3-scenario-descriptions-ui-focused-final-02.log` passes four cases,
exit 0: all 103 actual tree labels in EN/RU and both formats, linked choices,
alias edit/Undo/language refresh, owner close and unchanged copy buffer/data.
The final checks include viewport/depth width and minimum-window wrap assertions.
Initial `-01`/`-02` renders exposed clipping; retain them as diagnostics, never
as final layout acceptance. Eight final renders are retained in
`Test/scenario-descriptions-ui-render-final-02`; the RU project and EN native
minimum-width views were inspected with the entire long quoted alias visible.
Debug/Release `editor3-scenario-descriptions-build-*-final-03.log` both report
zero warnings/errors. Full consistent run `editor3-scenario-descriptions-20261008-01`
is terminal Completed, Error null, runner and gate workers gone. Core passes
88/0; Debug/Release WPF each pass 316 cases, including four actual loaded-map
checks and all four 103-type description cases. The corpus passes 1737 checks
with the same complete ordered 94 failures and the exact same root/578 KWD
inputs as the accepted alias stage. Actual installed Ancients.map passes 89/0.
Captured exits are 0/0/1/0/0; corpus exit 1 is the independently compared baseline,
not a zero-failure corpus. Proofs are
`Test/editor3-scenario-descriptions-20261008-01-corpus-verified-comparison.json`
and `Test/editor3-scenario-descriptions-20261008-01-five-gates-verified.json`.
The latter checks source and tested assembly timestamps precede the run and
records their SHA256 snapshots; original source inputs are unchanged.

## Remaining acceptance

The description checkpoint has full regression acceptance. Resume production
edits for complete semantic/enum/compound event editors, other historical
metadata, original GUI/audio/native dialogs, hardware/DPI and actual gameplay
remain in FEATURE_PARITY after this feature checkpoint.
