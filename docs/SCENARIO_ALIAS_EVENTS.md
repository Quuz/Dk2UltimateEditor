# Inline variable/timer event editing

The four original alias parameter pages identified in
[the source audit](SCENARIO_ALIAS_WIZARD_SCOPE.md) now have an actual EN/RU
window, opened from a supported event's Properties panel. It combines event
operands and editor alias names in a private draft. Apply commits both in one
Undo; Close discards pending inputs. The ordinary names manager remains the
explicit clearing/whole-table workflow.

## Covered source behavior

- Variable conditions expose all six comparisons, a number/variable operand,
  all 129 primary variable slots and a selection-only secondary variable.
- Timer conditions expose all six comparisons, all 17 slots and separate
  hours/minutes/seconds. The original two-digit minute/second controls accept
  60–99 and add them directly; the modern window keeps that arithmetic.
- SetVariable exposes six assign/add/subtract modes, all 129 destinations and
  an editable source alias only in variable modes 0x0C/0x14/0x24.
- SetTimer exposes slots 0–15; it does not invent time-limit slot 16 as an
  editable choice. An excluded imported slot is retained as unknown/read-only
  and can be explicitly replaced by a verified slot.

Typing changes a name, not an ID. Each slot has its own cached input when
browsing; only the current editable operands are accepted. Two operands using
the same slot share their text. The Core API rejects conflicting duplicate
requests instead of arbitrarily choosing one. Reserved Score/time-limit
captions remain separate from their retained raw aliases.

Empty inline text retains an existing alias, matching the original pages.
Changed names use strict original 63-byte CP1252/NUL limits, with a visible
EN/RU format hint. Imported nonterminated 64-byte names and unchanged padding
remain exact on no-op and save/reopen; they are not silently normalized.
Native aliases use the existing editor-only `.dk2names` companion, not game
fields. Future/unverified alias tables remain unavailable.

Native and project operands are independently mapped. Unsigned source input
and signed LONG storage share exact 32-bit bits, including 0xFFFFFFFF. Graph
IDs, edges, repeat/scope bytes, reserved bytes and unrelated aliases are retained.
Explicit variable-condition operand edits follow source inactive-operand
clearing. Pure alias edits/no-op preserve inactive imported operands rather
than silently repairing them. This is a deliberate preservation rule.
Long-duration imports remain representable; arithmetic is checked rather
than silently wrapping an overflowing duration.

## Transaction and lifetime

The immutable Core draft captures the actual bounded record, layout, revision
and whole-document fingerprint. Preparation validates every source operand and
alias before a write. Apply rechecks the baseline and writes one transaction.
Raw byte changes, edit+Undo, foreign documents and nested edits cannot reuse
old authority. The original wizard's early alias writes on Back/Next are
deliberately replaced with private draft composition and outer cancellation.

Actual UI Apply freezes inputs, commits pending generic DataGrid edits, and
rechecks owner/context/input authority before writing. A pending ordinary
commit retains that ordinary edit but rejects the stale alias plan. Reentrant
Apply, changed inputs during commit, closed windows/owners and replaced
documents are guarded. Live language changes retain IDs and typed slot inputs.
Document and window events are unsubscribed on close. The new operation has
an explicit translated history label.

## Current evidence and remaining scope

Focused Core exit 0: `Test/editor3-scenario-alias-events-core-focused-02.log`.
Both formats independently encode original ABI/compiler bytes, enumerate every
valid primary slot (582 choices across the four pages), all comparisons and
arithmetic modes, and check one Undo/Redo, no-op, limits, shared/different slots,
unknown/excluded IDs, foreign/raw/edit+Undo guards, CP1252, native companion,
future metadata, nonterminated imports and save/reopen.

Focused WPF exit 0: `Test/editor3-scenario-alias-events-ui-focused-06.log`,
16 cases across EN/RU × native/project × all four types. These cover actual
command creation and inner editable TextBoxes, source choices/mode states,
stable IDs, shared slot names, source time arithmetic, unknown retention and
explicit repair, translation, pending grid commit, reentrance/input revision,
owner/window cancellation and exact bytes/Undo. Final visible name-format hint
has a subsequent focused run, `editor3-scenario-alias-events-ui-focused-final.log`.
Final 32 wide/minimum-size renders are in `Test/scenario-alias-event-ui-render-final`;
EN timer and RU SetVariable minimum-size layouts are inspected.

Both final solution builds have zero warnings/errors. Combined five-gate run
`editor3-scenario-alias-events-20261008-01` has accepted DebugCore exit 0,
87 groups / zero failures, plus DebugUI exit 0 and 312 PASS lines including
four real loaded-map EN/RU checks. ReleaseCorpus finished at 14:05:21 Moscow:
captured exit 1, 1736 passes / 94 failures and 578 KWD inputs. The complete
ordered failure array and exact input-root spelling match the accepted parent;
proof is `Test/editor3-scenario-alias-events-20261008-01-corpus-verified-comparison.json`.
ReleaseUI finished at 14:16:49 Moscow with captured exit 0 and 312 PASS lines,
including all four actual loaded-map EN/RU cases. ReleaseRealText finished at
14:19:02 Moscow: exit 0, 88 groups / zero failures on installed Ancients.map.
All five gates are accepted; status is Completed, Error null and worker gone.
`Test/editor3-scenario-alias-events-20261008-01-five-gates-verified.json` captures
all terminal checks, loaded-map inputs, the entire ordered corpus comparison
and accepted C# source hashes. The reproducible evidence reader is
`Test/verify_scenario_alias_regression.py`. The earlier native stage
has accepted Core 86, Debug UI 296 and corpus 1735 / the raw-identical ordered
94 failures, but its Release UI was interrupted by the new source operation
before its translation was compiled. It is a failed run, not evidence that
the native Release gates finished. The replacement full run must cover both
native-variant and alias changes on a consistent source/assembly snapshot.

The [complete description source audit](SCENARIO_DESCRIPTION_SOURCE_SCOPE.md)
now inventories all 103 original formatting cases and actual native operand
mapping. Complete condition/action descriptions, other semantic/enum/compound event
controls and original GUI/game acceptance remain in FEATURE_PARITY. This
window does not claim those wider requirements or the full goal are complete.

Subsequent complete descriptions now have accepted five-gate evidence in
SCENARIO_DESCRIPTIONS.md. The remaining parameter-page controls are inventoried
in SCENARIO_CONTROL_PAGES_SOURCE_SCOPE.md; the full goal remains open.

The current 13 October source follow-up corrects the initial secondary selector
for ACTION_SETVARIABLE: literal modes start at variable0, while the number stays
cached separately; existing variable modes retain their stored source ID.
This changes neither aliases nor game operands merely by opening the page.
See [dependent input behavior](SCENARIO_DEPENDENT_INPUTS.md) for current source,
model scope and user-run acceptance. Historical WPF runs above predate this fix.
