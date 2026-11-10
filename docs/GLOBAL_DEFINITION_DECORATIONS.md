# Named effects, room objects and creature armour

The global-definition tab adds a bilingual panel for terrain, room, door, trap and creature definitions: 17 effect fields, six room object fields and creature armour material (24 fields total). Creature controls belong to the original Parameters reset group. Other numeric, named-rule, projectile, attack, job and possession drafts remain independent.

## Authoritative source

- `Datas/Datas_Effects.c`, `Effects`: 371 selectable IDs, 0–370, followed by FFFFFFFF. Zero is a real None option; the end marker is not.
- `Datas/Texts_Effects.c`: the descriptions underlying the curated EN/RU `EffectNames.txt`. Twelve nonzero IDs are explicitly described as `<rien>` in the source; they retain individual IDs and a translated “Nothing (source label)”, not a guessed effect or replacement by zero. Six IDs 0x16D–0x172 retain the source 1.7 annotation. These are editor descriptions, not extracted game-language resources or proof of game behavior.
- `Properties/GlobalVariables/Properties_Global_Elements.c`: terrain, room, door and trap effect lists and six room object lists; `Effects[0].Name` supplies the explicit None preceding the room Objects list.
- `Properties_Global_Creatures.c`: the four creature effects and `GDoorsMats` used for armour material.
- `Datas_GlobalVariables.c`, `GRooms`, and `Includes/GlobalVariables.h`: per-room `MGEF_EFFECT0..5` / `MGEF_OBJECT0..5` masks. Room array positions and terrain IDs are different; masks are resolved using the source catalogue ID. BattleEffect is always available in the original dialog, independently of slot masks.

None of the 24 original room types enables `Effects[5]`. The sixth slot is therefore shown but disabled, with its stored value retained. All six object slots are enabled for the Torture Chamber; availability differs for other rooms. Disabled fields are not zeroed when applying an active field. The generic raw workflow remains distinct from this source-policy panel.

## Independent storage evidence

Project fields are original UINT values. Original compiler and reset assignments agree on these record-relative native offsets:

| Family | WORD effect fields | BYTE fields |
| --- | --- | --- |
| Terrains | MaxHealthEffect 0x1A4; DestroyedEffect 0x1A6 | — |
| Rooms | BattleEffect 0x37A; Effects[0..5] 0x382, 0x384, 0x386, 0x388, 0x38A, 0x38C | Objects[0..5] 0x39A–0x39F |
| Doors | DeathEffect 0x232 | — |
| Traps | DeathEffect 0x1F8; PowerlessEffect 0x1F4; IdleEffect 0x1F6 | — |
| Creatures | SlapEffect 0xF3E; HealthEffect 0xCFD; EntranceEffect 0xF36; DeathEffect 0xF40 | ArmourType 0xF76 |

Effect IDs above 255 must retain both WORD bytes. The unaligned creature health-effect WORD at 0xCFD is deliberately encoded as two bytes, not native-memory marshalling. Room objects and armour remain BYTE values; these are not effect-generator identity bytes.

## Input and history guarantees

The complete subset is preflighted before any write. Changed IDs must belong to the corresponding source effect/object/material list, and changed room slots must be active for that room. Applying changed fields uses one transaction; unchanged fields are not rewritten. Imported unknown IDs, inactive-slot values and out-of-range aliases can be retained exactly. Explicit repair of one field does not normalize another. No-op/default-backed edits do not create project arrays, history or change notifications.

Drafts carry document ownership, family/index and the full subset baseline. Stale, foreign or missing/unverified layouts fail without partial mutation. A generic pending commit to any subset field invalidates the old draft. Changes outside the subset remain independent. Save-copy/export and compilation use existing map-scoped and no-overwrite rules.

Live language switching and changing families/creature groups retain input. The workspace now remembers the selected record separately for each family: an actual regression reproduced losing a non-first room draft when the selector returned to room zero. Explicit record changes still reload that family's draft. Selected/all reset discards the corresponding unapplied draft even when the stored reset is a no-op; resetting another creature group does not discard Parameters input. Closing/replacing the document clears selection memory and controls.

Expanded definition controls have vertical scrolling with reserved space for the property table. A 980 × 500 workspace test reaches the last room object and retains a usable table; 1500- and 900-wide layouts are also rendered. Combo tooltips expose full selected descriptions.

## Verification state

`GlobalDecorationChecks.cs` uses literal offsets/sizes and all 24 literal room masks independently of production metadata. It checks all 371 translations/source IDs/version/empty descriptions, defaults of all 130 relevant definitions, every source choice in each active mapped field, complete native expected buffers, disabled-slot rejection, whole-subset transactions, no-ops, undo/redo, stale/foreign/missing guards, unknown retention/repair, independent rule edits and saved/compiled copies. It intentionally does not treat the never-active sixth effect slot as an editable source capability.

`DecorationSmoke.cs` supplies four actual-control cases (EN/RU × native/project): all field lists, all room masks, family/group/translation drafts, imported values, atomic Apply/Revert, pending generic edits, no-op reset, missing-native state, document close and low-height scrolling. The focused four cases pass; 24 offscreen layouts are generated under `Test/DecorationsUiQA-Final-20261006`, separately from full regression and game acceptance.

```powershell
dotnet run --project tests/DK2MapEditor.Validation -c Debug --no-build --no-restore -- --decorations-only
dotnet run --project tests/DK2MapEditor.WindowsSmoke -c Debug --no-build --no-restore -- --decorations-only
```

Both runners also register these cases in the full suite. Named controls for the four placed-map generator slots are now implemented and tested separately in [the generator editor](EFFECT_GENERATORS.md). Historical GUI/game acceptance, broader native variants and other incomplete editing rows remain open in [the parity ledger](FEATURE_PARITY.md).

Completed decorations gate: Debug/Release builds pass with zero warnings/errors; the full WPF runner passed all 136 cases in each configuration, including four loaded-map cases. The focused exhaustive core group and full Debug 55-group regression pass; full Release passes 118 groups (55 synthetic plus six text and 57 native inputs). Representative final EN creature and RU room/narrow-room layouts were inspected. Newer generator/cache changes have their own final regression gate; this completed milestone is not proof of historical GUI/gameplay acceptance or complete editor parity.
