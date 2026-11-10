# Creature job editing

The global definitions tab has a specialized EN/RU editor under **Creatures → Jobs**. It edits all four original behaviour groups, not just their individual stored fields. The original source and game maps are unchanged.

## Original evidence

- `Source/Includes/GlobalVariables.h`: `GLOBAL_HAPPYJOBS`, `GLOBAL_UNHAPPYJOBS`, `GLOBAL_ANGRYJOBS`, `GLOBAL_HATEJOBS` and `GLOBALCREATURE` declare capacities 3 / 2 / 3 / 4. Happy, Unhappy and Angry each store job, chance, mood and mana; Hate stores only job IDs. There are 36 editable stored fields. The original `GLOBALCTX2` arrays have one extra zero sentinel for removal shifts.
- `Source/Sources/Datas/Datas_GlobalVariables.c`, `GCrJobs`: all 22 job IDs, including noncontiguous IDs 0x18, 0x19 and 0x1C. Four explicit catalogue flags govern each group's available jobs: 12 Happy, 16 Unhappy, 10 Angry and 12 Hate. `Texts_Things.c` provides the original French names; the new catalogue has English and Russian equivalents.
- `Source/Sources/Properties/GlobalVariables/Properties_Global_Creatures.c`, `Properties_GlobalUpdateJobs`: choices exclude jobs already used in that group. `Properties_GlobalAddJob` chooses the **first** empty slot in every group. `Properties_GlobalRemJobs` shifts remaining whole tuples left and uses the zero sentinel; it does not reorder all occupied jobs or erase inactive data in other holes.
- `Properties_GlobalJobsProc`, `EN_CHANGE`: new chance is 0–100, new mood/mana are signed integers −32767–32767. These are not 20.12 fixed-point fields. `Properties_GlobalInitCr` marks all four Hate slots `HasNoData`.
- `Properties_GlobalJobsGetTotal` and `Properties_GlobalJobsCheckApply`: total the chances of occupied slots in Happy, Unhappy and Angry, accepting 0 or 100. A different total triggers a confirmation that permits saving; it is **not** an unconditional rejection or automatic normalization. All three groups are checked even if only Hate changed.
- `Source/Sources/Compiler/Compiler_Generate.c`, creature offsets 0x0E14–0x0E80: native job IDs are LONG, mood/mana are WORD bit patterns, chances are BYTE; the historical project stores 32-bit integers. No extra probability rescaling is performed by the compiler.

## Editing and safety

The specialized picker shows named allowed jobs and retains an explicit unknown choice for imported IDs, including negative native LONG values and the full unsigned project UINT range. The fixed-capacity table shows stored slot order, including holes. Mood/mana WORD aliases are displayed signed (65527 → −9) without changing the stored integer. Existing unknown, duplicated or out-of-range values can be retained or removed; newly added jobs must be allowed and unused. Re-entering the same displayed values is a bit-exact no-op, including aliases and values outside the new-input limits.

Add, update and multiselection removal prepare an immutable draft without modifying the map. Switching among the four groups retains all drafted lists. Apply saves all four groups as one atomic undo step; Discard restores the loaded lists. Direct selected-slot updating is an additional convenience; the original add/remove operations remain available. A visible acknowledgement permits nonstandard totals, following the original warning's intent. No-op apply does not require acknowledgement and does not materialize a default-backed project override.

The draft is tied to its owning document, creature and exact original 36-field baseline. Concurrent job edits or a pending generic property-grid commit cannot apply a stale snapshot. Changing language retains the draft, selected group/slots and typed inputs while translating names. Closing/replacing the document clears the state. Missing or unverified native definitions remain read-only with blank current values, not substituted reference defaults. Other source groups, opaque bytes, unused project slots and game maps are preserved by the existing transactional definition writer.

## Verification

`CreatureJobChecks` independently checks all catalogue IDs/group flags, all 45 creature projections, every allowed job in each group in native/project formats, all slot capacities, literal native offsets/WORD/BYTE encoding, later slots, whole-tuple multiremoval, signed limits and no-op aliases, invalid additions/duplicates/ranges, unknown IDs/holes, moved imported tuples, all-group probability warnings/overrides, stale/foreign drafts, atomic notifications, exact undo/redo, save/reopen and original compiler casts. It is part of the full validation suite and can run with `--jobs-only`.

Four WPF cases exercise actual EN/RU × native/project controls: group lists and filtered names, correct Hate layout, drafting all groups without writes, atomic apply/no-op, multiselection removal, warning acknowledgement, invalid input, translation and live input retention, discard, actual generic DataGrid pending commit, unknown IDs/signed aliases, missing-native and close guards. `--jobs-only` runs these four cases. `DK2_JOBS_UI_ARTIFACTS` optionally writes all four rendered groups for all four combinations (16 images). Inspected layouts are retained under `Test/CreatureJobsUiQA-Final-20261006`.

Original Win32 dialog and in-game behavioural acceptance remain pending. Attacks are covered by the separate [creature-attack editor](CREATURE_ATTACKS.md). Level modifiers, other option catalogues, newer creature layouts and unrelated incomplete rows still remain in [the parity ledger](FEATURE_PARITY.md); this milestone is not full original-editor parity.

Verified 2026-10-06 including the creature-attack milestone and no-op reset draft fix: Debug/Release builds have zero warnings/errors; Debug core passes all 52 groups; Release core plus local corpus passes 115 cases (52 core + six text inputs + 57 native maps). Debug and Release WPF each pass 124 cases, including four optional loaded-map cases. The full-width unsigned project ID 4294967295 and negative native ID −1 are preserved in both specialized controls and save/reopen; project compilation produces the original LONG bit cast. Sixteen final renders are saved under `Test/CreatureJobsUiQA-Final-20261006`; EN/RU examples cover all four groups, showing readable names, signed parameters and Hate's ID-only layout. `git diff --exit-code -- Source Binaries` confirms the preserved source/default files. These are regression and rendered-control results, not historical-dialog/gameplay acceptance.

## Current nine-page parent warning

The actual shared creature parent now uses an owned [Continue/Cancel request](CREATURE_JOBS_WARNING_REQUEST.md)
with source defaultContinue, disabled Cancel while remembering, close without
remembering and process-session auto reply/reset. The standalone direct panel's
acknowledgement is a separate convenience workflow. New96 native/project model
cases prove read-only consent and lifetime/flags; owned-window UI is prepared
and compiled but unexecuted. Earlier WPF counts do not prove this new request.

The source [retained Jobs buffer](CREATURE_JOBS_CALLBACK_SCOPE.md) now selects
the common Apply warning, replacing a scan of changed creature rows. A buffer
for the last Jobs row survives another page's shared creature selection and is
checked on inactive/no-op Apply. The documented active KillActive→Apply trace
can ask twice unless the first Continue was remembered; actual old GUI delivery
is still unexecuted. Common modern Apply remains one atomic history operation.


Продолжение: [Jobs9 input/step callbacks](CREATURE_JOB_INPUT_CALLBACKS.md), CreatureJobInputs-20261019. I11 открыт; далее5 Global Elements numeric pages и остальныеcallbacks.
