# Named door, trap and keeper-spell rules

The global-definition tab has an English/Russian rules panel for three families. It covers ten named fields and the trap trigger parameter; numeric properties, effects and projectile drafts remain independent. These controls edit the same stored IDs as the original editor, not combo-box positions.

## Source evidence and coverage

The preserved `Source/Sources/Datas/Datas_GlobalVariables.c` supplies eight catalogues with 51 source choices. `Properties/GlobalVariables/Properties_Global_Elements.c`, pages 2–4, initializes, selects and saves these catalogues. The `GSpellsTrget` spelling is the original source spelling.

| Family / field | Original list | Selectable IDs in source order |
| --- | --- | --- |
| Doors and traps / Material | GDoorsMats | 0, 1, 2, 3, 4, 5, 6, 7 |
| Doors / Crate | GDoorsCrates | 0, 52, 53, 54, 55, 56, 57 |
| Doors / Key | GDoorsKeys | 0, 17, 104, 105, 106, 107, 137 |
| Traps / Crate | GTrapsCrates | 0, 5, 36, 37, 38, 39, 92, 40, 41, 42, 43, 44, 138 |
| Traps / TriggerType | GTrapsTypes | 1, 2, 3 |
| Keeper spells / HandAnim and NoGoHandAnim | GSpellsHand | 0, 1 |
| Keeper spells / CastRule | GSpellsCast | 1, 2, 3, 4, 5 |
| Keeper spells / TargetRule | GSpellsTrget | 1, 2, 3, 4, 5, 6 |

Materials include flesh, rock, wood, two metal types, magic and glass; crate/key labels reuse the source object catalogue. Door key zero is explicitly “No key”. Hand-animation zero is a real “None” choice. The trigger/casting/target lists instead end at ID zero: that terminator is not a selectable “None”. An imported zero in those fields is displayed as unknown and retained, not replaced by the first choice.

`Properties_GlobalSetTypeOptions` and `Properties_GlobalTypeProc` (original lines 808–921) show the trap parameter rules. Modes 1/2/3 use the source action-range / required-pressure / data captions, respectively. All three accept an integer 0–32767. Changing the trigger type does not clear the parameter. No distance, pressure or time unit is inferred from the caption. The old modal dialog updates its parameter only on OK; the modern panel exposes explicit Apply/Revert instead.

## Storage and mutation

`Includes/GlobalVariables.h` stores project IDs as UINT and the trigger parameter as LONG. The original game compiler stores these fields at the following record-relative offsets; the tests encode expected bytes independently using these literals.

| Family | BYTE fields | WORD field |
| --- | --- | --- |
| Doors | Material 0x226; Crate 0x246; Key 0x247 | — |
| Traps | Material 0x22F; Crate 0x20E; TriggerType 0x20B | TriggerData 0x1DC |
| Keeper spells | HandAnim 0x194; NoGoHandAnim 0x195; CastRule 0xEF; TargetRule 0xDA | — |

Each panel keeps a complete immutable subset draft: three door, four trap or four spell fields. All proposed values are preflighted before applying any change, then changed fields are written in one undo transaction. Unchanged fields are not rewritten. Re-entering original defaults does not create an optional project array or history entry.

New IDs must belong to the corresponding source catalogue. Imported unknown IDs and out-of-range parameters can be retained exactly; they are not silently normalized. Unknown trigger types disable parameter editing in this panel. Choosing a known type enables the parameter without erasing an imported value such as 65535; changing that value then requires 0–32767. The low-level raw editor remains a separate workflow.

Drafts belong to a document, family and record index. Applying a stale or foreign draft fails without partial writes. Changes outside the named subset are retained; a pending generic edit that changes a named field invalidates the old panel draft. Missing or unverified native arrays remain read-only. Existing global-definition layout guards and map-scoped export rules still apply.

The three family drafts survive language switching independently. Explicit selected/all source reset also reloads the current panel when the stored reset is a no-op. Other compound drafts remain independent. Closing a document clears controls and cached inputs.

## Verification

`tests/DK2MapEditor.Validation/GlobalRuleChecks.cs` checks all eight source catalogues, zero/sentinel semantics, every choice in all ten mapped fields (61 mapped choices), all 36 source definition defaults in both formats, complete expected native buffers, exact undo/redo and no-op history. It covers trigger endpoints and invalid syntax, unknown IDs/parameters and explicit repair, stale/foreign/missing guards, unrelated-field retention, saved copies/reopening and DKM compilation with original BYTE/WORD casts. Input buffers and original fixtures remain unchanged by export.

`tests/DK2MapEditor.WindowsSmoke/DefinitionRuleSmoke.cs` adds four actual-control cases: EN/RU × native/project. They cover all fields/choices, family drafts and live translation, Apply/Revert, trigger captions and retention, all-field validation, imported unknown values, pending generic commits, explicit no-op reset, missing-native state and document close. Twelve offscreen door/trap/spell layouts are rendered separately for visual inspection.

Focused checks can be run with:

```powershell
dotnet run --project tests/DK2MapEditor.Validation -c Debug --no-build --no-restore -- --definition-rules-only
dotnet run --project tests/DK2MapEditor.WindowsSmoke -c Debug --no-build --no-restore -- --definition-rules-only
```

These cases are also registered in the full runners. They do not establish acceptance by the historical GUI or Dungeon Keeper 2. Effects, other named/compound definition controls, broader native variants and gameplay remain open in [the full parity ledger](FEATURE_PARITY.md).

The 2026-10-06 full gate passes: Debug/Release solution builds with zero warnings/errors; 54 Debug core groups; 117 Release validation groups (54 synthetic groups plus six text inputs and 57 native maps); 132 WPF cases in each configuration, including four loaded-map cases. All twelve Release layouts were generated under `Test/DefinitionRulesUiQA-Final-20261006`; representative EN trap and RU door/spell layouts were visually inspected. `git diff --exit-code -- Source Binaries` confirms the tracked original source and default binaries were not changed.
