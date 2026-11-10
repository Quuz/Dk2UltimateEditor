# Inline scenario alias wizard: primary-source scope

Read-only follow-up audit, 2026-10-08. The existing named manager, counters,
ordinary alias-aware selectors and editor-only native companion are covered
by [SCENARIO_NAMES](SCENARIO_NAMES.md). They do not implement the original
editable alias controls inside event parameter pages. This document records
the next concrete source-derived gap; it is not an implementation claim.

The subsequent [implementation and acceptance report](SCENARIO_ALIAS_EVENTS.md)
records the named four-page window, atomic operands+aliases and actual focused
Core/WPF results. Audit-baseline statements below describe the original gap;
full regression and broader event descriptions remain separate requirements.

## Original widgets and operands

`Source/Resources/Editor.rc:3183–3233,3989–4020` defines the four relevant
parameter pages. The primary combo 212 uses CBS_DROPDOWN (editable), while
variable-condition secondary combo 209 uses CBS_DROPDOWNLIST. SetVariable's
secondary combo 214 is also editable.

| Page / source event | Editable alias and logical argument | Other operand |
| --- | --- | --- |
| 7101 / TRIGGER_VARIABLE | Variable in param2, all 129 slots | Six comparisons in param1; number/variable mode in param3; secondary variable param4 is selection-only; numeric value param5 |
| 7102 / TRIGGER_TIMER | Timer in param2, all 17 slots | Six comparisons in param1; hours/minutes/seconds become param5 |
| 7201 / ACTION_SETVARIABLE | Destination variable param1, all 129 slots | Six assign/add/subtract modes in param2; editable source variable param5 only for 0x0C/0x14/0x24; number for 0x0A/0x12/0x22 |
| 7202 / ACTION_SETTIMER | Timer in param1, only slots 0–15 | Time-limit timer 16 is excluded from this source action |

`Triggers_Triggers.c:835–878` populates variable/timer choices, showing Score
for variable 128 and the time limit for timer 16 independently of raw aliases.
`Triggers_Actions.c:1023–1061` populates SetVariable/SetTimer with the same
Score override and the 16-slot SetTimer restriction. These are source logical
arguments: independently map native packed fields rather than copying project
offsets or assuming all five native operands exist.

`Triggers_Triggers.c:723–731` changes param2 only on an actual selection.
`Triggers_Actions.c:925–936` similarly changes param1/param5. Typing a name
does not choose a new ID. Unknown imported IDs must not be silently replaced
by a first/default option.

## Storage, cancellation and source quirks

`Triggers_TriggerDlgParamsStore` at `Triggers_Triggers.c:1398–1436` writes a
nonempty primary edit text to its selected raw alias slot. Its variable
secondary choice does not rename a slot. `Triggers_ActionDlgParamsStore` at
`Triggers_Actions.c:1653–1678` writes the primary alias and, only in a variable
operand mode, the secondary editable alias. Empty edit text does not erase an
existing alias in these original pages. The named manager is the independent
workflow for deliberate clearing. Names use the original 64-byte capacity;
retain strict CP1252/NUL bounds and imported padding rather than transliterate.

Source Back/Next call parameter storage (`Triggers_Triggers.c:805–815`;
`Triggers_Actions.c:974–985`), which can write the global alias arrays before
the entire wizard is accepted. The modern implementation must compose alias
and event parameter changes in a private draft, commit once on acceptance,
and make Cancel discard both. Document this deliberate transaction correction
rather than reproduce a premature mutation during navigation. Preserve graph
IDs, edges, scope/repeat bytes, inactive parameters, owner bindings and aliases
outside the accepted edit. Conflicting edits of the same slot through both
operands must have an explicit deterministic rule, with independent tests.

ACTION_DISPLAYTIMER is a separate page, not a fifth rename page. Its combo
212 is initially disabled in `Editor.rc:4278–4279`, and the source Store branch
does not write TriggersTimers. Its selectable 17-slot timer list is already
covered by ordinary alias-aware selectors. Do not add unsupported inline
renaming by analogy.

## Descriptive formatting and acceptance

`Triggers_Texts.c:983–1021` formats a nonempty variable/timer alias using
szQuotedText, otherwise the slot number. It also collapses an alias when
`atol(alias)` equals the slot and canonical-number length equals alias length.
This is a length/numeric test, not literal equality, and has a source corner
case for nonnumeric one-character names at slot 0. Reserved-list captions
and this raw-alias descriptive formatter are different source operations.
Specify and test any deliberate correction instead of claiming that current
choice labels implement the full formatter. Full condition/action descriptions
remain a wider requirement beyond these four alias pages.

Next implementation requires actual named EN/RU controls for all four pages,
retained ID while typing, independent per-slot drafts, comparisons/modes/time
units, exact source operand storage, atomic event+alias history and native
companion save/reopen. Independently cover all modes, reserved/missing/future
tables, unknown IDs, same/different operand slots, CP1252 limits/imported
padding, empty-name behavior, no-op, Undo/Redo, pending generic commits,
raw/stale/edit+Undo/reentrant and closed-document/window rejection. Verify
actual controls, translation/navigation, rendering and full corpus regression.
Original GUI/game acceptance remains a separate full-goal requirement.
